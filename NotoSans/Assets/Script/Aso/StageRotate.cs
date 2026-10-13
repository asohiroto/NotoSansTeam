using System.Collections.Generic;
using UnityEngine;

// モード切り替えのとき、床・壁・アイテムを含むステージ全体を、ステージの中心軸（X=0, Z=0 の縦軸）のまわりに回す
// カメラの回り込み（CameraMove）より速く回し終える
// シーンに置かなくても、StageManager が自分に付けて使う
public class StageRotate : MonoBehaviour
{
    // ステージを回し終えるまでの時間（秒）。カメラの回り込み（CameraMove の k_switchTime）より短くする
    [SerializeField] private float k_TurnTime = 0.25f;
    // 回すオブジェクト（空なら、シーンの一番上のオブジェクトのうち、プレイヤー・カメラ・ライト・背景以外をすべて回す）
    [SerializeField] private List<GameObject> k_StageObjs = new List<GameObject>();
    // 移動モードの参照先（未設定ならシーンから探す）
    [SerializeField] private PlayerMove playerMove_;

    // 回す前（正面モード）の位置・回転
    private Vector3[] basePos_;
    private Quaternion[] baseRot_;

    // 補間の開始角度・目標角度・現在の角度
    private float fromYaw_;
    private float toYaw_;
    private float currentYaw_;
    // 切り替え開始からの経過時間
    private float elapsed_;

    void Start()
    {
        if (playerMove_ == null)
        {
            playerMove_ = FindAnyObjectByType<PlayerMove>();
        }

        if (k_StageObjs.Count == 0)
        {
            CollectStageObjects();
        }

        // シーンに置いた状態を、正面モードのときの基準として覚えておく
        basePos_ = new Vector3[k_StageObjs.Count];
        baseRot_ = new Quaternion[k_StageObjs.Count];
        for (int i = 0; i < k_StageObjs.Count; i++)
        {
            if (k_StageObjs[i] != null)
            {
                basePos_[i] = k_StageObjs[i].transform.position;
                baseRot_[i] = k_StageObjs[i].transform.rotation;
            }
        }

        ResetRotation();
    }

    // シーンの一番上のオブジェクトから、ステージとして回すものを集める
    void CollectStageObjects()
    {
        GameObject background = null;
        CameraMove cameraMove = FindAnyObjectByType<CameraMove>();
        if (cameraMove != null)
        {
            background = cameraMove.Background;
        }

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            // プレイヤー・カメラ・ライト・背景は回さない
            if (root == background
                || root.GetComponentInChildren<Player>(true) != null
                || root.GetComponentInChildren<Camera>(true) != null
                || root.GetComponentInChildren<Light>(true) != null)
            {
                continue;
            }
            // 見た目も当たり判定もないもの（マネージャーなど）は回さない
            if (root.GetComponentInChildren<Renderer>(true) == null
                && root.GetComponentInChildren<Collider>(true) == null)
            {
                continue;
            }
            k_StageObjs.Add(root);
        }
    }

    void FixedUpdate()
    {
        float target = GetTargetYaw();

        // 目標の角度が変わったら、今の角度から補間し直す
        if (target != toYaw_)
        {
            fromYaw_ = currentYaw_;
            toYaw_ = target;
            elapsed_ = 0.0f;
        }

        if (elapsed_ >= k_TurnTime)
        {
            return;
        }

        elapsed_ += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed_ / k_TurnTime);
        currentYaw_ = Mathf.Lerp(fromYaw_, toYaw_, t);
        Apply();
    }

    // ステージのリセット: 補間せず、すぐに今のモードの角度（正面）へ戻す
    public void ResetRotation()
    {
        currentYaw_ = GetTargetYaw();
        fromYaw_ = currentYaw_;
        toYaw_ = currentYaw_;
        elapsed_ = k_TurnTime;
        Apply();
    }

    // 回す前の位置・回転を、ステージの中心軸のまわりに currentYaw_ だけ回して置く
    void Apply()
    {
        if (basePos_ == null)
        {
            return;
        }

        Quaternion turn = Quaternion.Euler(0.0f, currentYaw_, 0.0f);
        for (int i = 0; i < k_StageObjs.Count; i++)
        {
            if (k_StageObjs[i] == null)
            {
                continue;
            }
            Vector3 pivot = new Vector3(0.0f, basePos_[i].y, 0.0f);
            k_StageObjs[i].transform.position = pivot + turn * (basePos_[i] - pivot);
            k_StageObjs[i].transform.rotation = turn * baseRot_[i];
        }
        // 動かした床の当たり判定を、すぐに新しい位置へ合わせる
        Physics.SyncTransforms();
    }

    float GetTargetYaw()
    {
        if (playerMove_ != null)
        {
            return playerMove_.StageYaw;
        }
        return 0.0f;
    }
}
