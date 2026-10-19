using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 実績の一覧を、画面いっぱいに出す（見た目は実行時に作る）。達成したものは明るく、まだのものは暗く出る。
// 実績が多いので、ページに分けてある（左右キー・十字キー、または画面の「前へ」「次へ」ボタンで切り替える）。
// 開くたびに作り直すので、いまの達成の状態が出る。A・B ボタン（Enter・Space・Esc）か、「閉じる」ボタンで閉じる
public class AchievementsPanel : MonoBehaviour
{
    private const int k_RowsPerColumn = 6;
    private const float k_RowWidth = 860.0f;
    private const float k_RowHeight = 100.0f;
    private const float k_RowSpacing = 112.0f;

    private static readonly Color k_UnlockedColor = new Color(0.2f, 0.27f, 0.48f, 1.0f);
    private static readonly Color k_LockedColor = new Color(0.14f, 0.15f, 0.2f, 1.0f);
    private static readonly Color k_GoldColor = new Color(1.0f, 0.85f, 0.3f, 1.0f);

    private GameObject list_;
    private TMP_Text countText_;
    private TMP_Text pageText_;
    private int page_;

    public bool IsOpen { get { return gameObject.activeSelf; } }
    public int LastChangeFrame { get; private set; }

    public static AchievementsPanel Create(Transform canvasRoot)
    {
        GameObject obj = new GameObject("AchievementsPanel", typeof(RectTransform));
        obj.transform.SetParent(canvasRoot, false);
        Stretch((RectTransform)obj.transform);
        AchievementsPanel panel = obj.AddComponent<AchievementsPanel>();
        panel.BuildFrame();
        obj.SetActive(false);
        return panel;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        page_ = 0;
        Refresh();
        LastChangeFrame = Time.frameCount;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        LastChangeFrame = Time.frameCount;
    }

    private void ChangePage(int direction)
    {
        int count = Achievements.PageSizes.Length;
        page_ = (page_ + direction + count) % count;
        SoundEffects.Play(SoundEffects.Se.Change);
        Refresh();
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount <= LastChangeFrame)
        {
            return;
        }
        bool close = false;
        int move = 0;
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            close |= pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
            if (pad.dpad.left.wasPressedThisFrame) { move = -1; }
            if (pad.dpad.right.wasPressedThisFrame) { move = 1; }
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            close |= keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame;
            if (keyboard.leftArrowKey.wasPressedThisFrame) { move = -1; }
            if (keyboard.rightArrowKey.wasPressedThisFrame) { move = 1; }
        }
        if (close)
        {
            SoundEffects.Play(SoundEffects.Se.Cancel);
            Close();
        }
        else if (move != 0)
        {
            ChangePage(move);
        }
    }

    // 背景・見出し・ボタン・案内（最初に1度だけ作る）
    private void BuildFrame()
    {
        Image back = CreateImage("Back", transform, new Color(0.0f, 0.0f, 0.0f, 0.88f));
        Stretch(back.rectTransform);

        TMP_Text title = CreateText("Title", transform, "実績", 90, Color.white, TextAlignmentOptions.Center);
        GameFonts.SetHeading(title);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.0f, 450.0f), new Vector2(800.0f, 120.0f));
        countText_ = CreateText("Count", transform, string.Empty, 34, k_GoldColor, TextAlignmentOptions.Center);
        SetRect(countText_.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.0f, 375.0f), new Vector2(800.0f, 46.0f));

        // ページの切り替え（◀ ページ名 ▶）
        pageText_ = CreateText("Page", transform, string.Empty, 40, Color.white, TextAlignmentOptions.Center);
        GameFonts.SetHeading(pageText_);
        SetRect(pageText_.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.0f, -380.0f), new Vector2(700.0f, 60.0f));
        CreateButton("PrevButton", "前へ", new Vector2(-460.0f, -380.0f), new Vector2(180.0f, 70.0f), () => ChangePage(-1));
        CreateButton("NextButton", "次へ", new Vector2(460.0f, -380.0f), new Vector2(180.0f, 70.0f), () => ChangePage(1));
        CreateButton("CloseButton", "閉じる", new Vector2(0.0f, -460.0f), new Vector2(260.0f, 60.0f), () => { SoundEffects.Play(SoundEffects.Se.Cancel); Close(); });


        TMP_Text hint = CreateText("Hint", transform, "左右：ページ切り替え　　A・Bボタン：閉じる", 28, new Color(1.0f, 1.0f, 1.0f, 0.6f), TextAlignmentOptions.Right);
        hint.rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
        hint.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
        hint.rectTransform.pivot = new Vector2(1.0f, 0.0f);
        hint.rectTransform.anchoredPosition = new Vector2(-30.0f, 16.0f);
        hint.rectTransform.sizeDelta = new Vector2(900.0f, 44.0f);
    }

    // いまのページの一覧を作り直す
    private void Refresh()
    {
        if (list_ != null)
        {
            Destroy(list_);
        }
        list_ = new GameObject("List", typeof(RectTransform));
        list_.transform.SetParent(transform, false);
        Stretch((RectTransform)list_.transform);
        // 閉じるボタンなどより奥に置く
        list_.transform.SetSiblingIndex(1);

        countText_.text = "達成  " + Achievements.UnlockedCount + " / " + Achievements.All.Length;
        pageText_.text = Achievements.PageTitles[page_] + "  (" + (page_ + 1) + "/" + Achievements.PageSizes.Length + ")";

        int start = 0;
        for (int p = 0; p < page_; p++)
        {
            start += Achievements.PageSizes[p];
        }
        int size = Achievements.PageSizes[page_];

        for (int n = 0; n < size && start + n < Achievements.All.Length; n++)
        {
            Achievements.Def def = Achievements.All[start + n];
            bool unlocked = SaveData.IsAchievementUnlocked(def.id);
            int column = n / k_RowsPerColumn;
            int row = n % k_RowsPerColumn;
            float x = (column == 0 ? -1.0f : 1.0f) * (k_RowWidth * 0.5f + 20.0f);
            float y = 285.0f - k_RowSpacing * row;

            Image box = CreateImage("Row" + n, list_.transform, unlocked ? k_UnlockedColor : k_LockedColor);
            box.raycastTarget = false;
            SetRect(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(k_RowWidth, k_RowHeight));

            TMP_Text label = CreateText("Name", box.transform, def.name, 40, unlocked ? Color.white : new Color(1.0f, 1.0f, 1.0f, 0.45f), TextAlignmentOptions.Left);
            GameFonts.SetHeading(label);
            Fill(label.rectTransform, 24.0f, -190.0f, -6.0f, -52.0f);
            TMP_Text desc = CreateText("Description", box.transform, def.description, 26, new Color(1.0f, 1.0f, 1.0f, unlocked ? 0.85f : 0.4f), TextAlignmentOptions.Left);
            Fill(desc.rectTransform, 24.0f, -190.0f, -52.0f, -92.0f);
            TMP_Text state = CreateText("State", box.transform, unlocked ? "達成！" : "未達成", 32, unlocked ? k_GoldColor : new Color(1.0f, 1.0f, 1.0f, 0.35f), TextAlignmentOptions.Right);
            state.rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
            state.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            state.rectTransform.pivot = new Vector2(1.0f, 0.5f);
            state.rectTransform.sizeDelta = new Vector2(170.0f, 0.0f);
            state.rectTransform.anchoredPosition = new Vector2(-20.0f, 0.0f);
        }
    }

    private void CreateButton(string name, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreateImage(name, transform, new Color(0.2f, 0.45f, 0.8f, 1.0f));
        SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), position, size);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        TMP_Text label = CreateText("Label", image.transform, text, 36, Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
    }

    // 親の左から left、右から right（負の値で内側）、上から top、下から bottom の位置に広げる（top・bottom は上の端からの距離）
    private static void Fill(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = new Vector2(0.0f, 1.0f);
        rect.anchorMax = new Vector2(1.0f, 1.0f);
        rect.pivot = new Vector2(0.0f, 1.0f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, int size, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        if (GameFonts.Body != null)
        {
            text.font = GameFonts.Body;
        }
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
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

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
