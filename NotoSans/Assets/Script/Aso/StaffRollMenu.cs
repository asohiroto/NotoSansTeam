using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// スタッフロール。Resources/StaffRoll.txt の内容を、画面の下から上へ流す（見た目は実行時に作る）。
// テキストの書き方は StaffRoll.txt の先頭に書いてある。A ボタン（Enter / スペース / 下キー）を押している間は速く流れ、
// B ボタン（Esc / スタート）で飛ばしてタイトルへ戻る。最後まで流れたら、少し待ってタイトルへ戻る
public class StaffRollMenu : MonoBehaviour
{
    // 読み込むテキスト（Resources フォルダの中の名前。拡張子は付けない）
    [SerializeField] private string k_TextResource = "StaffRoll";
    // 流れる速さ（画面の高さが 1080 のときの 1 秒あたりのピクセル数）。押している間は k_FastMultiplier 倍
    [SerializeField] private float k_ScrollSpeed = 110.0f;
    [SerializeField] private float k_FastMultiplier = 4.0f;
    // 終わったあとに戻るシーンと、戻るまでの待ち時間（秒）
    [SerializeField] private string k_ReturnScene = "TitleScene";
    [SerializeField] private float k_EndWait = 4.0f;

    // 文字の大きさと色
    private const int k_HeadingSize = 76;
    private const int k_SubHeadingSize = 56;
    private const int k_NormalSize = 46;
    private static readonly Color k_HeadingColor = new Color(1.0f, 0.85f, 0.3f, 1.0f);
    private static readonly Color k_SubHeadingColor = new Color(0.65f, 0.9f, 1.0f, 1.0f);

    private RectTransform content_;
    private float contentHeight_;
    private float position_;
    private TMP_Text endText_;
    private float endTimer_;
    private bool finished_;
    private bool leaving_;

    void Start()
    {
        string text = LoadText();
        BuildUi(text);
        position_ = -1080.0f;
        content_.anchoredPosition = new Vector2(0.0f, position_);
    }

    void Update()
    {
        if (leaving_ || SceneTransition.IsBusy)
        {
            return;
        }

        bool fast = false;
        bool skip = false;
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            fast |= pad.buttonSouth.isPressed || pad.dpad.down.isPressed;
            skip |= pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            fast |= keyboard.enterKey.isPressed || keyboard.spaceKey.isPressed || keyboard.downArrowKey.isPressed;
            skip |= keyboard.escapeKey.wasPressedThisFrame;
        }

        if (skip)
        {
            Leave();
            return;
        }

        if (!finished_)
        {
            position_ += k_ScrollSpeed * (fast ? k_FastMultiplier : 1.0f) * Time.unscaledDeltaTime;
            content_.anchoredPosition = new Vector2(0.0f, position_);
            // 最後の行が画面の上に抜けたら、終わりの文字を出す
            if (position_ >= contentHeight_ + 200.0f)
            {
                finished_ = true;
                endText_.gameObject.SetActive(true);
                content_.gameObject.SetActive(false);
            }
        }
        else
        {
            endTimer_ += Time.unscaledDeltaTime;
            if (endTimer_ >= k_EndWait)
            {
                Leave();
            }
        }
    }

    private void Leave()
    {
        leaving_ = true;
        SceneTransition.Go(k_ReturnScene);
    }

    private string LoadText()
    {
        TextAsset asset = Resources.Load<TextAsset>(k_TextResource);
        if (asset == null)
        {
            Debug.LogWarning("StaffRollMenu: Resources/" + k_TextResource + ".txt が見つかりません");
            return "# スタッフロール\n\nThank you for playing!";
        }
        return asset.text;
    }

    // ---- 見た目の組み立て ----

    private void BuildUi(string text)
    {
        GameObject canvasObj = new GameObject("StaffRollCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        // Unityちゃんが文字の手前に出られるよう、カメラに映る方式にする（文字は、カメラから遠い面に置く）
        if (Camera.main != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 12.0f;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 背景は真っ黒
        Image back = new GameObject("Back").AddComponent<Image>();
        back.transform.SetParent(canvasObj.transform, false);
        back.color = Color.black;
        RectTransform backRect = back.rectTransform;
        backRect.anchorMin = Vector2.zero;
        backRect.anchorMax = Vector2.one;
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;

        // 流れる内容（上の中央を基準にして、下へ並べる）
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(canvasObj.transform, false);
        content_ = (RectTransform)contentObj.transform;
        content_.anchorMin = new Vector2(0.5f, 1.0f);
        content_.anchorMax = new Vector2(0.5f, 1.0f);
        content_.pivot = new Vector2(0.5f, 1.0f);
        content_.sizeDelta = new Vector2(1500.0f, 100.0f);

        float y = 0.0f;
        foreach (string rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            string line = rawLine.TrimEnd();
            // コメント行（// から始まる行）は、画面に出さない
            if (line.TrimStart().StartsWith("//"))
            {
                continue;
            }
            if (line.Length == 0)
            {
                y += 40.0f;
                continue;
            }

            int size = k_NormalSize;
            Color color = Color.white;
            bool heading = false;
            string body = line;
            if (line.StartsWith("# "))
            {
                // 見出し：前に少し空けて、大きく
                y += 90.0f;
                size = k_HeadingSize;
                color = k_HeadingColor;
                heading = true;
                body = line.Substring(2);
            }
            else if (line.StartsWith("## "))
            {
                y += 30.0f;
                size = k_SubHeadingSize;
                color = k_SubHeadingColor;
                body = line.Substring(3);
            }

            TMP_Text label = CreateText(content_, body, size, color);
            if (heading)
            {
                GameFonts.SetHeading(label);
            }
            float height = size * 1.45f;
            label.rectTransform.anchoredPosition = new Vector2(0.0f, -y);
            label.rectTransform.sizeDelta = new Vector2(1500.0f, height);
            y += height;
        }
        contentHeight_ = y;
        content_.sizeDelta = new Vector2(1500.0f, y);

        // 最後まで流れたあとの文字
        endText_ = CreateText(canvasObj.transform, "Thank you for playing!", 96, Color.white);
        endText_.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        endText_.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        endText_.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        endText_.rectTransform.anchoredPosition = Vector2.zero;
        endText_.rectTransform.sizeDelta = new Vector2(1700.0f, 160.0f);
        GameFonts.SetHeading(endText_);
        endText_.gameObject.SetActive(false);

        // 操作の説明（右下の小さな文字）
        TMP_Text hint = CreateText(canvasObj.transform, "Aボタン：早送り　　Bボタン：飛ばす", 30, new Color(1.0f, 1.0f, 1.0f, 0.45f));
        hint.rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
        hint.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
        hint.rectTransform.pivot = new Vector2(1.0f, 0.0f);
        hint.rectTransform.anchoredPosition = new Vector2(-40.0f, 30.0f);
        hint.rectTransform.sizeDelta = new Vector2(900.0f, 50.0f);
        hint.alignment = TextAlignmentOptions.Right;
    }

    private static TMP_Text CreateText(Transform parent, string value, int fontSize, Color color)
    {
        GameObject obj = new GameObject("Line");
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
        // 位置の基準は上の中央
        text.rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
        text.rectTransform.pivot = new Vector2(0.5f, 1.0f);
        return text;
    }
}
