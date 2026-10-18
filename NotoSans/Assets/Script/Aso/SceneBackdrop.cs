using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// タイトル・ステージ選択・クリアの背景に、ステージの一部（Resources/Backgrounds の画像）を出す（シーンに置く必要はない）。
//  ・タイトル：ステージ1　・クリア：ステージ2　・ステージ選択：選んでいるステージ（ModeSelectMenu が Show で切り替える）
// 画像は、Resources/Backgrounds/ の中の BgStage1 / BgStage2 / BgExtra。差し替えるときは、同じ名前で置き換える
public class SceneBackdrop : MonoBehaviour
{
    // 背景を暗くする濃さ（文字やボタンを見やすくする）
    private const float k_Dim = 0.45f;
    // 切り替えるときの、なめらかさ（秒）
    private const float k_FadeTime = 0.35f;
    // カメラからの距離（ロゴやUnityちゃんより奥）
    private const float k_PlaneDistance = 30.0f;

    private RawImage front_;
    private RawImage back_;
    private string current_ = string.Empty;
    private float fade_ = 1.0f;

    private static SceneBackdrop instance_;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        // 「ドメインの再読み込みをしない」設定でも、登録が重ならないようにする
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Install(SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install(scene.name);
    }

    private static void Install(string sceneName)
    {
        string image = null;
        switch (sceneName)
        {
            case "TitleScene": image = "BgStage1"; break;
            case "GameClearScene": image = "BgStage2"; break;
            // ステージ選択は、ModeSelectMenu が選んでいるカードに合わせて切り替える（最初は1つ目）
            case "ModeSelectScene": image = "BgStage1"; break;
        }
        if (image == null || Camera.main == null)
        {
            instance_ = null;
            return;
        }
        if (FindAnyObjectByType<SceneBackdrop>() != null)
        {
            return;
        }

        // これまでの灰色の背景（Cube）は隠す
        GameObject cube = GameObject.Find("Cube");
        if (cube != null)
        {
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        GameObject obj = new GameObject("SceneBackdrop", typeof(RectTransform));
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = k_PlaneDistance;
        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);

        instance_ = obj.AddComponent<SceneBackdrop>();
        instance_.back_ = CreateImage(obj.transform, "Back");
        instance_.front_ = CreateImage(obj.transform, "Front");
        CreateDim(obj.transform);
        instance_.SetImmediate(image);
    }

    // 背景を切り替える（name は BgStage1 / BgStage2 / BgExtra）
    public static void Show(string name)
    {
        if (instance_ != null)
        {
            instance_.Change(name);
        }
    }

    // ステージのシーン名に合う背景の名前
    public static string NameForStage(string sceneName)
    {
        switch (sceneName)
        {
            case "kiraScene": return "BgStage1";
            case "HirakiNewScene": return "BgStage2";
            case "ibukiScene1": return "BgExtra";
            default: return "BgStage1";
        }
    }

    private static RawImage CreateImage(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RawImage image = obj.AddComponent<RawImage>();
        image.raycastTarget = false;
        Stretch(image.rectTransform);
        return image;
    }

    private static void CreateDim(Transform parent)
    {
        GameObject obj = new GameObject("Dim", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image dim = obj.AddComponent<Image>();
        dim.color = new Color(0.0f, 0.0f, 0.0f, k_Dim);
        dim.raycastTarget = false;
        Stretch(dim.rectTransform);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetImmediate(string name)
    {
        current_ = name;
        front_.texture = Resources.Load<Texture2D>("Backgrounds/" + name);
        front_.color = Color.white;
        back_.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        fade_ = 1.0f;
    }

    private void Change(string name)
    {
        if (name == current_)
        {
            return;
        }
        // 今の絵を奥に回して、新しい絵を手前でゆっくり出す
        back_.texture = front_.texture;
        back_.color = Color.white;
        front_.texture = Resources.Load<Texture2D>("Backgrounds/" + name);
        front_.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        current_ = name;
        fade_ = 0.0f;
    }

    void Update()
    {
        if (fade_ >= 1.0f)
        {
            return;
        }
        fade_ = Mathf.Min(1.0f, fade_ + Time.unscaledDeltaTime / k_FadeTime);
        front_.color = new Color(1.0f, 1.0f, 1.0f, fade_);
    }

    void OnDestroy()
    {
        if (instance_ == this)
        {
            instance_ = null;
        }
    }
}
