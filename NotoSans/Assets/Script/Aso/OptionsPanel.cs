using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// オプション画面（BGM と SE の音量）。見た目は実行時に作る。
// 十字キー（矢印キー）の上下で項目を選び、左右で音量を変える。もどるは、A ボタンか B ボタン（Esc）
public class OptionsPanel : MonoBehaviour, IMenuSelector
{
    // 音量を 1 回で変える量と、押しっぱなしで繰り返す間隔
    private const float k_Step = 0.05f;
    private const float k_RepeatDelay = 0.35f;
    private const float k_RepeatInterval = 0.08f;

    // 項目：0 = BGM、1 = SE、2 = もどる
    private const int k_RowBgm = 0;
    private const int k_RowSe = 1;
    private const int k_RowBack = 2;

    private static readonly Color k_SelectedColor = new Color(1.0f, 0.92f, 0.45f, 1.0f);
    private static readonly Color k_NormalColor = new Color(1.0f, 1.0f, 1.0f, 0.85f);

    private GameObject root_;
    private Slider[] sliders_ = new Slider[2];
    private TMP_Text[] valueTexts_ = new TMP_Text[2];
    private TMP_Text[] labels_ = new TMP_Text[3];
    private RectTransform backButton_;
    private int selected_ = 0;
    private float holdTimer_ = 0.0f;
    private int holdDirection_ = 0;
    private bool building_ = false;

    // 開いている間か
    public bool IsOpen { get { return root_ != null && root_.activeSelf; } }
    // 最後に開いた・閉じたフレーム（開いたボタンの入力を、同じフレームに拾わないための目印）
    public int LastChangeFrame { get; private set; }

    // canvasRoot の下に、オプション画面を作る（最初は閉じている）
    public static OptionsPanel Create(Transform canvasRoot)
    {
        // 画面いっぱいの大きさの入れ物（中の部品が、画面を基準に並ぶように、RectTransform にする）
        GameObject obj = new GameObject("OptionsPanel", typeof(RectTransform));
        obj.transform.SetParent(canvasRoot, false);
        Stretch((RectTransform)obj.transform);
        OptionsPanel panel = obj.AddComponent<OptionsPanel>();
        panel.Build();
        return panel;
    }

    public void Open()
    {
        selected_ = 0;
        Refresh();
        root_.SetActive(true);
        LastChangeFrame = Time.frameCount;
    }

    public void Close()
    {
        root_.SetActive(false);
        LastChangeFrame = Time.frameCount;
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount <= LastChangeFrame)
        {
            return;
        }

        int moveY = 0;
        int moveX = 0;
        bool heldLeft = false;
        bool heldRight = false;
        bool submit = false;
        bool back = false;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.up.wasPressedThisFrame) { moveY = -1; }
            if (pad.dpad.down.wasPressedThisFrame) { moveY = 1; }
            heldLeft |= pad.dpad.left.isPressed;
            heldRight |= pad.dpad.right.isPressed;
            if (pad.buttonSouth.wasPressedThisFrame) { submit = true; }
            if (pad.buttonEast.wasPressedThisFrame) { back = true; }
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame) { moveY = -1; }
            if (keyboard.downArrowKey.wasPressedThisFrame) { moveY = 1; }
            heldLeft |= keyboard.leftArrowKey.isPressed;
            heldRight |= keyboard.rightArrowKey.isPressed;
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { submit = true; }
            if (keyboard.escapeKey.wasPressedThisFrame) { back = true; }
        }

        if (moveY != 0)
        {
            selected_ = (selected_ + moveY + 3) % 3;
            SoundEffects.Play(SoundEffects.Se.Change);
        }

        // 左右：押した瞬間に1段階、押しっぱなしで少し待ってから繰り返す
        int direction = heldLeft == heldRight ? 0 : (heldRight ? 1 : -1);
        if (direction != 0 && selected_ != k_RowBack)
        {
            if (direction != holdDirection_)
            {
                holdDirection_ = direction;
                holdTimer_ = k_RepeatDelay;
                moveX = direction;
            }
            else
            {
                holdTimer_ -= Time.unscaledDeltaTime;
                if (holdTimer_ <= 0.0f)
                {
                    holdTimer_ += k_RepeatInterval;
                    moveX = direction;
                }
            }
        }
        else
        {
            holdDirection_ = 0;
        }

        if (moveX != 0)
        {
            AddVolume(selected_, moveX * k_Step);
        }

        if (back || (submit && selected_ == k_RowBack))
        {
            SoundEffects.Play(SoundEffects.Se.Cancel);
            Close();
            return;
        }
        Refresh();
    }

    // 音量を変える（row：0 = BGM、1 = SE）
    private void AddVolume(int row, float delta)
    {
        if (row == k_RowBgm)
        {
            GameSettings.BgmVolume = Mathf.Round((GameSettings.BgmVolume + delta) / k_Step) * k_Step;
        }
        else if (row == k_RowSe)
        {
            GameSettings.SeVolume = Mathf.Round((GameSettings.SeVolume + delta) / k_Step) * k_Step;
            // 変えた音量が分かるよう、SE は鳴らして聞かせる
            SoundEffects.Play(SoundEffects.Se.Change);
        }
        Refresh();
    }

    // 画面の表示を、いまの設定と選んでいる項目に合わせる
    private void Refresh()
    {
        building_ = true;
        float[] volumes = { GameSettings.BgmVolume, GameSettings.SeVolume };
        for (int i = 0; i < 2; i++)
        {
            sliders_[i].SetValueWithoutNotify(volumes[i]);
            valueTexts_[i].text = Mathf.RoundToInt(volumes[i] * 100.0f) + "%";
            bool on = selected_ == i;
            labels_[i].color = on ? k_SelectedColor : k_NormalColor;
            valueTexts_[i].color = on ? k_SelectedColor : k_NormalColor;
            labels_[i].fontStyle = on ? FontStyles.Underline : FontStyles.Normal;
        }
        bool backOn = selected_ == k_RowBack;
        backButton_.localScale = Vector3.one * (backOn ? 1.15f : 1.0f);
        backButton_.GetComponent<Image>().color = backOn ? Color.white : new Color(0.7f, 0.72f, 0.8f, 1.0f);
        building_ = false;
    }

    // マウスが乗ったとき（0・1 = 音量の行、2 = もどる）
    public void Select(int index)
    {
        if (index >= 0 && index < 3 && index != selected_)
        {
            selected_ = index;
            SoundEffects.Play(SoundEffects.Se.Change);
            Refresh();
        }
    }

    public void Activate(int index)
    {
        if (index == k_RowBack)
        {
            SoundEffects.Play(SoundEffects.Se.Cancel);
            Close();
        }
    }

    // スライダーをマウスで動かしたとき
    private void OnSliderChanged(int row, float value)
    {
        if (building_)
        {
            return;
        }
        selected_ = row;
        float v = Mathf.Round(value / k_Step) * k_Step;
        if (row == k_RowBgm) { GameSettings.BgmVolume = v; }
        else { GameSettings.SeVolume = v; SoundEffects.Play(SoundEffects.Se.Change); }
        Refresh();
    }

    // ---- 見た目の組み立て ----

    private void Build()
    {
        // 画面全体を暗くして、後ろのボタンを押せなくする
        Image dim = CreateImage("Dim", transform, new Color(0.0f, 0.0f, 0.0f, 0.7f));
        Stretch(dim.rectTransform);
        root_ = dim.gameObject;

        Image window = CreateImage("Window", root_.transform, new Color(0.12f, 0.16f, 0.3f, 1.0f));
        window.rectTransform.sizeDelta = new Vector2(1200.0f, 660.0f);
        window.raycastTarget = false;

        TMP_Text title = CreateText("Title", window.transform, "オプション", 84, Color.white);
        title.rectTransform.anchoredPosition = new Vector2(0.0f, 240.0f);
        title.rectTransform.sizeDelta = new Vector2(1000.0f, 120.0f);
        GameFonts.SetHeading(title);

        string[] names = { "BGM音量", "SE音量" };
        for (int i = 0; i < 2; i++)
        {
            float y = 70.0f - 130.0f * i;
            labels_[i] = CreateText("Label" + i, window.transform, names[i], 56, k_NormalColor);
            labels_[i].rectTransform.anchoredPosition = new Vector2(-330.0f, y);
            labels_[i].rectTransform.sizeDelta = new Vector2(360.0f, 80.0f);
            labels_[i].alignment = TextAlignmentOptions.Left;

            sliders_[i] = CreateSlider("Slider" + i, window.transform, new Vector2(60.0f, y), new Vector2(520.0f, 56.0f), i);

            valueTexts_[i] = CreateText("Value" + i, window.transform, "100%", 56, k_NormalColor);
            valueTexts_[i].rectTransform.anchoredPosition = new Vector2(440.0f, y);
            valueTexts_[i].rectTransform.sizeDelta = new Vector2(200.0f, 80.0f);
            valueTexts_[i].alignment = TextAlignmentOptions.Right;

            // マウスが行に乗ったら、その項目を選ぶ
            MenuButtonPointer pointer = labels_[i].gameObject.AddComponent<MenuButtonPointer>();
            pointer.Init(this, i);
            labels_[i].raycastTarget = true;
        }

        Image back = CreateImage("BackButton", window.transform, Color.white);
        back.rectTransform.anchoredPosition = new Vector2(0.0f, -215.0f);
        back.rectTransform.sizeDelta = new Vector2(380.0f, 96.0f);
        backButton_ = back.rectTransform;
        TMP_Text backLabel = CreateText("Label", back.transform, "もどる", 56, new Color(0.1f, 0.12f, 0.2f, 1.0f));
        Stretch(backLabel.rectTransform);
        MenuButtonPointer backPointer = back.gameObject.AddComponent<MenuButtonPointer>();
        backPointer.Init(this, k_RowBack);

        root_.SetActive(false);
    }

    private Slider CreateSlider(string objName, Transform parent, Vector2 position, Vector2 size, int row)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        // 土台の細い棒
        Image back = CreateImage("Background", obj.transform, new Color(0.05f, 0.07f, 0.14f, 1.0f));
        back.rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
        back.rectTransform.anchorMax = new Vector2(1.0f, 0.5f);
        back.rectTransform.sizeDelta = new Vector2(0.0f, 22.0f);

        // 満たされた部分
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(obj.transform, false);
        RectTransform fillAreaRect = (RectTransform)fillArea.transform;
        fillAreaRect.anchorMin = new Vector2(0.0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1.0f, 0.5f);
        fillAreaRect.sizeDelta = new Vector2(-30.0f, 22.0f);
        Image fill = CreateImage("Fill", fillArea.transform, new Color(1.0f, 0.78f, 0.2f, 1.0f));
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
        fill.rectTransform.sizeDelta = new Vector2(10.0f, 0.0f);

        // つまみ
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(obj.transform, false);
        RectTransform handleAreaRect = (RectTransform)handleArea.transform;
        Stretch(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(15.0f, 0.0f);
        handleAreaRect.offsetMax = new Vector2(-15.0f, 0.0f);
        Image handle = CreateImage("Handle", handleArea.transform, Color.white);
        handle.rectTransform.sizeDelta = new Vector2(34.0f, 64.0f);

        Slider slider = obj.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0.0f;
        slider.maxValue = 1.0f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(value => OnSliderChanged(row, value));
        return slider;
    }

    private static Image CreateImage(string objName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(string objName, Transform parent, string value, int fontSize, Color color)
    {
        GameObject obj = new GameObject(objName);
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
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
