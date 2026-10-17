using UnityEngine;

// 光のゲートの見た目。細い光の筋が、横に開いて光る輪になり、またすぼまる動きをくり返す。
// 見た目はすべて実行時にコードで作る（画像や素材は要らない）。いつもカメラのほうを向く
public class LightGate : MonoBehaviour
{
    // ゲートの大きさ（開ききったとき）
    [SerializeField] private float k_Width = 2.4f;
    [SerializeField] private float k_Height = 3.6f;
    // 開いて閉じるまでの1周期（秒）
    [SerializeField] private float k_Period = 4.0f;
    // 光の色
    [SerializeField] private Color k_RingColor = new Color(0.6f, 0.95f, 1.0f, 1.0f);
    [SerializeField] private Color k_GlowColor = new Color(0.75f, 1.0f, 1.0f, 1.0f);

    // 1周期のうちの割合：閉じている → 開いていく → 開いたまま → 閉じていく
    private const float k_ClosedRate = 0.12f;
    private const float k_OpeningRate = 0.28f;
    private const float k_HoldRate = 0.38f;
    // 閉じているときの幅の割合（細い光の筋として残す）
    private const float k_SlitWidthRate = 0.04f;
    private const int k_SparkleCount = 14;

    private Transform visual_;
    private Transform fill_;
    private Transform ring_;
    private Transform beam_;
    private Transform[] sparkles_;
    private float[] sparkleSpeed_;
    private float[] sparkleOffset_;
    private float[] sparkleX_;

    private Material fillMaterial_;
    private Material ringMaterial_;
    private Material beamMaterial_;
    private Material sparkleMaterial_;
    private Texture2D[] textures_;
    private Mesh quad_;
    private MeshRenderer[] sparkleRenderers_;
    private MaterialPropertyBlock block_;

    void Awake()
    {
        Build();
    }

    void OnDestroy()
    {
        // 実行時に作ったものを片づける
        foreach (Texture2D texture in textures_ ?? new Texture2D[0]) { Destroy(texture); }
        Destroy(fillMaterial_);
        Destroy(ringMaterial_);
        Destroy(beamMaterial_);
        Destroy(sparkleMaterial_);
        Destroy(quad_);
    }

    void Update()
    {
        float open = EvaluateOpen(Time.time / k_Period);
        float widthRate = Mathf.Lerp(k_SlitWidthRate, 1.0f, open);

        FaceCamera();

        // 光の輪：横に開く
        ring_.localScale = new Vector3(k_Width * widthRate, k_Height, 1.0f);
        SetColor(ringMaterial_, k_RingColor, Mathf.Lerp(0.55f, 1.0f, open));

        // 中の光：開くほど明るい。開いたままの間は、ゆっくり脈打つ
        float pulse = 0.9f + 0.1f * Mathf.Sin(Time.time * 4.0f);
        fill_.localScale = new Vector3(k_Width * widthRate * 0.96f, k_Height * 0.98f, 1.0f);
        SetColor(fillMaterial_, k_GlowColor, open * pulse);

        // 縦に伸びる光の筋
        beam_.localScale = new Vector3(k_Width * Mathf.Lerp(0.3f, 1.3f, open), k_Height * 1.25f, 1.0f);
        SetColor(beamMaterial_, k_GlowColor, open * 0.55f * pulse + 0.15f);

        // 光の粒：開いている間、下から上へ昇る
        for (int i = 0; i < k_SparkleCount; i++)
        {
            float p = Mathf.Repeat(Time.time * sparkleSpeed_[i] + sparkleOffset_[i], 1.0f);
            float y = Mathf.Lerp(-k_Height * 0.45f, k_Height * 0.45f, p);
            float x = sparkleX_[i] * k_Width * 0.38f * widthRate;
            sparkles_[i].localPosition = new Vector3(x, y, 0.03f);
            float size = Mathf.Lerp(0.1f, 0.2f, sparkleOffset_[i]);
            sparkles_[i].localScale = new Vector3(size, size, 1.0f);

            block_.SetColor("_Color", new Color(k_GlowColor.r, k_GlowColor.g, k_GlowColor.b, open * Mathf.Sin(p * Mathf.PI)));
            sparkleRenderers_[i].SetPropertyBlock(block_);
        }
    }

    // 1周期の中での、開き具合（0＝閉じている、1＝開ききっている）
    private static float EvaluateOpen(float cycle)
    {
        float t = Mathf.Repeat(cycle, 1.0f);
        if (t < k_ClosedRate)
        {
            return 0.0f;
        }
        t -= k_ClosedRate;
        if (t < k_OpeningRate)
        {
            // 開くときは、はじめ速く、終わりにゆっくり
            float x = t / k_OpeningRate;
            return 1.0f - (1.0f - x) * (1.0f - x) * (1.0f - x);
        }
        t -= k_OpeningRate;
        if (t < k_HoldRate)
        {
            return 1.0f;
        }
        t -= k_HoldRate;
        // 閉じるときは、はじめゆっくり、終わりに速く
        float closing = t / (1.0f - k_ClosedRate - k_OpeningRate - k_HoldRate);
        return 1.0f - closing * closing;
    }

    // 見た目をカメラのほうへ向ける（上下には傾けない）
    private void FaceCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }
        Vector3 toCamera = cam.transform.position - transform.position;
        toCamera.y = 0.0f;
        if (toCamera.sqrMagnitude > 0.0001f)
        {
            visual_.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }

    private static void SetColor(Material material, Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        material.color = color;
    }

    // ---- 見た目の組み立て ----

    private void Build()
    {
        block_ = new MaterialPropertyBlock();
        quad_ = CreateQuad();

        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(transform, false);
        visual_ = visualObj.transform;

        Texture2D fillTexture = CreateEllipseTexture(64, 128, false);
        Texture2D ringTexture = CreateEllipseTexture(128, 256, true);
        Texture2D beamTexture = CreateBeamTexture(32, 256);
        Texture2D sparkleTexture = CreateDotTexture(32);
        textures_ = new Texture2D[] { fillTexture, ringTexture, beamTexture, sparkleTexture };

        fillMaterial_ = CreateMaterial(fillTexture);
        ringMaterial_ = CreateMaterial(ringTexture);
        beamMaterial_ = CreateMaterial(beamTexture);
        sparkleMaterial_ = CreateMaterial(sparkleTexture);

        // 奥から順に：筋 → 中の光 → 輪 → 粒
        beam_ = CreateLayer("Beam", beamMaterial_, 0.0f);
        fill_ = CreateLayer("Fill", fillMaterial_, 0.01f);
        ring_ = CreateLayer("Ring", ringMaterial_, 0.02f);

        sparkles_ = new Transform[k_SparkleCount];
        sparkleRenderers_ = new MeshRenderer[k_SparkleCount];
        sparkleSpeed_ = new float[k_SparkleCount];
        sparkleOffset_ = new float[k_SparkleCount];
        sparkleX_ = new float[k_SparkleCount];
        for (int i = 0; i < k_SparkleCount; i++)
        {
            Transform spark = CreateLayer("Sparkle" + i, sparkleMaterial_, 0.03f);
            sparkles_[i] = spark;
            sparkleRenderers_[i] = spark.GetComponent<MeshRenderer>();
            // 毎回同じ動きになるよう、番号から決める
            sparkleSpeed_[i] = 0.18f + 0.1f * ((i * 7) % 5) / 4.0f;
            sparkleOffset_[i] = ((i * 37) % k_SparkleCount) / (float)k_SparkleCount;
            sparkleX_[i] = (((i * 11) % 9) / 4.0f) - 1.0f;
        }
    }

    private Transform CreateLayer(string layerName, Material material, float z)
    {
        GameObject obj = new GameObject(layerName);
        obj.transform.SetParent(visual_, false);
        obj.transform.localPosition = new Vector3(0.0f, 0.0f, z);
        obj.AddComponent<MeshFilter>().sharedMesh = quad_;
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return obj.transform;
    }

    private static Material CreateMaterial(Texture2D texture)
    {
        // 透明で、両面が見える標準のスプライト用の素材
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = texture;
        material.renderQueue = 3100;
        return material;
    }

    private static Mesh CreateQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "LightGateQuad";
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0.0f), new Vector3(0.5f, -0.5f, 0.0f),
            new Vector3(-0.5f, 0.5f, 0.0f), new Vector3(0.5f, 0.5f, 0.0f),
        };
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    // 縦長の楕円の光。ring が true なら輪（ふちが光る）、false なら中がふんわり光る塗り
    private static Texture2D CreateEllipseTexture(int width, int height, bool ring)
    {
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f) / width * 2.0f - 1.0f;
                float ny = (y + 0.5f) / height * 2.0f - 1.0f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float a;
                if (ring)
                {
                    float d = (r - 0.88f) / 0.06f;
                    float core = Mathf.Exp(-d * d);
                    float e = (r - 0.88f) / 0.2f;
                    float glow = 0.4f * Mathf.Exp(-e * e);
                    a = Mathf.Clamp01(core + glow);
                }
                else
                {
                    a = Mathf.Pow(Mathf.Clamp01(1.0f - r), 1.3f) * 0.85f;
                }
                // ふちで必ず消す（四角い枠が見えないように）
                a *= Mathf.Clamp01((1.0f - r) * 6.0f);
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255.0f));
            }
        }
        return CreateTexture(width, height, pixels);
    }

    // 縦に伸びる光の筋（中心ほど明るく、上下は薄れる）
    private static Texture2D CreateBeamTexture(int width, int height)
    {
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f) / width * 2.0f - 1.0f;
                float ny = (y + 0.5f) / height * 2.0f - 1.0f;
                float a = Mathf.Exp(-(nx * nx) / 0.12f) * (1.0f - Mathf.Pow(Mathf.Abs(ny), 2.5f));
                a *= Mathf.Clamp01((1.0f - Mathf.Abs(nx)) * 6.0f);
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255.0f));
            }
        }
        return CreateTexture(width, height, pixels);
    }

    // 丸くぼやけた光の粒
    private static Texture2D CreateDotTexture(int size)
    {
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2.0f - 1.0f;
                float ny = (y + 0.5f) / size * 2.0f - 1.0f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Pow(Mathf.Clamp01(1.0f - r), 1.8f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255.0f));
            }
        }
        return CreateTexture(size, size, pixels);
    }

    private static Texture2D CreateTexture(int width, int height, Color32[] pixels)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
