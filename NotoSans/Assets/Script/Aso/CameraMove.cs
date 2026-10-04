using UnityEngine;

public class CameraMove : MonoBehaviour
{
    // カメラのプレイヤーまでの距離
    [SerializeField] private float k_distance = 5.0f;
    // モード切り替え時にカメラが回り込むまでの時間（秒）
    [SerializeField] private float k_switchTime = 0.5f;

    [SerializeField] private GameObject cameraObj_;
    [SerializeField] private GameObject playerObj_;
    // 移動モードの参照先（未設定ならシーンから探す）
    [SerializeField] private PlayerMove playerMove_;

    // 補間の開始方向・目標方向・現在の方向
    private Vector3 fromDir_;
    private Vector3 toDir_;
    private Vector3 currentDir_;
    // 切り替え開始からの経過時間
    private float elapsed_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log(playerObj_.transform.position);

        if (playerMove_ == null)
        {
            playerMove_ = FindFirstObjectByType<PlayerMove>();
        }

        currentDir_ = GetTargetDir();
        fromDir_ = currentDir_;
        toDir_ = currentDir_;
        elapsed_ = k_switchTime;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 target = GetTargetDir();

        // 目標方向が変わったら、今の方向から補間し直す
        if (target != toDir_)
        {
            fromDir_ = currentDir_;
            toDir_ = target;
            elapsed_ = 0.0f;
        }

        elapsed_ += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed_ / k_switchTime);
        // 方向をLerpし、長さを1に戻して距離を一定に保つ
        currentDir_ = Vector3.Lerp(fromDir_, toDir_, t).normalized;

        cameraObj_.transform.position = playerObj_.transform.position + currentDir_ * k_distance;
        cameraObj_.transform.LookAt(playerObj_.transform.position);
    }

    // 移動モードに応じたカメラの方向
    Vector3 GetTargetDir()
    {
        // 左モードでは、正面から見て左側（プレイヤーの右側）から見る
        if (playerMove_ != null && playerMove_.CurrentMoveMode == PlayerMove.MoveMode.Left)
        {
            return playerObj_.transform.right;
        }
        return playerObj_.transform.forward;
    }
}
