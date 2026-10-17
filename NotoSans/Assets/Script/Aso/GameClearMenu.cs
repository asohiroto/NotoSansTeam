using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// クリアシーンのメニュー。見た目はすべて実行時に作る（シーンに置くのはこのコンポーネントだけ）。
//  ・「タイトルへ戻る」「ステージ選択へ」の2つのボタン。十字キー（矢印キー）で選び、A ボタン（Enter）で決定
//  ・ここへ来ると、「クリアシーンに行ったことがある」が記録され、ステージ選択にエクストラステージのボタンが出る
public class GameClearMenu : MonoBehaviour, IMenuSelector
{
    [SerializeField] private string k_TitleScene = "TitleScene";
    [SerializeField] private string k_StageSelectScene = "ModeSelectScene";

    // ボタンの画像（選んでいるものと、選んでいないもの）
    [SerializeField] private Sprite k_NormalSprite;
    [SerializeField] private Sprite k_SelectedSprite;
    // 選んでいるときの大きさ（選んでいないときは 1）と、大きさが変わる速さ
    [SerializeField] private float k_SelectedScale = 1.25f;
    [SerializeField] private float k_ScaleLerpRate = 14.0f;
    [SerializeField] private Color k_SelectedTextColor = new Color(0.25f, 0.15f, 0.0f, 1.0f);
    [SerializeField] private Color k_NormalTextColor = Color.white;

    private RectTransform[] buttons_;
    private int selected_ = 0;

    void Start()
    {
        // 初めてここへ来たときだけ、エクストラステージが出たことを知らせる
        bool firstVisit = !SaveData.ClearSceneSeen;
        SaveData.MarkClearSceneSeen();

        // 仮に置いてあった文字（シーン名など）は隠す
        foreach (TMP_Text old in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            old.gameObject.SetActive(false);
        }

        EnsureEventSystem();
        BuildUi(firstVisit);
        Apply(true);
    }

    void Update()
    {
        // 遷移の演出中は操作を受け付けない
        if (SceneTransition.IsBusy)
        {
            Apply(false);
            return;
        }

        int move = 0;
        bool submit = false;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.up.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame) { move = -1; }
            if (pad.dpad.down.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame) { move = 1; }
            if (pad.buttonSouth.wasPressedThisFrame) { submit = true; }
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) { move = -1; }
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) { move = 1; }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { submit = true; }
        }

        if (move != 0)
        {
            selected_ = (selected_ + move + buttons_.Length) % buttons_.Length;
        }
        if (submit)
        {
            Activate(selected_);
        }
        Apply(false);
    }

    public void Select(int index)
    {
        selected_ = index;
    }

    public void Activate(int index)
    {
        if (SceneTransition.IsBusy)
        {
            return;
        }
        SceneTransition.Go(index == 0 ? k_TitleScene : k_StageSelectScene);
    }

    // 選んでいるボタンを大きく・目立たせる
    void Apply(bool immediate)
    {
        bool useSprites = k_NormalSprite != null && k_SelectedSprite != null;
        for (int i = 0; i < buttons_.Length; i++)
        {
            bool on = i == selected_;
            float target = on ? k_SelectedScale : 1.0f;
            float scale = immediate ? target : Mathf.Lerp(buttons_[i].localScale.x, target, 1.0f - Mathf.Exp(-k_ScaleLerpRate * Time.unscaledDeltaTime));
            buttons_[i].localScale = new Vector3(scale, scale, 1.0f);

            Image image = buttons_[i].GetComponent<Image>();
            if (useSprites)
            {
                image.sprite = on ? k_SelectedSprite : k_NormalSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = on ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1.0f);
            }
            buttons_[i].GetComponentInChildren<TMP_Text>().color = on ? k_SelectedTextColor : k_NormalTextColor;
        }
    }

    // ---- 見た目の組み立て ----

    void BuildUi(bool firstVisit)
    {
        GameObject canvasObj = new GameObject("ClearCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        Transform root = canvasObj.transform;

        TMP_Text title = CreateText(root, "ゲームクリア！", 140, new Vector2(0.0f, 300.0f), new Vector2(1600.0f, 200.0f), Color.white);
        GameFonts.SetHeading(title);

        if (firstVisit)
        {
            TMP_Text note = CreateText(root, "エクストラステージのボタンが ステージ選択に出現した！", 46, new Vector2(0.0f, 160.0f), new Vector2(1600.0f, 80.0f), new Color(1.0f, 0.9f, 0.4f, 1.0f));
            note.name = "ExtraNote";
        }

        buttons_ = new RectTransform[2];
        string[] labels = { "タイトルへ戻る", "ステージ選択へ" };
        for (int i = 0; i < buttons_.Length; i++)
        {
            buttons_[i] = CreateButton(root, labels[i], new Vector2(0.0f, -20.0f - 150.0f * i), i);
        }
    }

    RectTransform CreateButton(Transform parent, string label, Vector2 position, int index)
    {
        GameObject obj = new GameObject("Button" + index);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.sprite = k_NormalSprite;
        image.type = Image.Type.Sliced;
        RectTransform rect = image.rectTransform;
        rect.sizeDelta = new Vector2(560.0f, 100.0f);
        rect.anchoredPosition = position;

        TMP_Text text = CreateText(obj.transform, label, 52, Vector2.zero, Vector2.zero, k_NormalTextColor);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        // マウスでも選べる・押せるようにしておく
        MenuButtonPointer pointer = obj.AddComponent<MenuButtonPointer>();
        pointer.Init(this, index);
        return rect;
    }

    static TMP_Text CreateText(Transform parent, string value, int fontSize, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        if (GameFonts.Body != null)
        {
            text.font = GameFonts.Body;
        }
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = size;
        return text;
    }

    // マウス操作のために EventSystem が要る。無いときは作る
    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }
        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        InputSystemUIInputModule module = obj.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }
}
