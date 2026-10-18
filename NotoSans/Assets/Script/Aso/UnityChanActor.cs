using System.Collections;
using UnityEngine;

// 演出用のUnityちゃん。タイトル・クリア・スタッフロールの画面に出て、動き回ったり、喜んだりする。
// 見た目は Resources/TransitionUnityChan（ゲーム内のプレイヤーと同じ衣装。player.prefab から自動で作られる）、
// 動きは Resources/UnityChanActor（アニメーター）を使う。画面のどこに、どれくらいの大きさで出すかは、カメラの映る範囲で決める
public class UnityChanActor : MonoBehaviour
{
    public enum Behavior
    {
        // あっちこっちへ歩いて、止まってはあたりを見回す（タイトル）
        Wander,
        // 同じ場所で、回ったり、拍手したり、跳ねたりして喜び続ける（クリア）
        Cheer,
        // ときどき横から歩いて出てきて、喜んだり拍手したりして、また帰っていく（スタッフロール）
        Visit,
    }

    // アニメーターのステート名（UnityChanActorControllerBuilder と同じ）
    private const string k_Idle = "Idle";
    private const string k_Walk = "Walk";
    private const string k_Clap = "Clap";
    private const string k_Spin = "Spin";
    private const string k_Hold = "Hold";
    private const string k_Jump = "Jump";

    private const string k_PrefabName = "TransitionUnityChan";
    private const string k_ControllerName = "UnityChanActor";
    // 動きの切り替えのなめらかさ（秒）
    private const float k_FadeTime = 0.15f;

    private Behavior behavior_;
    private Animator animator_;
    private Camera camera_;
    // 画面上の位置（0〜1）。x は左右、feetY は足元の高さ。depth はカメラからの距離
    private float viewX_;
    private float feetY_;
    private float depth_;
    // 歩く速さ（画面の幅に対する割合 / 秒）
    private float walkSpeed_ = 0.085f;
    private float yaw_;

    // 演出用のUnityちゃんを出す。heightRate は、画面の高さに対する背の高さの割合
    public static UnityChanActor Spawn(Behavior behavior, float viewX, float feetY, float depth, float heightRate)
    {
        Camera cam = Camera.main;
        GameObject prefab = Resources.Load<GameObject>(k_PrefabName);
        RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>(k_ControllerName);
        if (cam == null || prefab == null || controller == null)
        {
            Debug.LogWarning("UnityChanActor: カメラ・キャラクター・アニメーターのどれかが見つからないので、出せません");
            return null;
        }

        GameObject obj = Instantiate(prefab);
        obj.name = "UnityChanActor_" + behavior;
        // 背の高さを、画面の高さに対する割合に合わせる
        float screenHeight = 2.0f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float height = MeasureHeight(obj);
        if (height > 0.0f)
        {
            obj.transform.localScale = Vector3.one * (screenHeight * heightRate / height);
        }

        UnityChanActor actor = obj.AddComponent<UnityChanActor>();
        actor.behavior_ = behavior;
        actor.camera_ = cam;
        actor.viewX_ = viewX;
        actor.feetY_ = feetY;
        actor.depth_ = depth;
        actor.animator_ = obj.GetComponent<Animator>();
        if (actor.animator_ != null)
        {
            actor.animator_.runtimeAnimatorController = controller;
            actor.animator_.applyRootMotion = false;
            actor.animator_.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        return actor;
    }

    void Start()
    {
        yaw_ = FaceCameraYaw();
        Place();
        switch (behavior_)
        {
            case Behavior.Wander: StartCoroutine(WanderRoutine()); break;
            case Behavior.Cheer: StartCoroutine(CheerRoutine()); break;
            case Behavior.Visit: StartCoroutine(VisitRoutine()); break;
        }
    }

    // ---- 動きの流れ ----

    // あっちこっちへ歩いて、止まってはあたりを見回す
    private IEnumerator WanderRoutine()
    {
        Play(k_Idle);
        yield return Wait(Random.Range(0.5f, 1.5f));
        while (true)
        {
            float target = Random.Range(0.08f, 0.92f);
            int guard = 0;
            while (Mathf.Abs(target - viewX_) < 0.2f && guard++ < 10)
            {
                target = Random.Range(0.08f, 0.92f);
            }
            yield return WalkTo(target);
            yield return LookAround();

            // ときどき、気まぐれに喜ぶ
            float dice = Random.value;
            if (dice < 0.18f)
            {
                yield return DoAction(k_Clap, Random.Range(2.0f, 3.0f));
            }
            else if (dice < 0.32f)
            {
                yield return DoAction(k_Jump, ClipLength("Jump", 1.2f));
            }
        }
    }

    // 同じ場所で喜び続ける
    private IEnumerator CheerRoutine()
    {
        while (true)
        {
            yield return TurnTo(FaceCameraYaw() + (viewX_ < 0.5f ? -20.0f : 20.0f), 0.3f);
            yield return DoAction(k_Spin, ClipLength("4spin", 3.0f));
            yield return DoAction(k_Hold, 1.6f);
            yield return DoAction(k_Clap, 3.0f);
            yield return DoAction(k_Jump, ClipLength("Jump", 1.2f));
            yield return DoAction(k_Jump, ClipLength("Jump", 1.2f));
            yield return DoAction(k_Clap, 2.0f);
        }
    }

    // ときどき横から出てきて、喜んで、帰っていく
    private IEnumerator VisitRoutine()
    {
        SetVisible(false);
        yield return Wait(Random.Range(2.0f, 4.0f));
        while (true)
        {
            bool fromLeft = Random.value < 0.5f;
            float outside = fromLeft ? -0.12f : 1.12f;
            float inside = fromLeft ? 0.07f : 0.93f;

            viewX_ = outside;
            Place();
            SetVisible(true);
            yield return WalkTo(inside);

            // 喜び方をランダムに選ぶ
            int kind = Random.Range(0, 3);
            if (kind == 0)
            {
                yield return DoAction(k_Spin, ClipLength("4spin", 3.0f));
                yield return DoAction(k_Hold, 1.5f);
            }
            else if (kind == 1)
            {
                yield return DoAction(k_Clap, Random.Range(3.0f, 4.5f));
            }
            else
            {
                yield return DoAction(k_Jump, ClipLength("Jump", 1.2f));
                yield return DoAction(k_Jump, ClipLength("Jump", 1.2f));
                yield return DoAction(k_Hold, 1.2f);
            }

            yield return WalkTo(outside);
            SetVisible(false);
            yield return Wait(Random.Range(5.0f, 9.0f));
        }
    }

    // ---- 部品となる動き ----

    // 画面の x（0〜1）まで、歩いていく
    private IEnumerator WalkTo(float targetX)
    {
        float direction = targetX >= viewX_ ? 1.0f : -1.0f;
        float camYaw = camera_.transform.eulerAngles.y;
        yield return TurnTo(camYaw + 90.0f * direction, 0.25f);
        Play(k_Walk);
        while ((targetX - viewX_) * direction > 0.0f)
        {
            viewX_ += direction * walkSpeed_ * Time.deltaTime;
            Place();
            yield return null;
        }
        viewX_ = targetX;
        Place();
        Play(k_Idle);
        yield return TurnTo(FaceCameraYaw(), 0.35f);
    }

    // 立ち止まって、左右を見回す（体の向きを、左右にゆっくり変える）
    private IEnumerator LookAround()
    {
        Play(k_Idle);
        int count = Random.Range(2, 4);
        float side = Random.value < 0.5f ? -1.0f : 1.0f;
        for (int i = 0; i < count; i++)
        {
            yield return TurnTo(FaceCameraYaw() + side * Random.Range(40.0f, 75.0f), 0.55f);
            yield return Wait(Random.Range(0.6f, 1.2f));
            side = -side;
        }
        yield return TurnTo(FaceCameraYaw(), 0.5f);
        yield return Wait(Random.Range(0.4f, 1.0f));
    }

    // 指定の動きを、指定の秒数だけ
    private IEnumerator DoAction(string state, float seconds)
    {
        Play(state);
        yield return Wait(seconds);
        Play(k_Idle);
        yield return Wait(0.15f);
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0.0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    // 体の向きを、指定の角度へ、指定の秒数でなめらかに変える
    private IEnumerator TurnTo(float targetYaw, float seconds)
    {
        float start = yaw_;
        float t = 0.0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            yaw_ = Mathf.LerpAngle(start, targetYaw, Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(t / seconds)));
            Place();
            yield return null;
        }
        yaw_ = targetYaw;
        Place();
    }

    // 見えるか・見えないか（オブジェクトごと消すと、動きの流れが止まってしまうので、見た目だけ消す）
    private void SetVisible(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = visible;
        }
    }

    private void Play(string state)
    {
        if (animator_ != null && gameObject.activeInHierarchy)
        {
            animator_.CrossFadeInFixedTime(state, k_FadeTime, 0, 0.0f);
        }
    }

    // カメラのほうを向く角度
    private float FaceCameraYaw()
    {
        return camera_.transform.eulerAngles.y + 180.0f;
    }

    // 画面上の位置（足元）と向きを、実際の位置に反映する
    private void Place()
    {
        if (camera_ == null)
        {
            return;
        }
        transform.position = camera_.ViewportToWorldPoint(new Vector3(viewX_, feetY_, depth_));
        transform.rotation = Quaternion.Euler(0.0f, yaw_, 0.0f);
    }

    // アニメーターの中のクリップのうち、名前に key を含むものの長さ（秒）
    private float ClipLength(string key, float fallback)
    {
        if (animator_ == null || animator_.runtimeAnimatorController == null)
        {
            return fallback;
        }
        foreach (AnimationClip clip in animator_.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name.Contains(key))
            {
                return clip.length;
            }
        }
        return fallback;
    }

    // キャラクターの見た目の高さ（スケール1のとき）
    private static float MeasureHeight(GameObject obj)
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
