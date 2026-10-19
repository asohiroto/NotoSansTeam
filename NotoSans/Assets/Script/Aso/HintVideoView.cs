using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// ヒントの動画を、画面いっぱいに流す（見た目は実行時に作る）。
// 動画は StreamingAssets フォルダの中の動画ファイル（mp4 など）を、ファイル名で指定して読み込む。
// ファイルが無いときは、「まだ置かれていません」と出す。A・B ボタン（Enter・Esc）で、いつでも閉じられる
public class HintVideoView : MonoBehaviour
{
    private GameObject root_;
    private RawImage screen_;
    private AspectRatioFitter fitter_;
    private TMP_Text message_;
    private VideoPlayer player_;
    private AudioSource audio_;
    private RenderTexture texture_;
    private string fileName_;
    private Action onClosed_;

    // 開いている間か
    public bool IsOpen { get { return root_ != null && root_.activeSelf; } }

    // canvasRoot の下に作る（最初は閉じている）。fileName は StreamingAssets の中の動画ファイル名
    public static HintVideoView Create(Transform canvasRoot, string fileName, Action onClosed)
    {
        // 画面いっぱいの大きさの入れ物（中の部品が、画面を基準に並ぶように、RectTransform にする）
        GameObject obj = new GameObject("HintVideoView", typeof(RectTransform));
        obj.transform.SetParent(canvasRoot, false);
        Stretch((RectTransform)obj.transform);
        HintVideoView view = obj.AddComponent<HintVideoView>();
        view.fileName_ = fileName;
        view.onClosed_ = onClosed;
        view.Build();
        return view;
    }

    void OnDestroy()
    {
        if (texture_ != null)
        {
            texture_.Release();
            Destroy(texture_);
        }
    }

    // 動画を流し始める
    public void Open()
    {
        root_.SetActive(true);
        string path = Path.Combine(Application.streamingAssetsPath, fileName_);
        if (!File.Exists(path))
        {
            ShowMessage("ヒントの動画は、まだありません\n（StreamingAssets/" + fileName_ + " に置くと、ここで流れます）");
            return;
        }

        message_.gameObject.SetActive(false);
        screen_.gameObject.SetActive(true);
        Achievements.Unlock("hint");
        player_.source = VideoSource.Url;
        player_.url = path;
        audio_.volume = GameSettings.SeVolume;
        player_.prepareCompleted -= OnPrepared;
        player_.prepareCompleted += OnPrepared;
        player_.errorReceived -= OnError;
        player_.errorReceived += OnError;
        player_.loopPointReached -= OnEnded;
        player_.loopPointReached += OnEnded;
        player_.Prepare();
    }

    // 閉じる（流れている動画は止める）
    public void Close()
    {
        if (player_ != null)
        {
            player_.Stop();
        }
        root_.SetActive(false);
        if (onClosed_ != null)
        {
            onClosed_();
        }
    }

    private void OnPrepared(VideoPlayer source)
    {
        if (source.height > 0)
        {
            fitter_.aspectRatio = source.width / (float)source.height;
        }
        source.Play();
    }

    private void OnEnded(VideoPlayer source)
    {
        // 最後まで流れたら、閉じる
        Close();
    }

    private void OnError(VideoPlayer source, string message)
    {
        Debug.LogWarning("HintVideoView: 動画を再生できませんでした: " + message);
        screen_.gameObject.SetActive(false);
        ShowMessage("動画を再生できませんでした\n（mp4 の H.264 形式がおすすめです）");
    }

    private void ShowMessage(string text)
    {
        message_.text = text;
        message_.gameObject.SetActive(true);
    }

    // ---- 見た目の組み立て ----

    private void Build()
    {
        // 画面全体を黒くして、後ろのポーズ画面を隠す
        Image back = new GameObject("Back").AddComponent<Image>();
        back.transform.SetParent(transform, false);
        back.color = Color.black;
        Stretch(back.rectTransform);
        root_ = back.gameObject;

        // 動画（縦横比を保って、画面に収める）
        GameObject screenObj = new GameObject("Screen", typeof(RectTransform));
        screenObj.transform.SetParent(root_.transform, false);
        Stretch((RectTransform)screenObj.transform);
        fitter_ = screenObj.AddComponent<AspectRatioFitter>();
        fitter_.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter_.aspectRatio = 16.0f / 9.0f;
        screen_ = screenObj.AddComponent<RawImage>();
        screen_.raycastTarget = false;

        texture_ = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        screen_.texture = texture_;

        player_ = gameObject.AddComponent<VideoPlayer>();
        player_.playOnAwake = false;
        player_.isLooping = false;
        player_.skipOnDrop = true;
        player_.renderMode = VideoRenderMode.RenderTexture;
        player_.targetTexture = texture_;
        // ポーズ中は時間が止まっているので、止まらない時計で動かす
        player_.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player_.audioOutputMode = VideoAudioOutputMode.AudioSource;
        audio_ = gameObject.AddComponent<AudioSource>();
        audio_.playOnAwake = false;
        audio_.spatialBlend = 0.0f;
        // ポーズ中（AudioListener.pause）でも、動画の音は鳴らす
        audio_.ignoreListenerPause = true;
        player_.EnableAudioTrack(0, true);
        player_.SetTargetAudioSource(0, audio_);

        // メッセージ（動画が無いときなど）
        message_ = new GameObject("Message").AddComponent<TextMeshProUGUI>();
        message_.transform.SetParent(root_.transform, false);
        if (GameFonts.Body != null)
        {
            message_.font = GameFonts.Body;
        }
        message_.fontSize = 56;
        message_.color = Color.white;
        message_.alignment = TextAlignmentOptions.Center;
        message_.raycastTarget = false;
        message_.textWrappingMode = TextWrappingModes.Normal;
        Stretch(message_.rectTransform);
        message_.gameObject.SetActive(false);

        // 閉じ方の説明（右下の小さな文字）
        TMP_Text hint = new GameObject("CloseHint").AddComponent<TextMeshProUGUI>();
        hint.transform.SetParent(root_.transform, false);
        if (GameFonts.Body != null)
        {
            hint.font = GameFonts.Body;
        }
        hint.text = "A・Bボタン：閉じる";
        hint.fontSize = 30;
        hint.color = new Color(1.0f, 1.0f, 1.0f, 0.55f);
        hint.alignment = TextAlignmentOptions.Right;
        hint.raycastTarget = false;
        hint.rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
        hint.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
        hint.rectTransform.pivot = new Vector2(1.0f, 0.0f);
        hint.rectTransform.anchoredPosition = new Vector2(-40.0f, 30.0f);
        hint.rectTransform.sizeDelta = new Vector2(700.0f, 50.0f);

        root_.SetActive(false);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
