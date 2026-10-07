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
    // 背景（カメラと同じ角度だけ、ステージの中心軸のまわりを回す）
    [SerializeField] private GameObject backgroundObj_;

    // 補間の開始方向・目標方向・現在の方向
    private Vector3 fromDir_;
    private Vector3 toDir_;
    private Vector3 currentDir_;
    // 切り替え開始からの経過時間
    private float elapsed_;

    // 背景を回す基準（正面モードのカメラの向きと、そのときの背景の位置・回転）
    private Vector3 frontDir_;
    private Vector3 backgroundBasePos_;
    private Quaternion backgroundBaseRot_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerMove_ == null)
        {
            playerMove_ = FindAnyObjectByType<PlayerMove>();
        }

        // プレイヤーが存在しなければ何もしない
        if (playerObj_ == null)
        {
            return;
        }

        Debug.Log(playerObj_.transform.position);

        currentDir_ = GetTargetDir();
        fromDir_ = currentDir_;
        toDir_ = currentDir_;
        elapsed_ = k_switchTime;

        // シーンに置いた背景の状態を、正面モードのときの基準として覚えておく
        frontDir_ = playerObj_.transform.forward;
        if (backgroundObj_ != null)
        {
            backgroundBasePos_ = backgroundObj_.transform.position;
            backgroundBaseRot_ = backgroundObj_.transform.rotation;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // プレイヤーが倒されて存在しなければ、カメラはその場で止める
        if (playerObj_ == null)
        {
            return;
        }

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

        RotateBackground();
    }

    // ステージのリセット: 補間せず、すぐに今のモードの向き（正面）へ戻す
    public void ResetView()
    {
        if (playerObj_ == null)
        {
            return;
        }

        currentDir_ = GetTargetDir();
        fromDir_ = currentDir_;
        toDir_ = currentDir_;
        elapsed_ = k_switchTime;

        cameraObj_.transform.position = playerObj_.transform.position + currentDir_ * k_distance;
        cameraObj_.transform.LookAt(playerObj_.transform.position);
        RotateBackground();
    }

    // 背景を、カメラが正面から回り込んだ角度と同じだけ回す（ステージの中心＝X=0, Z=0 の縦軸まわり）
    void RotateBackground()
    {
        if (backgroundObj_ == null)
        {
            return;
        }

        float yaw = Vector3.SignedAngle(frontDir_, currentDir_, Vector3.up);
        Quaternion turn = Quaternion.Euler(0.0f, yaw, 0.0f);
        Vector3 pivot = new Vector3(0.0f, backgroundBasePos_.y, 0.0f);

        backgroundObj_.transform.position = pivot + turn * (backgroundBasePos_ - pivot);
        backgroundObj_.transform.rotation = turn * backgroundBaseRot_;
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
