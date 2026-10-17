using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// シーン遷移の演出。上からUnityちゃんを降らせながら、ゆっくり黒くフェードアウトし、
// 読み込み後にフェードインする。シーンに何も置かなくても SceneTransition.Go("シーン名") で使える
// （演出用のオブジェクトは最初に呼んだときに自動で作られ、シーンをまたいで残る）
public class SceneTransition : MonoBehaviour
{
    // 降らせるキャラクター（Resources/TransitionUnityChan）
    private const string k_CharacterName = "TransitionUnityChan";

    // フェードアウト（暗くなる）にかける時間（秒）。ゆっくりにする
    private const float k_FadeOutTime = 2.0f;
    // 真っ暗になってから読み込みを始めるまでの間（秒）
    private const float k_HoldTime = 0.2f;
    // フェードイン（明るくなる）にかける時間（秒）
    private const float k_FadeInTime = 1.5f;

    // 降らせる人数と、1人が画面を落ちきるまでの時間（秒）
    private const int k_CharacterCount = 6;
    private const float k_FallTime = 1.2f;
    // キャラクターの大きさ（カメラの高さに対する割合）
    private const float k_CharacterHeightRate = 0.28f;

    // 演出用カメラの高さの半分（正射影）
    private const float k_CameraHalfHeight = 5.0f;
    // 演出用カメラを置く場所（ゲーム本編のオブジェクトから離れた場所）
    private static readonly Vector3 k_StagePosition = new Vector3(0.0f, -1000.0f, 0.0f);

    private static SceneTransition instance_;

    // 遷移中か（メニューの二重入力を止める用）
    public static bool IsBusy { get { return instance_ != null && instance_.busy_; } }

    private bool busy_ = false;
    private Image fade_;
    private RawImage characterView_;
    private Camera camera_;
    private RenderTexture texture_;
    private Transform[] characters_;
    private float[] startDelay_;
    private float[] startX_;
    private float[] roll_;

    // 指定のシーンへ、演出つきで遷移する
    public static void Go(string sceneName)
    {
        if (IsBusy)
        {
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("SceneTransition: シーン「" + sceneName + "」が Build Settings に入っていません");
            return;
        }

        if (instance_ == null)
        {
            GameObject obj = new GameObject("SceneTransition");
            DontDestroyOnLoad(obj);
            instance_ = obj.AddComponent<SceneTransition>();
        }
        instance_.StartCoroutine(instance_.Run(sceneName));
    }

    IEnumerator Run(string sceneName)
    {
        busy_ = true;
        Build();
        fade_.gameObject.SetActive(true);
        SetFade(0.0f);
        characterView_.gameObject.SetActive(true);

        // フェードアウト：Unityちゃんを降らせながら、ゆっくり黒くする
        float time = 0.0f;
        while (time < k_FadeOutTime)
        {
            time += Time.unscaledDeltaTime;
            SetFade(Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(time / k_FadeOutTime)));
            UpdateCharacters(time);
            yield return null;
        }
        SetFade(1.0f);
        characterView_.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(k_HoldTime);

        // 読み込み（真っ暗のまま待つ）
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone)
        {
            yield return null;
        }
        // 読み込み直後の1フレームは重いので、飛ばしてから明るくする
        yield return null;

        // フェードイン：ゆっくり明るくする
        time = 0.0f;
        while (time < k_FadeInTime)
        {
            time += Time.unscaledDeltaTime;
            SetFade(1.0f - Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(time / k_FadeInTime)));
            yield return null;
        }
        SetFade(0.0f);
        fade_.gameObject.SetActive(false);
        busy_ = false;
    }

    void SetFade(float alpha)
    {
        Color color = fade_.color;
        color.a = alpha;
        fade_.color = color;
    }

    // 上から降ってくる位置と向きを決める
    void UpdateCharacters(float time)
    {
        float top = k_StagePosition.y + k_CameraHalfHeight * 1.4f;
        float bottom = k_StagePosition.y - k_CameraHalfHeight * 1.4f;
        for (int i = 0; i < characters_.Length; i++)
        {
            float rate = Mathf.Clamp01((time - startDelay_[i]) / k_FallTime);
            // 落ちるほど速くなる
            float y = Mathf.Lerp(top, bottom, rate * rate);
            characters_[i].position = new Vector3(k_StagePosition.x + startX_[i], y, k_StagePosition.z);
            characters_[i].rotation = Quaternion.Euler(0.0f, 0.0f, roll_[i] + 200.0f * rate);
            characters_[i].gameObject.SetActive(rate > 0.0f && rate < 1.0f);
        }
    }

    // 演出用のカメラ・キャンバス・キャラクターを作る（最初の1回だけ。画面サイズが変わったら画像だけ作り直す）
    void Build()
    {
        if (camera_ != null && texture_ != null && texture_.width == Screen.width && texture_.height == Screen.height)
        {
            return;
        }
        if (texture_ != null)
        {
            texture_.Release();
        }
        texture_ = new RenderTexture(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 24, RenderTextureFormat.ARGB32);

        if (camera_ != null)
        {
            camera_.targetTexture = texture_;
            characterView_.texture = texture_;
            return;
        }

        // Unityちゃんだけを写すカメラ（透明な背景で、画面に重ねる用）
        GameObject cameraObj = new GameObject("TransitionCamera");
        cameraObj.transform.SetParent(transform, false);
        cameraObj.transform.position = k_StagePosition + new Vector3(0.0f, 0.0f, 10.0f);
        cameraObj.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
        camera_ = cameraObj.AddComponent<Camera>();
        camera_.orthographic = true;
        camera_.orthographicSize = k_CameraHalfHeight;
        camera_.nearClipPlane = 0.1f;
        camera_.farClipPlane = 30.0f;
        camera_.clearFlags = CameraClearFlags.SolidColor;
        camera_.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        camera_.allowHDR = false;
        camera_.allowMSAA = false;
        camera_.targetTexture = texture_;

        // どのシーンでも暗くならないよう、専用の光を当てる（キャラクターの正面から）
        GameObject lightObj = new GameObject("TransitionLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.position = k_StagePosition;
        lightObj.transform.rotation = Quaternion.LookRotation(new Vector3(0.3f, -0.4f, -1.0f));
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;

        // 画面に重ねるキャンバス（黒いフェードの上にUnityちゃんを載せる）
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        fade_ = CreateFullScreen<Image>(canvasObj.transform, "Fade");
        fade_.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        fade_.raycastTarget = false;
        characterView_ = CreateFullScreen<RawImage>(canvasObj.transform, "Characters");
        characterView_.texture = texture_;
        characterView_.raycastTarget = false;

        BuildCharacters();
    }

    T CreateFullScreen<T>(Transform parent, string objName) where T : Graphic
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent, false);
        T graphic = obj.AddComponent<T>();
        RectTransform rect = graphic.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return graphic;
    }

    void BuildCharacters()
    {
        GameObject prefab = Resources.Load<GameObject>(k_CharacterName);
        if (prefab == null)
        {
            Debug.LogWarning("Resources/" + k_CharacterName + " が見つからないので、キャラクターなしでフェードします");
        }

        characters_ = new Transform[k_CharacterCount];
        startDelay_ = new float[k_CharacterCount];
        startX_ = new float[k_CharacterCount];
        roll_ = new float[k_CharacterCount];

        // 横いっぱいに散らばるよう、人数で等分した位置に降らせる（降る順番は入れ替える）
        float halfWidth = k_CameraHalfHeight * 16.0f / 9.0f;
        float lastDelay = k_FadeOutTime - k_FallTime;
        for (int i = 0; i < k_CharacterCount; i++)
        {
            float rate = (i + 0.5f) / k_CharacterCount;
            float order = ((i * 5) % k_CharacterCount) / (float)(k_CharacterCount - 1);
            startX_[i] = Mathf.Lerp(-halfWidth, halfWidth, rate) * 0.9f;
            startDelay_[i] = lastDelay * order;
            roll_[i] = (i % 2 == 0 ? -1.0f : 1.0f) * 15.0f * (1 + i % 3);

            GameObject chara;
            if (prefab != null)
            {
                chara = Instantiate(prefab, transform);
                chara.name = "Character" + i;
                // 高さがカメラの k_CharacterHeightRate になるよう大きさを合わせる
                float height = MeasureHeight(chara);
                if (height > 0.0f)
                {
                    chara.transform.localScale = Vector3.one * (k_CameraHalfHeight * 2.0f * k_CharacterHeightRate / height);
                }
            }
            else
            {
                chara = new GameObject("Empty" + i);
                chara.transform.SetParent(transform, false);
            }
            chara.SetActive(false);
            characters_[i] = chara.transform;
        }
    }

    // キャラクターの見た目の高さ（スケール1のとき）
    static float MeasureHeight(GameObject obj)
    {
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
        {
            // 位置合わせ用の文字オブジェクト（GuideDummy）は含めない
            if (renderer.GetComponent<TextMesh>() != null)
            {
                continue;
            }
            if (first)
            {
                bounds = renderer.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return first ? 0.0f : bounds.size.y;
    }
}
