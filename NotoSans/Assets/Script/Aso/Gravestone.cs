using System.Collections.Generic;
using UnityEngine;

// プレイヤーが死んだ場所に立つ墓。Gravestone.Spawn(場所) で立てる（シーンには何も置かなくてよい）。
// モデルは四角を3つ組み合わせただけの軽いもの（1つの小さなメッシュを全部の墓で共有する）。
// 当たり判定は無いので、プレイヤーにも床にも影響しない。いつもカメラのほうを向く
public class Gravestone : MonoBehaviour
{
    // 立てておく墓の最大数。超えたら古いものから消す
    private const int k_MaxCount = 40;

    // 墓の大きさ（プレイヤーの背丈は約1）
    private const float k_BaseWidth = 0.9f;
    private const float k_BaseHeight = 0.1f;
    private const float k_BaseDepth = 0.35f;
    private const float k_BodyWidth = 0.64f;
    private const float k_BodyHeight = 0.62f;
    private const float k_BodyDepth = 0.2f;
    private const float k_TopWidth = 0.44f;
    private const float k_TopHeight = 0.12f;

    // 輪郭線の太さ（墓の外側へふくらませる量）と色
    private const float k_OutlineThickness = 0.035f;
    private static readonly Color k_OutlineColor = new Color(0.07f, 0.08f, 0.12f, 1.0f);

    private static readonly List<Gravestone> all_ = new List<Gravestone>();
    private static Mesh mesh_;
    private static Material material_;
    private static Material outlineMaterial_;

    // feetPosition：プレイヤーの足元。真下に床があれば、その表面に立てる
    public static void Spawn(Vector3 feetPosition)
    {
        Vector3 position = feetPosition;
        RaycastHit hit;
        if (Physics.Raycast(feetPosition + Vector3.up * 0.5f, Vector3.down, out hit, 2.0f, ~0, QueryTriggerInteraction.Ignore))
        {
            position.y = hit.point.y;
        }

        GameObject obj = new GameObject("Gravestone");
        obj.transform.position = position;
        obj.AddComponent<MeshFilter>().sharedMesh = GetMesh();
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        // 1つ目：石の面、2つ目：輪郭線（ふくらませた裏面だけを暗い色で描く）
        // 素材を作れなかったとき（シェーダーが見つからないなど）は、無い素材を飛ばす。ここで止まって、死亡の処理を止めないようにする
        List<Material> materials = new List<Material>();
        Material stone = GetMaterial();
        Material outline = GetOutlineMaterial();
        if (stone != null) { materials.Add(stone); }
        if (outline != null) { materials.Add(outline); }
        renderer.sharedMaterials = materials.ToArray();
        // 影は落とさない（軽くするため）
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Gravestone grave = obj.AddComponent<Gravestone>();
        all_.Add(grave);
        grave.FaceCamera();

        // 多すぎるときは、いちばん古い墓から消す
        while (all_.Count > k_MaxCount)
        {
            Gravestone oldest = all_[0];
            all_.RemoveAt(0);
            if (oldest != null)
            {
                Destroy(oldest.gameObject);
            }
        }
    }

    void Update()
    {
        FaceCamera();
    }

    void OnDestroy()
    {
        all_.Remove(this);
    }

    // カメラは横の向きにも回り込むので、どの向きからでも正面が見えるようにする（上下には傾けない）
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
            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }

    // ---- モデル（実行時に1つだけ作って、全部の墓で使い回す） ----

    // ビルドに入っているシェーダーだけを使う。Shader.Find は、ビルドに含まれていないシェーダーだと null を返す
    // （どの素材からも使われていないシェーダーは、ビルドから外される）。ゲームの素材が使っている URP の Lit を使う
    private static Shader FindShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        return shader;
    }

    private static Material GetMaterial()
    {
        if (material_ == null)
        {
            Shader shader = FindShader();
            if (shader == null)
            {
                return null;
            }
            material_ = new Material(shader);
            material_.name = "GravestoneMaterial";
            Color stone = new Color(0.56f, 0.58f, 0.62f, 1.0f);
            material_.color = stone;
            if (material_.HasProperty("_BaseColor"))
            {
                material_.SetColor("_BaseColor", stone);
            }
            if (material_.HasProperty("_Smoothness"))
            {
                material_.SetFloat("_Smoothness", 0.1f);
            }
        }
        return material_;
    }

    // 輪郭線用の素材。ふくらませたモデルの「表側の面を描かない」ので、はみ出した裏面だけが縁取りとして見える。
    // 光を受けても暗いままになるよう、艶と反射は切っておく
    private static Material GetOutlineMaterial()
    {
        if (outlineMaterial_ == null)
        {
            Shader shader = FindShader();
            if (shader == null)
            {
                return null;
            }
            outlineMaterial_ = new Material(shader);
            outlineMaterial_.name = "GravestoneOutlineMaterial";
            outlineMaterial_.color = k_OutlineColor;
            if (outlineMaterial_.HasProperty("_BaseColor"))
            {
                outlineMaterial_.SetColor("_BaseColor", k_OutlineColor);
            }
            if (outlineMaterial_.HasProperty("_Smoothness"))
            {
                outlineMaterial_.SetFloat("_Smoothness", 0.0f);
            }
            if (outlineMaterial_.HasProperty("_Metallic"))
            {
                outlineMaterial_.SetFloat("_Metallic", 0.0f);
            }
            if (outlineMaterial_.HasProperty("_SpecularHighlights"))
            {
                outlineMaterial_.SetFloat("_SpecularHighlights", 0.0f);
            }
            if (outlineMaterial_.HasProperty("_EnvironmentReflections"))
            {
                outlineMaterial_.SetFloat("_EnvironmentReflections", 0.0f);
            }
            // 表側の面（Front）を描かない（0＝両面、1＝表を描かない、2＝裏を描かない）
            if (outlineMaterial_.HasProperty("_Cull"))
            {
                outlineMaterial_.SetFloat("_Cull", 1.0f);
            }
        }
        return outlineMaterial_;
    }
    // 下から「土台」「墓石の本体」「墓石の上の細い部分」を重ねる。足元（y=0）が底。
    // 1つ目の面の組が石、2つ目が少しふくらませた輪郭線用
    private static Mesh GetMesh()
    {
        if (mesh_ != null)
        {
            return mesh_;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> stone = new List<int>();
        List<int> outline = new List<int>();

        float y = 0.0f;
        Vector3[] centers =
        {
            new Vector3(0.0f, y + k_BaseHeight * 0.5f, 0.0f),
            new Vector3(0.0f, y + k_BaseHeight + k_BodyHeight * 0.5f, 0.0f),
            new Vector3(0.0f, y + k_BaseHeight + k_BodyHeight + k_TopHeight * 0.5f, 0.0f),
        };
        Vector3[] sizes =
        {
            new Vector3(k_BaseWidth, k_BaseHeight, k_BaseDepth),
            new Vector3(k_BodyWidth, k_BodyHeight, k_BodyDepth),
            new Vector3(k_TopWidth, k_TopHeight, k_BodyDepth),
        };
        for (int i = 0; i < centers.Length; i++)
        {
            AddBox(vertices, normals, stone, centers[i], sizes[i]);
        }
        // 輪郭線：同じ箱を、全方向へ少しだけ大きくしたもの
        for (int i = 0; i < centers.Length; i++)
        {
            AddBox(vertices, normals, outline, centers[i], sizes[i] + Vector3.one * (k_OutlineThickness * 2.0f));
        }

        mesh_ = new Mesh();
        mesh_.name = "GravestoneMesh";
        mesh_.SetVertices(vertices);
        mesh_.SetNormals(normals);
        mesh_.subMeshCount = 2;
        mesh_.SetTriangles(stone, 0);
        mesh_.SetTriangles(outline, 1);
        mesh_.RecalculateBounds();
        return mesh_;
    }

    // 四角い箱を1つ足す（面ごとに頂点を分けて、角がくっきり見えるようにする）
    private static void AddBox(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 center, Vector3 size)
    {
        Vector3 h = size * 0.5f;
        // 面の向きと、その面の2つの辺の向き
        Vector3[] faceNormals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
        Vector3[] faceUs = { Vector3.right, Vector3.left, Vector3.back, Vector3.forward, Vector3.right, Vector3.right };
        Vector3[] faceVs = { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.back, Vector3.forward };
        for (int f = 0; f < 6; f++)
        {
            Vector3 n = faceNormals[f];
            Vector3 u = faceUs[f];
            Vector3 v = faceVs[f];
            Vector3 faceCenter = center + Vector3.Scale(n, h);
            Vector3 du = Vector3.Scale(u, h);
            Vector3 dv = Vector3.Scale(v, h);

            int start = vertices.Count;
            vertices.Add(faceCenter - du - dv);
            vertices.Add(faceCenter + du - dv);
            vertices.Add(faceCenter + du + dv);
            vertices.Add(faceCenter - du + dv);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(n);
            }

            // 外から見て時計回りになる向きに、三角形を並べる
            bool clockwise = Vector3.Dot(Vector3.Cross(u, v), n) < 0.0f;
            if (clockwise)
            {
                triangles.AddRange(new int[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
            else
            {
                triangles.AddRange(new int[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
        }
    }
}
