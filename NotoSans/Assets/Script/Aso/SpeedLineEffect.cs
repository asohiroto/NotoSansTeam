using UnityEngine;

// 最高速で落ちているあいだ、画面の左右の端に下から上へ流れる線（アニメ風の疾走感）を出す
// カメラに付ける（CameraMove が開始時に付ける）。線はカメラの子のメッシュとして、カメラのすぐ前に描く
public class SpeedLineEffect : MonoBehaviour
{
    // 片側の線の本数
    [SerializeField] private int k_LineCountPerSide = 18;
    // 線を出す帯の幅（画面の半分の幅に対する割合。端からこの幅の中に出す）
    [SerializeField] private float k_EdgeWidth = 0.22f;
    // 線の長さ（画面の高さに対する割合）の最小・最大
    [SerializeField] private Vector2 k_Length = new Vector2(0.25f, 0.7f);
    // 線の太さ（画面の高さに対する割合）の最小・最大
    [SerializeField] private Vector2 k_Thickness = new Vector2(0.008f, 0.024f);
    // 線が流れる速さ（1秒に画面の高さの何倍進むか）の最小・最大
    [SerializeField] private Vector2 k_Speed = new Vector2(3.0f, 6.0f);
    // 線の色
    [SerializeField] private Color k_Color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    // 出し始め・消えるまでの時間（秒）
    [SerializeField] private float k_FadeTime = 0.15f;
    // 最高速とみなす割合（落下速度の最大値に対して）
    [SerializeField] private float k_MaxSpeedRate = 0.98f;
    // 線を置くカメラからの距離（ニアクリップより少し奥）
    [SerializeField] private float k_Depth = 0.5f;

    private Player player_;
    private Camera camera_;
    private Mesh mesh_;
    private MeshRenderer renderer_;
    private Material material_;

    // 線ごとの状態（画面の高さを 2 とした座標。x: -1〜1 を画面の横幅、y: -1〜1 を画面の高さとする）
    private float[] lineX_;
    private float[] lineY_;
    private float[] lineLength_;
    private float[] lineThickness_;
    private float[] lineSpeed_;

    private Vector3[] vertices_;
    private Color[] colors_;

    // 今の濃さ（0: 見えない 〜 1: はっきり）
    private float intensity_ = 0.0f;

    public void Init(Player player)
    {
        player_ = player;
    }

    void Start()
    {
        camera_ = GetComponent<Camera>();
        CreateMesh();
    }

    void OnDestroy()
    {
        if (mesh_ != null)
        {
            Destroy(mesh_);
        }
        if (material_ != null)
        {
            Destroy(material_);
        }
    }

    // 線を描くメッシュ（線1本＝四角形1枚）と、その表示用のオブジェクトを作る
    void CreateMesh()
    {
        int count = k_LineCountPerSide * 2;
        lineX_ = new float[count];
        lineY_ = new float[count];
        lineLength_ = new float[count];
        lineThickness_ = new float[count];
        lineSpeed_ = new float[count];
        for (int i = 0; i < count; i++)
        {
            SpawnLine(i, Random.Range(-1.0f, 1.0f));
        }

        vertices_ = new Vector3[count * 4];
        colors_ = new Color[count * 4];
        Vector2[] uvs = new Vector2[count * 4];
        int[] indices = new int[count * 6];
        for (int i = 0; i < count; i++)
        {
            int v = i * 4;
            // 横: u = 0〜1（線の太さ方向）、縦: v = 0〜1（線の長さ方向）
            uvs[v + 0] = new Vector2(0.0f, 0.0f);
            uvs[v + 1] = new Vector2(1.0f, 0.0f);
            uvs[v + 2] = new Vector2(1.0f, 1.0f);
            uvs[v + 3] = new Vector2(0.0f, 1.0f);
            int n = i * 6;
            indices[n + 0] = v + 0;
            indices[n + 1] = v + 2;
            indices[n + 2] = v + 1;
            indices[n + 3] = v + 0;
            indices[n + 4] = v + 3;
            indices[n + 5] = v + 2;
        }

        mesh_ = new Mesh();
        mesh_.name = "SpeedLines";
        mesh_.MarkDynamic();
        mesh_.vertices = vertices_;
        mesh_.colors = colors_;
        mesh_.uv = uvs;
        mesh_.triangles = indices;
        // カメラの前に常にあるので、画面外として消されないよう大きめの範囲にしておく
        mesh_.bounds = new Bounds(Vector3.zero, Vector3.one * 100.0f);

        GameObject obj = new GameObject("SpeedLines");
        obj.transform.SetParent(transform, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh_;
        renderer_ = obj.AddComponent<MeshRenderer>();
        renderer_.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer_.receiveShadows = false;

        material_ = new Material(Shader.Find("Sprites/Default"));
        material_.mainTexture = CreateLineTexture();
        // ほかの半透明（エフェクトなど）より手前に描く
        material_.renderQueue = 4000;
        renderer_.sharedMaterial = material_;
        renderer_.enabled = false;
    }

    // 先が細く、中心がいちばん明るい線のテクスチャ（横: 太さ、縦: 長さ）
    Texture2D CreateLineTexture()
    {
        const int w = 16;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            // 下（後ろ）は長く尾を引き、上（先頭）は短く細くなる
            float along = v < 0.8f ? Mathf.SmoothStep(0.0f, 1.0f, v / 0.8f) : Mathf.SmoothStep(1.0f, 0.0f, (v - 0.8f) / 0.2f);
            // 先頭に近いほど太く、尾は細くする
            float width = Mathf.Lerp(0.25f, 1.0f, along);
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / w * 2.0f - 1.0f);
                float across = Mathf.Clamp01(1.0f - u / width);
                across = across * across;
                pixels[y * w + x] = new Color(1.0f, 1.0f, 1.0f, along * across);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // 線を、画面の左右の端の帯のどこかに作り直す（startY: 線の下端の高さ）
    void SpawnLine(int i, float startY)
    {
        // 前半は左の端、後半は右の端
        float side = i < k_LineCountPerSide ? -1.0f : 1.0f;
        // 端に近いほど多く出す
        float fromEdge = Random.value * Random.value * k_EdgeWidth;
        lineX_[i] = side * (1.0f - fromEdge);
        lineLength_[i] = Random.Range(k_Length.x, k_Length.y) * 2.0f;
        lineThickness_[i] = Random.Range(k_Thickness.x, k_Thickness.y) * 2.0f;
        lineSpeed_[i] = Random.Range(k_Speed.x, k_Speed.y) * 2.0f;
        lineY_[i] = startY;
    }

    void LateUpdate()
    {
        if (mesh_ == null || camera_ == null)
        {
            return;
        }

        // 最高速で落ちているときだけ濃くする（プレイヤーが倒されて隠れているときは消す）
        bool active = player_ != null
            && player_.gameObject.activeInHierarchy
            && player_.speed_.y <= player_.MaxFallSpeed * k_MaxSpeedRate;
        float step = k_FadeTime > 0.0f ? Time.deltaTime / k_FadeTime : 1.0f;
        intensity_ = Mathf.MoveTowards(intensity_, active ? 1.0f : 0.0f, step);

        renderer_.enabled = intensity_ > 0.0f;
        if (!renderer_.enabled)
        {
            return;
        }

        // 画面の高さの半分・幅の半分（カメラから k_Depth 離れた面での大きさ）
        float halfHeight = k_Depth * Mathf.Tan(camera_.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * camera_.aspect;

        for (int i = 0; i < lineX_.Length; i++)
        {
            // 下から上へ流す（落ちているので景色が上へ流れる）。画面の上へ抜けたら下から出し直す
            lineY_[i] += lineSpeed_[i] * Time.deltaTime;
            if (lineY_[i] > 1.0f)
            {
                SpawnLine(i, -1.0f - lineLength_[i] * Random.value);
            }

            float x = lineX_[i] * halfWidth;
            float bottom = lineY_[i] * halfHeight;
            float top = (lineY_[i] + lineLength_[i]) * halfHeight;
            float half = lineThickness_[i] * halfHeight * 0.5f;

            int v = i * 4;
            vertices_[v + 0] = new Vector3(x - half, bottom, k_Depth);
            vertices_[v + 1] = new Vector3(x + half, bottom, k_Depth);
            vertices_[v + 2] = new Vector3(x + half, top, k_Depth);
            vertices_[v + 3] = new Vector3(x - half, top, k_Depth);

            Color c = k_Color;
            c.a *= intensity_;
            colors_[v + 0] = c;
            colors_[v + 1] = c;
            colors_[v + 2] = c;
            colors_[v + 3] = c;
        }

        mesh_.vertices = vertices_;
        mesh_.colors = colors_;
    }
}
