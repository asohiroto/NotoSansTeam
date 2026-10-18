using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 操作説明の画像を、画面いっぱいに出す（見た目は実行時に作る）。
// 画像は Resources/ControlsGuide（png / jpg）。差し替えるときは、同じ名前で置き換える。
// A・B ボタン（Enter・Space・Esc）か、クリックで閉じる
public class ControlsGuideView : MonoBehaviour
{
    private const string k_ImageResource = "ControlsGuide";

    private GameObject root_;

    public bool IsOpen { get { return root_ != null && root_.activeSelf; } }
    public int LastChangeFrame { get; private set; }

    public static ControlsGuideView Create(Transform canvasRoot)
    {
        GameObject obj = new GameObject("ControlsGuideView", typeof(RectTransform));
        obj.transform.SetParent(canvasRoot, false);
        Stretch((RectTransform)obj.transform);
        ControlsGuideView view = obj.AddComponent<ControlsGuideView>();
        view.Build();
        return view;
    }

    public void Open()
    {
        root_.SetActive(true);
        root_.transform.SetAsLastSibling();
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
        bool close = false;
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            close |= pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            close |= keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame;
        }
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            close |= mouse.leftButton.wasPressedThisFrame;
        }
        if (close)
        {
            SoundEffects.Play(SoundEffects.Se.Select);
            Close();
        }
    }

    private void Build()
    {
        // 暗い背景
        Image back = new GameObject("Back", typeof(RectTransform)).AddComponent<Image>();
        back.transform.SetParent(transform, false);
        back.color = new Color(0.0f, 0.0f, 0.0f, 0.85f);
        Stretch(back.rectTransform);
        root_ = gameObject;

        Texture2D texture = Resources.Load<Texture2D>(k_ImageResource);
        if (texture != null)
        {
            RawImage image = new GameObject("Image", typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(transform, false);
            image.texture = texture;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            // 画面（1920x1080）に収まる、いちばん大きな大きさ
            float scale = Mathf.Min(1800.0f / texture.width, 960.0f / texture.height);
            rect.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
        }
        else
        {
            TMP_Text message = CreateText("Message", "操作説明の画像が見つかりません\n（Assets/Resources/ControlsGuide.png に置いてください）", 44, Color.white);
            Stretch(message.rectTransform);
            message.alignment = TextAlignmentOptions.Center;
        }

        TMP_Text hint = CreateText("CloseHint", "A・Bボタン：閉じる", 30, new Color(1.0f, 1.0f, 1.0f, 0.6f));
        hint.alignment = TextAlignmentOptions.Right;
        hint.rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
        hint.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
        hint.rectTransform.pivot = new Vector2(1.0f, 0.0f);
        hint.rectTransform.anchoredPosition = new Vector2(-40.0f, 20.0f);
        hint.rectTransform.sizeDelta = new Vector2(700.0f, 50.0f);

        root_.SetActive(false);
    }

    private TMP_Text CreateText(string name, string value, int size, Color color)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(transform, false);
        if (GameFonts.Body != null)
        {
            text.font = GameFonts.Body;
        }
        text.text = value;
        text.fontSize = size;
        text.color = color;
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
