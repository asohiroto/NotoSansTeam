using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// ゲーム中のUI。プレハブ（GameHud）をシーンに入れるだけで、見た目はすべて自動で作られて動く。
//  ・左上に、そのシーンで死んだ回数を表示する
//  ・スタートボタン（Esc / P）でポーズ。ポーズ中は「再開」「ステージ選択へ」「ゲーム終了」を選べる
//  ・「ゲーム終了」は、確認のポップアップで「YES」を選んだときだけ終了する
// 操作は十字キー（矢印キー）で選び、A ボタン（Enter）で決定、B ボタンで戻る
public class GameHud : MonoBehaviour, IMenuSelector
{
    // 「ステージ選択へ」で遷移するシーン名（Build Settings に入れておくこと）
    [SerializeField] private string k_StageSelectScene = "ModeSelectScene";

    // 選んでいるボタンの大きさ（選んでいないときは 1）と、大きさが変わる速さ
    [SerializeField] private float k_SelectedScale = 1.2f;
    [SerializeField] private float k_ScaleLerpRate = 16.0f;
    [SerializeField] private Color k_SelectedColor = Color.white;
    [SerializeField] private Color k_NormalColor = new Color(0.62f, 0.64f, 0.72f, 1.0f);

    // ポーズ中か（他のスクリプトが、ポーズ中の入力を無視するための目印）
    public static bool IsPaused { get; private set; }
    // 最後にポーズの状態が変わったフレーム
    public static int LastChangeFrame { get; private set; }

    private enum Page
    {
        // ポーズしていない
        None,
        // ポーズのメニュー
        Pause,
        // 「ゲーム終了」の確認ポップアップ
        ConfirmQuit,
    }

    // 死亡数の表示を更新する間隔（秒）
    private const float k_DeathRefreshInterval = 0.2f;

    private Page page_ = Page.None;
    private int selected_ = 0;

    private TMP_Text deathText_;
    private int shownDeaths_ = -1;
    private float refreshTimer_ = 0.0f;

    private GameObject pausePanel_;
    private GameObject confirmPanel_;
    private RectTransform[] pauseButtons_;
    private RectTransform[] confirmButtons_;

    void Awake()
    {
        IsPaused = false;
        BuildUi();
        SetPage(Page.None);
    }

    void OnDestroy()
    {
        // シーンを離れるときは、時間も音も元に戻す（次のシーンが止まったままにならないように）
        Time.timeScale = 1.0f;
        AudioListener.pause = false;
        IsPaused = false;
    }

    void Update()
    {
        RefreshDeaths();

        // 遷移の演出中は操作を受け付けない
        if (SceneTransition.IsBusy)
        {
            ApplyButtons(false);
            return;
        }

        int move = 0;
        bool submit = false;
        bool back = false;
        bool start = false;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.up.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame) { move = -1; }
            if (pad.dpad.down.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame) { move = 1; }
            if (pad.buttonSouth.wasPressedThisFrame) { submit = true; }
            if (pad.buttonEast.wasPressedThisFrame) { back = true; }
            if (pad.startButton.wasPressedThisFrame) { start = true; }
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) { move = -1; }
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) { move = 1; }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { submit = true; }
            if (keyboard.escapeKey.wasPressedThisFrame) { back = true; start = true; }
            if (keyboard.pKey.wasPressedThisFrame) { start = true; }
        }

        switch (page_)
        {
            case Page.None:
                // ポーズしていないときは、スタートボタンだけ受け付ける
                if (start)
                {
                    SoundEffects.Play(SoundEffects.Se.Select);
                    SetPage(Page.Pause);
                }
                break;

            case Page.Pause:
                if (start || back)
                {
                    SoundEffects.Play(SoundEffects.Se.Cancel);
                    SetPage(Page.None);
                }
                else
                {
                    Navigate(move, pauseButtons_.Length);
                    if (submit)
                    {
                        Activate(selected_);
                    }
                }
                break;

            case Page.ConfirmQuit:
                // 確認中は、戻る操作で「NO」と同じ（ポーズのメニューへ戻る）
                if (back && !start)
                {
                    SoundEffects.Play(SoundEffects.Se.Cancel);
                    SetPage(Page.Pause, 2);
                }
                else if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    SoundEffects.Play(SoundEffects.Se.Cancel);
                    SetPage(Page.Pause, 2);
                }
                else
                {
                    Navigate(move, confirmButtons_.Length);
                    if (submit)
                    {
                        Activate(selected_);
                    }
                }
                break;
        }

        ApplyButtons(false);
    }

    void Navigate(int move, int count)
    {
        if (move != 0 && count > 0)
        {
            selected_ = (selected_ + move + count) % count;
            SoundEffects.Play(SoundEffects.Se.Change);
        }
    }

    // マウスが乗ったとき（いま表示しているページのボタンの番号）
    public void Select(int index)
    {
        if (index != selected_)
        {
            selected_ = index;
            SoundEffects.Play(SoundEffects.Se.Change);
        }
    }

    // 決定（いま表示しているページのボタンの番号）
    public void Activate(int index)
    {
        if (SceneTransition.IsBusy)
        {
            return;
        }

        if (page_ == Page.Pause)
        {
            switch (index)
            {
                case 0:
                    SoundEffects.Play(SoundEffects.Se.Cancel);
                    SetPage(Page.None);
                    break;
                case 1:
                    SoundEffects.Play(SoundEffects.Se.Select);
                    // 遷移の演出中も止めたままにしておく（シーンが替わると元に戻る）
                    SceneTransition.Go(k_StageSelectScene);
                    break;
                case 2:
                    SoundEffects.Play(SoundEffects.Se.Select);
                    // 確認のポップアップを出す。最初は「いいえ」を選んでおく
                    SetPage(Page.ConfirmQuit, 1);
                    break;
            }
        }
        else if (page_ == Page.ConfirmQuit)
        {
            if (index == 0)
            {
                SoundEffects.Play(SoundEffects.Se.Select);
                Quit();
            }
            else
            {
                SoundEffects.Play(SoundEffects.Se.Cancel);
                SetPage(Page.Pause, 2);
            }
        }
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ページを切り替える（ポーズの開始・解除もここで行う）
    void SetPage(Page page, int select = 0)
    {
        bool wasPaused = IsPaused;
        page_ = page;
        selected_ = select;
        IsPaused = page != Page.None;

        pausePanel_.SetActive(page != Page.None);
        confirmPanel_.SetActive(page == Page.ConfirmQuit);

        Time.timeScale = IsPaused ? 0.0f : 1.0f;
        AudioListener.pause = IsPaused;
        if (wasPaused != IsPaused)
        {
            LastChangeFrame = Time.frameCount;
        }
        ApplyButtons(true);
    }

    // そのシーンで死んだ回数を表示する（StageManager が数えている）
    void RefreshDeaths()
    {
        refreshTimer_ -= Time.unscaledDeltaTime;
        if (refreshTimer_ > 0.0f)
        {
            return;
        }
        refreshTimer_ = k_DeathRefreshInterval;

        int deaths = StageManager.DeathCount;
        if (deaths != shownDeaths_)
        {
            shownDeaths_ = deaths;
            deathText_.text = "死亡数  " + deaths;
        }
    }

    // 選んでいるボタンを大きく・明るくする
    void ApplyButtons(bool immediate)
    {
        ApplyButtons(pauseButtons_, page_ == Page.Pause, immediate);
        ApplyButtons(confirmButtons_, page_ == Page.ConfirmQuit, immediate);
    }

    void ApplyButtons(RectTransform[] buttons, bool active, bool immediate)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            bool on = active && i == selected_;
            float target = on ? k_SelectedScale : 1.0f;
            float scale = immediate ? target : Mathf.Lerp(buttons[i].localScale.x, target, 1.0f - Mathf.Exp(-k_ScaleLerpRate * Time.unscaledDeltaTime));
            buttons[i].localScale = new Vector3(scale, scale, 1.0f);
            buttons[i].GetComponent<Image>().color = on ? k_SelectedColor : k_NormalColor;
        }
    }

    // ---- 見た目の組み立て（コードで作るので、プレハブの中身は空のままでよい） ----

    void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObj = new GameObject("HudCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 遷移の演出（SceneTransition）より手前には出さない
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        Transform root = canvasObj.transform;

        // 左上：死亡数
        Image deathBack = CreateImage("DeathBack", root, new Color(0.0f, 0.0f, 0.0f, 0.45f));
        SetRect(deathBack.rectTransform, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 1.0f), new Vector2(30.0f, -30.0f), new Vector2(380.0f, 80.0f));
        deathBack.raycastTarget = false;
        deathText_ = CreateText("DeathText", deathBack.transform, "死亡数  0", 48, Color.white);
        deathText_.rectTransform.anchorMin = Vector2.zero;
        deathText_.rectTransform.anchorMax = Vector2.one;
        deathText_.rectTransform.offsetMin = Vector2.zero;
        deathText_.rectTransform.offsetMax = Vector2.zero;

        // ポーズ：画面全体を暗くして、メニューを出す
        Image pauseBack = CreateImage("PausePanel", root, new Color(0.0f, 0.0f, 0.0f, 0.6f));
        Stretch(pauseBack.rectTransform);
        pausePanel_ = pauseBack.gameObject;
        TMP_Text pauseTitle = CreateText("PauseTitle", pausePanel_.transform, "ポーズ", 120, Color.white);
        GameFonts.SetHeading(pauseTitle);
        SetRect(pauseTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.0f, 260.0f), new Vector2(1000.0f, 160.0f));
        TMP_Text pauseHint = CreateText("PauseHint", pausePanel_.transform, "十字キー：選ぶ　　Aボタン：決定　　スタート／Bボタン：再開", 32, new Color(1.0f, 1.0f, 1.0f, 0.75f));
        SetRect(pauseHint.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0.5f, 0.0f), new Vector2(0.0f, 60.0f), new Vector2(1500.0f, 50.0f));
        pauseButtons_ = new RectTransform[3];
        string[] pauseLabels = { "再開", "ステージ選択", "ゲーム終了" };
        for (int i = 0; i < pauseButtons_.Length; i++)
        {
            pauseButtons_[i] = CreateButton("PauseButton" + i, pausePanel_.transform, pauseLabels[i], new Vector2(0.0f, 80.0f - 130.0f * i), new Vector2(560.0f, 100.0f));
        }

        // ゲーム終了の確認ポップアップ（ポーズのメニューの手前に出す）
        Image confirmBack = CreateImage("ConfirmPanel", root, new Color(0.0f, 0.0f, 0.0f, 0.55f));
        Stretch(confirmBack.rectTransform);
        confirmPanel_ = confirmBack.gameObject;
        Image window = CreateImage("Window", confirmPanel_.transform, new Color(0.12f, 0.16f, 0.3f, 1.0f));
        SetRect(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000.0f, 460.0f));
        TMP_Text confirmText = CreateText("ConfirmText", window.transform, "ゲームを終了しますか？", 70, Color.white);
        GameFonts.SetHeading(confirmText);
        SetRect(confirmText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.0f, 100.0f), new Vector2(940.0f, 130.0f));
        confirmButtons_ = new RectTransform[2];
        string[] confirmLabels = { "はい", "いいえ" };
        for (int i = 0; i < confirmButtons_.Length; i++)
        {
            confirmButtons_[i] = CreateButton("ConfirmButton" + i, window.transform, confirmLabels[i], new Vector2(-210.0f + 420.0f * i, -100.0f), new Vector2(360.0f, 100.0f));
        }
    }

    // マウスで選ぶ・押すために EventSystem が要る。無いシーンでは自動で作る
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

    RectTransform CreateButton(string objName, Transform parent, string label, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(objName, parent, Color.white);
        SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        TMP_Text text = CreateText("Label", image.transform, label, 54, new Color(0.1f, 0.12f, 0.2f, 1.0f));
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        // マウスでも選べる・押せるようにしておく（番号は、そのページの中での順番）
        int index = int.Parse(objName.Substring(objName.Length - 1));
        MenuButtonPointer pointer = image.gameObject.AddComponent<MenuButtonPointer>();
        pointer.Init(this, index);
        return image.rectTransform;
    }

    static Image CreateImage(string objName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TMP_Text CreateText(string objName, Transform parent, string value, int fontSize, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        // 日本語のフォント（太字は、このフォント自体が太いので使わない）
        if (GameFonts.Body != null)
        {
            text.font = GameFonts.Body;
        }
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        // 左上に置くものは、左上の角を基準にする
        rect.pivot = anchorMin.x == 0.0f && anchorMin.y == 1.0f ? new Vector2(0.0f, 1.0f) : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
