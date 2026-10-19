using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 実績を達成したときに、左上にすべり込んで出る知らせ。シーンをまたいで残り、重なったときは順番に出す。
// どのシーンにも置く必要はない（最初に必要になったときに自動で作られる）
public class AchievementToast : MonoBehaviour
{
    private const float k_SlideTime = 0.4f;
    private const float k_HoldTime = 4.0f;
    private const float k_Width = 620.0f;
    private const float k_Height = 120.0f;
    // 画面の左上からの位置（ゲーム中の左上の死亡数の表示と重ならないように、少し下げる）
    private const float k_Left = 30.0f;
    private const float k_Top = 130.0f;

    private static AchievementToast instance_;

    private readonly Queue<Achievements.Def> queue_ = new Queue<Achievements.Def>();
    private RectTransform box_;
    private TMP_Text nameText_;
    private TMP_Text descText_;
    private Achievements.Def current_;
    private float timer_;

    public static void Show(Achievements.Def def)
    {
        if (instance_ == null)
        {
            GameObject obj = new GameObject("AchievementToast");
            DontDestroyOnLoad(obj);
            instance_ = obj.AddComponent<AchievementToast>();
            instance_.Build();
        }
        instance_.queue_.Enqueue(def);
        SoundEffects.Play(SoundEffects.Se.Clear);
    }

    void Update()
    {
        if (current_ == null)
        {
            if (queue_.Count == 0)
            {
                return;
            }
            current_ = queue_.Dequeue();
            nameText_.text = current_.name;
            descText_.text = current_.description;
            timer_ = 0.0f;
        }

        timer_ += Time.unscaledDeltaTime;
        float total = k_SlideTime + k_HoldTime + k_SlideTime;
        float shown;
        if (timer_ < k_SlideTime)
        {
            shown = Mathf.SmoothStep(0.0f, 1.0f, timer_ / k_SlideTime);
        }
        else if (timer_ < k_SlideTime + k_HoldTime)
        {
            shown = 1.0f;
        }
        else
        {
            shown = Mathf.SmoothStep(1.0f, 0.0f, (timer_ - k_SlideTime - k_HoldTime) / k_SlideTime);
        }
        // 画面の左の外から、すべり込む
        box_.anchoredPosition = new Vector2(Mathf.Lerp(-k_Width - 20.0f, k_Left, shown), -k_Top);

        if (timer_ >= total)
        {
            current_ = null;
            box_.anchoredPosition = new Vector2(-k_Width - 20.0f, -k_Top);
        }
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 画面の切り替えの暗幕（32000）よりは奥、ゲームの画面よりは手前
        canvas.sortingOrder = 31000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image back = CreateImage("Box", transform, new Color(0.1f, 0.13f, 0.25f, 0.95f));
        box_ = back.rectTransform;
        box_.anchorMin = new Vector2(0.0f, 1.0f);
        box_.anchorMax = new Vector2(0.0f, 1.0f);
        box_.pivot = new Vector2(0.0f, 1.0f);
        box_.sizeDelta = new Vector2(k_Width, k_Height);
        box_.anchoredPosition = new Vector2(-k_Width - 20.0f, -k_Top);

        // 左の金色の帯
        Image stripe = CreateImage("Stripe", box_, new Color(1.0f, 0.8f, 0.2f, 1.0f));
        stripe.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
        stripe.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
        stripe.rectTransform.pivot = new Vector2(0.0f, 0.5f);
        stripe.rectTransform.sizeDelta = new Vector2(12.0f, 0.0f);
        stripe.rectTransform.anchoredPosition = Vector2.zero;

        TMP_Text head = CreateText("Head", box_, "実績解除！", 26, new Color(1.0f, 0.85f, 0.3f, 1.0f));
        Place(head.rectTransform, -10.0f, 32.0f);
        nameText_ = CreateText("Name", box_, string.Empty, 42, Color.white);
        GameFonts.SetHeading(nameText_);
        Place(nameText_.rectTransform, -40.0f, 52.0f);
        descText_ = CreateText("Description", box_, string.Empty, 24, new Color(1.0f, 1.0f, 1.0f, 0.8f));
        Place(descText_.rectTransform, -92.0f, 28.0f);
    }

    // 帯の右から、右端まで。上の端から y の位置に、高さ height で置く
    private static void Place(RectTransform rect, float y, float height)
    {
        rect.anchorMin = new Vector2(0.0f, 1.0f);
        rect.anchorMax = new Vector2(1.0f, 1.0f);
        rect.pivot = new Vector2(0.0f, 1.0f);
        rect.offsetMin = new Vector2(34.0f, 0.0f);
        rect.offsetMax = new Vector2(-16.0f, 0.0f);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, int size, Color color)
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
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }
}
