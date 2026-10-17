using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// モード選択のメニュー。ステージのボタン（StageCard）を左右（十字キー・矢印キー）で選び、A ボタンで遷移する。
// 各ステージの下には「スコア削除」ボタンがあり、下キーで移れる。押すと確認のポップアップが出て、
// 「YES」を選んだときだけ、そのステージの死亡数とクリア数を消す。
// B ボタン（Esc）でタイトルへ戻る。選んでいるボタンは大きくなる
public class ModeSelectMenu : MonoBehaviour, IMenuSelector
{
    // 並べたステージのボタン（左から順に）
    [SerializeField] private StageCard[] k_Cards;
    // 戻る先のシーン名
    [SerializeField] private string k_TitleScene = "TitleScene";

    // 選んでいるときの大きさ（選んでいないときは 1）
    [SerializeField] private float k_SelectedScale = 1.12f;
    // 大きさが変わる速さ
    [SerializeField] private float k_ScaleLerpRate = 14.0f;
    [SerializeField] private Color k_SelectedColor = Color.white;
    [SerializeField] private Color k_NormalColor = new Color(0.62f, 0.64f, 0.72f, 1.0f);

    // 確認ポップアップのボタン番号は、この値からの連番にする（カードや削除ボタンの番号と重ならないように）
    private const int k_ConfirmIndexBase = 1000;
    // スコア削除ボタンの大きさと、選んだときの大きさ
    private const float k_DeleteSelectedScale = 1.2f;
    private static readonly Color k_DeleteNormalColor = new Color(0.62f, 0.25f, 0.25f, 1.0f);
    private static readonly Color k_DeleteSelectedColor = new Color(1.0f, 0.35f, 0.3f, 1.0f);

    // 選んでいるステージ
    private int selected_ = 0;
    // 0：ステージのボタンを選んでいる　1：その下のスコア削除ボタンを選んでいる
    private int row_ = 0;

    private RectTransform[] deleteButtons_;

    // ステージのボタンを並べる間隔
    private const float k_CardSpacing = 600.0f;
    // 隠れているステージ（エクストラが、まだ出ていないとき）
    private bool[] hidden_ = new bool[0];

    // 確認ポップアップ
    private bool confirming_ = false;
    private int confirmSelected_ = 1;
    private int deleteTarget_ = 0;
    private GameObject confirmPanel_;
    private TMP_Text confirmText_;
    private RectTransform[] confirmButtons_;

    void Start()
    {
        BuildDeleteButtons();
        BuildConfirmPopup();

        // シーンに置いてある文字も、日本語のフォントにする（見出しとステージ名は見出し用）
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GameFonts.ApplyBody(canvas.transform);
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "Title Text" || text.name == "Name" || text.name == "ConfirmText")
                {
                    GameFonts.SetHeading(text);
                }
            }
        }

        for (int i = 0; i < k_Cards.Length; i++)
        {
            // マウスでも選べる・押せるようにしておく
            MenuButtonPointer pointer = k_Cards[i].gameObject.AddComponent<MenuButtonPointer>();
            pointer.Init(this, i);
            k_Cards[i].Refresh();
        }
        RefreshLocks();
        Apply(true);
    }

    // 隠すステージ（エクストラは、一度クリアシーンに行くまで隠す）と、ロック中のステージを決める。
    // 一度クリアした・解放した記録は、スコアを削除しても残る
    void RefreshLocks()
    {
        hidden_ = new bool[k_Cards.Length];
        int visibleCount = 0;
        for (int i = 0; i < k_Cards.Length; i++)
        {
            hidden_[i] = !StageProgress.IsVisible(k_Cards[i].SceneName);
            k_Cards[i].gameObject.SetActive(!hidden_[i]);
            if (hidden_[i])
            {
                continue;
            }
            visibleCount++;

            bool locked = !StageProgress.IsUnlocked(k_Cards[i].SceneName);
            k_Cards[i].SetLocked(locked, locked ? UnlockCondition(k_Cards[i].SceneName) : string.Empty);
            // ロック中のステージのスコアは、削除できない（まだ何もないため）
            deleteButtons_[i].gameObject.SetActive(!locked);
        }

        // 出ているボタンだけを、画面の中央に等間隔で並べ直す
        int order = 0;
        for (int i = 0; i < k_Cards.Length; i++)
        {
            if (hidden_[i])
            {
                continue;
            }
            RectTransform rect = (RectTransform)k_Cards[i].transform;
            rect.anchoredPosition = new Vector2((order - (visibleCount - 1) * 0.5f) * k_CardSpacing, rect.anchoredPosition.y);
            order++;
        }

        // 隠れているステージを選んだままにしない
        if (k_Cards.Length > 0 && hidden_[selected_])
        {
            selected_ = NextVisible(selected_, 1);
        }
        if (k_Cards.Length > 0 && k_Cards[selected_].IsLocked)
        {
            row_ = 0;
        }
    }

    // from から direction（1＝右、-1＝左）の向きに、出ている次のステージを探す
    int NextVisible(int from, int direction)
    {
        int index = from;
        for (int n = 0; n < k_Cards.Length; n++)
        {
            index = (index + direction + k_Cards.Length) % k_Cards.Length;
            if (!hidden_[index])
            {
                return index;
            }
        }
        return from;
    }

    // ロック中に見せる解放の条件の文
    string UnlockCondition(string scene)
    {
        if (scene == StageProgress.ExtraStage)
        {
            // エクストラ：通常のステージそれぞれを 3 回以上クリア
            string stages = string.Empty;
            foreach (string stage in StageProgress.Stages)
            {
                stages += (stages.Length > 0 ? "と" : string.Empty) + DisplayName(stage);
            }
            return stages + "を" + (StageProgress.Stages.Length > 1 ? "どちらも" : string.Empty) + StageProgress.ExtraClearCount + "回以上クリアで解放";
        }

        // 通常のステージ：前のステージをクリア
        string names = string.Empty;
        int index = System.Array.IndexOf(StageProgress.Stages, scene);
        for (int i = 0; i < index; i++)
        {
            names += (names.Length > 0 ? "と" : string.Empty) + DisplayName(StageProgress.Stages[i]);
        }
        return names + "をクリアすると解放";
    }

    // シーン名から、ボタンに付いているステージ名を探す
    string DisplayName(string scene)
    {
        foreach (StageCard card in k_Cards)
        {
            if (card.SceneName == scene)
            {
                return card.StageName;
            }
        }
        return scene;
    }

    void Update()
    {
        // 遷移の演出中は操作を受け付けない
        if (SceneTransition.IsBusy)
        {
            Apply(false);
            return;
        }

        int moveX = 0;
        int moveY = 0;
        bool submit = false;
        bool back = false;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.left.wasPressedThisFrame) { moveX = -1; }
            if (pad.dpad.right.wasPressedThisFrame) { moveX = 1; }
            if (pad.dpad.up.wasPressedThisFrame) { moveY = -1; }
            if (pad.dpad.down.wasPressedThisFrame) { moveY = 1; }
            if (pad.buttonSouth.wasPressedThisFrame) { submit = true; }
            if (pad.buttonEast.wasPressedThisFrame) { back = true; }
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame) { moveX = -1; }
            if (keyboard.rightArrowKey.wasPressedThisFrame) { moveX = 1; }
            if (keyboard.upArrowKey.wasPressedThisFrame) { moveY = -1; }
            if (keyboard.downArrowKey.wasPressedThisFrame) { moveY = 1; }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { submit = true; }
            if (keyboard.escapeKey.wasPressedThisFrame) { back = true; }
        }

        if (confirming_)
        {
            // 確認中は、ポップアップだけを操作する（YES / NO を切り替える）
            if (moveX != 0 || moveY != 0)
            {
                confirmSelected_ = 1 - confirmSelected_;
            }
            if (submit)
            {
                ActivateConfirm(confirmSelected_);
            }
            else if (back)
            {
                CloseConfirm();
            }
        }
        else
        {
            if (moveX != 0 && k_Cards.Length > 0)
            {
                // 隠れているステージは飛ばす
                selected_ = NextVisible(selected_, moveX);
            }
            if (moveY < 0)
            {
                row_ = 0;
            }
            else if (moveY > 0 && k_Cards.Length > 0 && !k_Cards[selected_].IsLocked)
            {
                row_ = 1;
            }
            // ロック中のステージに移ったときは、スコア削除ボタンから外す
            if (k_Cards.Length > 0 && k_Cards[selected_].IsLocked)
            {
                row_ = 0;
            }

            if (submit)
            {
                Activate(row_ == 0 ? selected_ : k_Cards.Length + selected_);
            }
            else if (back)
            {
                SceneTransition.Go(k_TitleScene);
            }
        }

        Apply(false);
    }

    // マウスが乗ったとき（0〜: ステージ、その後: スコア削除、1000〜: 確認ポップアップ）
    public void Select(int index)
    {
        if (index >= k_ConfirmIndexBase)
        {
            confirmSelected_ = index - k_ConfirmIndexBase;
        }
        else if (!confirming_)
        {
            if (index >= k_Cards.Length)
            {
                selected_ = index - k_Cards.Length;
                row_ = 1;
            }
            else
            {
                selected_ = index;
                row_ = 0;
            }
        }
    }

    public void Activate(int index)
    {
        if (SceneTransition.IsBusy)
        {
            return;
        }

        if (index >= k_ConfirmIndexBase)
        {
            ActivateConfirm(index - k_ConfirmIndexBase);
        }
        else if (confirming_)
        {
            // ポップアップが出ている間は、後ろのボタンを押せない
            return;
        }
        else if (index >= k_Cards.Length)
        {
            OpenConfirm(index - k_Cards.Length);
        }
        else if (index >= 0)
        {
            // まだ解放されていないステージには行けない
            if (k_Cards[index].IsLocked)
            {
                return;
            }
            SceneTransition.Go(k_Cards[index].SceneName);
        }
    }

    // 「スコアを消しますか？」のポップアップを出す。最初は「NO」を選んでおく
    void OpenConfirm(int cardIndex)
    {
        deleteTarget_ = cardIndex;
        confirmText_.text = k_Cards[cardIndex].StageName + "のスコアを削除しますか？";
        confirmSelected_ = 1;
        confirming_ = true;
        confirmPanel_.SetActive(true);
    }

    void CloseConfirm()
    {
        confirming_ = false;
        confirmPanel_.SetActive(false);
    }

    void ActivateConfirm(int button)
    {
        if (button == 0)
        {
            // YES：そのステージの死亡数とクリア数を消して、表示を更新する
            SaveData.DeleteStage(k_Cards[deleteTarget_].SceneName);
            k_Cards[deleteTarget_].Refresh();
        }
        CloseConfirm();
    }

    // 選んでいるボタンを大きく・明るくする
    void Apply(bool immediate)
    {
        for (int i = 0; i < k_Cards.Length; i++)
        {
            bool on = i == selected_;
            RectTransform rect = (RectTransform)k_Cards[i].transform;
            Image image = k_Cards[i].GetComponent<Image>();
            Scale(rect, on ? k_SelectedScale : 1.0f, immediate);
            if (image != null)
            {
                image.color = on ? k_SelectedColor : k_NormalColor;
            }

            // スコア削除ボタンは、選んでいるときだけ大きく・明るくする（確認中は選んでいない扱い）
            bool deleteOn = !confirming_ && row_ == 1 && i == selected_;
            Scale(deleteButtons_[i], deleteOn ? k_DeleteSelectedScale : 1.0f, immediate);
            deleteButtons_[i].GetComponent<Image>().color = deleteOn ? k_DeleteSelectedColor : k_DeleteNormalColor;
        }

        for (int i = 0; i < confirmButtons_.Length; i++)
        {
            bool on = confirming_ && i == confirmSelected_;
            Scale(confirmButtons_[i], on ? k_DeleteSelectedScale : 1.0f, immediate);
            confirmButtons_[i].GetComponent<Image>().color = on ? k_SelectedColor : k_NormalColor;
        }
    }

    void Scale(RectTransform rect, float target, bool immediate)
    {
        float scale = immediate ? target : Mathf.Lerp(rect.localScale.x, target, 1.0f - Mathf.Exp(-k_ScaleLerpRate * Time.unscaledDeltaTime));
        rect.localScale = new Vector3(scale, scale, 1.0f);
    }

    // ---- 見た目の組み立て（シーンには置かず、コードで作る） ----

    // 各ステージのボタンの下に、スコア削除ボタンを作る
    void BuildDeleteButtons()
    {
        deleteButtons_ = new RectTransform[k_Cards.Length];
        for (int i = 0; i < k_Cards.Length; i++)
        {
            // ステージのボタンの子にするので、ステージのボタンと一緒に大きくなる
            RectTransform button = CreateButton("DeleteButton", k_Cards[i].transform, "スコア削除", 32, new Vector2(0.0f, -352.0f), new Vector2(320.0f, 56.0f), Color.white);
            MenuButtonPointer pointer = button.gameObject.AddComponent<MenuButtonPointer>();
            pointer.Init(this, k_Cards.Length + i);
            deleteButtons_[i] = button;
        }
    }

    // 「スコアを消しますか？」の確認ポップアップを作る（最初は隠しておく）
    void BuildConfirmPopup()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.transform : transform;

        Image dim = CreateImage("ConfirmPanel", root, new Color(0.0f, 0.0f, 0.0f, 0.65f));
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero;
        dim.rectTransform.offsetMax = Vector2.zero;
        confirmPanel_ = dim.gameObject;

        Image window = CreateImage("Window", confirmPanel_.transform, new Color(0.12f, 0.16f, 0.3f, 1.0f));
        window.rectTransform.sizeDelta = new Vector2(1000.0f, 460.0f);

        confirmText_ = CreateText("ConfirmText", window.transform, "スコアを削除しますか？", 52, Color.white);
        GameFonts.SetHeading(confirmText_);
        confirmText_.rectTransform.anchoredPosition = new Vector2(0.0f, 110.0f);
        confirmText_.rectTransform.sizeDelta = new Vector2(940.0f, 110.0f);
        TMP_Text note = CreateText("ConfirmNote", window.transform, "死亡数とクリア数が消えます（解放は残ります）", 38, new Color(1.0f, 1.0f, 1.0f, 0.8f));
        note.rectTransform.anchoredPosition = new Vector2(0.0f, 30.0f);
        note.rectTransform.sizeDelta = new Vector2(940.0f, 70.0f);

        confirmButtons_ = new RectTransform[2];
        string[] labels = { "はい", "いいえ" };
        for (int i = 0; i < confirmButtons_.Length; i++)
        {
            RectTransform button = CreateButton("ConfirmButton" + i, window.transform, labels[i], 54, new Vector2(-210.0f + 420.0f * i, -110.0f), new Vector2(360.0f, 100.0f), new Color(0.1f, 0.12f, 0.2f, 1.0f));
            MenuButtonPointer pointer = button.gameObject.AddComponent<MenuButtonPointer>();
            pointer.Init(this, k_ConfirmIndexBase + i);
            confirmButtons_[i] = button;
        }
        confirmPanel_.SetActive(false);
    }

    static RectTransform CreateButton(string objName, Transform parent, string label, int fontSize, Vector2 position, Vector2 size, Color textColor)
    {
        Image image = CreateImage(objName, parent, Color.white);
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        TMP_Text text = CreateText("Label", image.transform, label, fontSize, textColor);
        // 文字はボタンいっぱいに広げる
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
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
}
