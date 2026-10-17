using UnityEngine;

// 下から光で照らされているような演出（クリアのロゴ用）。
//  ・下から扇のように広がる、ゆっくり脈打つ光の筋
//  ・足元にたまる光の溜まり
//  ・下から昇っていく光の粒
//  ・ロゴを本当に下から照らす点光源（ロゴの下側ほど明るく見える）
// 見た目はすべて実行時にコードで作る。このコンポーネントを付けた場所が、光の出どころ（光の筋の根元）になる
public class UplightEffect : MonoBehaviour
{
    // 光の筋：本数・長さ・幅・広がる角度（左右それぞれ）
    [SerializeField] private int k_RayCount = 9;
    [SerializeField] private float k_RayLength = 10.0f;
    [SerializeField] private float k_RayWidth = 1.5f;
    [SerializeField] private float k_SpreadDegrees = 38.0f;
    // 脈打つ1周期（秒）
    [SerializeField] private float k_PulsePeriod = 3.0f;
    // 光の色
    [SerializeField] private Color k_RayColor = new Color(1.0f, 0.93f, 0.62f, 1.0f);

    // ロゴを下から照らす光（点光源）。根元からのずれ、色、強さ、届く距離
    [SerializeField] private bool k_AddLight = true;
    [SerializeField] private Vector3 k_LightOffset = new Vector3(0.0f, 3.0f, -2.2f);
    [SerializeField] private Color k_LightColor = new Color(1.0f, 0.86f, 0.5f, 1.0f);
    [SerializeField] private float k_LightIntensity = 5.0f;
    [SerializeField] private float k_LightRange = 14.0f;

    private const int k_SparkleCount = 22;

    private Mesh quad_;
    private Texture2D[] textures_;
    private Material rayMaterial_;
    private Material glowMaterial_;
    private Material sparkleMaterial_;
    private Transform[] rays_;
    private MeshRenderer[] rayRenderers_;
    private float[] rayAngle_;
    private Transform glow_;
    private MeshRenderer glowRenderer_;
    private Transform[] sparkles_;
    private MeshRenderer[] sparkleRenderers_;
    private float[] sparkleSpeed_;
    private float[] sparkleOffset_;
    private float[] sparkleX_;
    private Light light_;
    private MaterialPropertyBlock block_;

    void Awake()
    {
        Build();
    }

    void OnDestroy()
    {
        if (textures_ != null)
        {
            foreach (Texture2D texture in textures_) { Destroy(texture); }
        }
        Destroy(rayMaterial_);
        Destroy(glowMaterial_);
        Destroy(sparkleMaterial_);
        Destroy(quad_);
    }

    void Update()
    {
        Evaluate(Time.unscaledTime);
    }

    // 時刻 t（秒）の見た目にする（検査のために、時刻を指定して呼べるようにしてある）
    public void Evaluate(float t)
    {
        if (rays_ == null)
        {
            return;
        }

        float pulseAngle = t * Mathf.PI * 2.0f / k_PulsePeriod;
        int count = rays_.Length;
        for (int i = 0; i < count; i++)
        {
            float phase = i * 0.9f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(pulseAngle + phase);
            // 真ん中の筋ほど明るく、外側ほど薄くする
            float centerWeight = 1.0f - 0.55f * Mathf.Abs(rayAngle_[i]) / Mathf.Max(0.01f, k_SpreadDegrees);
            float sway = Mathf.Sin(t * 0.7f + i) * 3.0f;
            rays_[i].localRotation = Quaternion.Euler(0.0f, 0.0f, -(rayAngle_[i] + sway));
            rays_[i].localScale = new Vector3(k_RayWidth, k_RayLength * (0.9f + 0.15f * pulse), 1.0f);
            SetAlpha(rayRenderers_[i], k_RayColor, (0.26f + 0.42f * pulse) * centerWeight);
        }

        // 足元の光の溜まり
        glow_.localScale = new Vector3(k_RayLength * 1.5f, k_RayLength * 0.4f, 1.0f);
        SetAlpha(glowRenderer_, k_RayColor, 0.55f + 0.15f * Mathf.Sin(pulseAngle));

        // 昇っていく光の粒
        for (int i = 0; i < k_SparkleCount; i++)
        {
            float p = Mathf.Repeat(t * sparkleSpeed_[i] + sparkleOffset_[i], 1.0f);
            float y = p * k_RayLength * 0.8f;
            // 上へ行くほど、扇の幅いっぱいに広がる
            float x = sparkleX_[i] * (0.4f + p * 0.9f) * Mathf.Tan(k_SpreadDegrees * Mathf.Deg2Rad) * y * 0.8f;
            sparkles_[i].localPosition = new Vector3(x, y, -0.02f);
            float size = Mathf.Lerp(0.1f, 0.22f, sparkleOffset_[i]);
            sparkles_[i].localScale = new Vector3(size, size, 1.0f);
            SetAlpha(sparkleRenderers_[i], k_RayColor, Mathf.Sin(p * Mathf.PI) * 0.95f);
        }

        if (light_ != null)
        {
            light_.intensity = k_LightIntensity * (0.85f + 0.15f * Mathf.Sin(pulseAngle * 1.3f));
        }
    }

    private void SetAlpha(MeshRenderer renderer, Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        block_.SetColor("_Color", color);
        renderer.SetPropertyBlock(block_);
    }

    // ---- 見た目の組み立て ----

    private void Build()
    {
        block_ = new MaterialPropertyBlock();
        quad_ = CreateQuad();

        Texture2D rayTexture = CreateRayTexture(64, 256);
        Texture2D glowTexture = CreateGlowTexture(128, 64);
        Texture2D dotTexture = CreateDotTexture(32);
        textures_ = new Texture2D[] { rayTexture, glowTexture, dotTexture };
        rayMaterial_ = CreateMaterial(rayTexture);
        glowMaterial_ = CreateMaterial(glowTexture);
        sparkleMaterial_ = CreateMaterial(dotTexture);

        // 奥から順に：光の溜まり → 光の筋 → 光の粒
        glow_ = CreateLayer("Glow", glowMaterial_, 0.02f, out glowRenderer_);

        int count = Mathf.Max(1, k_RayCount);
        rays_ = new Transform[count];
        rayRenderers_ = new MeshRenderer[count];
        rayAngle_ = new float[count];
        for (int i = 0; i < count; i++)
        {
            rays_[i] = CreateLayer("Ray" + i, rayMaterial_, 0.0f, out rayRenderers_[i]);
            rayAngle_[i] = count == 1 ? 0.0f : Mathf.Lerp(-k_SpreadDegrees, k_SpreadDegrees, i / (float)(count - 1));
        }

        sparkles_ = new Transform[k_SparkleCount];
        sparkleRenderers_ = new MeshRenderer[k_SparkleCount];
        sparkleSpeed_ = new float[k_SparkleCount];
        sparkleOffset_ = new float[k_SparkleCount];
        sparkleX_ = new float[k_SparkleCount];
        for (int i = 0; i < k_SparkleCount; i++)
        {
            sparkles_[i] = CreateLayer("Sparkle" + i, sparkleMaterial_, -0.02f, out sparkleRenderers_[i]);
            // 毎回同じ動きになるよう、番号から決める
            sparkleSpeed_[i] = 0.12f + 0.12f * ((i * 7) % 5) / 4.0f;
            sparkleOffset_[i] = ((i * 37) % k_SparkleCount) / (float)k_SparkleCount;
            sparkleX_[i] = (((i * 11) % 9) / 4.0f) - 1.0f;
        }

        if (k_AddLight)
        {
            GameObject lightObj = new GameObject("UplightLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = k_LightOffset;
            light_ = lightObj.AddComponent<Light>();
            light_.type = LightType.Point;
            light_.color = k_LightColor;
            light_.intensity = k_LightIntensity;
            light_.range = k_LightRange;
            light_.shadows = LightShadows.None;
        }

        Evaluate(0.0f);
    }

    private Transform CreateLayer(string layerName, Material material, float z, out MeshRenderer renderer)
    {
        GameObject obj = new GameObject(layerName);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0.0f, 0.0f, z);
        obj.AddComponent<MeshFilter>().sharedMesh = quad_;
        renderer = obj.AddComponent<MeshRenderer>();
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
        material.renderQueue = 3050;
        return material;
    }

    // 根元（下の辺の真ん中）が原点で、上へ伸びる四角
    private static Mesh CreateQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "UplightQuad";
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0.0f, 0.0f), new Vector3(0.5f, 0.0f, 0.0f),
            new Vector3(-0.5f, 1.0f, 0.0f), new Vector3(0.5f, 1.0f, 0.0f),
        };
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    // 光の筋：中心が明るく、左右はなめらかに消える。下ほど強く、上へ行くほど薄れる
    private static Texture2D CreateRayTexture(int width, int height)
    {
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height;
            float fade = Mathf.Pow(1.0f - v, 1.5f);
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f) / width * 2.0f - 1.0f;
                float a = Mathf.Exp(-(nx * nx) / 0.2f) * fade;
                a *= Mathf.Clamp01((1.0f - Mathf.Abs(nx)) * 6.0f);
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255.0f));
            }
        }
        return CreateTexture(width, height, pixels);
    }

    // 足元の光の溜まり：下の辺の真ん中から、上へなだらかに消える半円
    private static Texture2D CreateGlowTexture(int width, int height)
    {
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height;
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f) / width * 2.0f - 1.0f;
                float r = Mathf.Sqrt(nx * nx + v * v);
                float a = Mathf.Pow(Mathf.Clamp01(1.0f - r), 1.4f);
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
                float a = Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Sqrt(nx * nx + ny * ny)), 1.8f);
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
