using UnityEngine;

public class CameraMove : MonoBehaviour
{
    // カメラのプレイヤーまでの距離
    [SerializeField] private float k_distance = 5.0f;
    // モード切り替え時にカメラが回り込むまでの時間（秒）
    [SerializeField] private float k_switchTime = 0.5f;
    // モード切り替え時に背景（壁）が回り終わるまでの時間（秒）。カメラ（k_switchTime）より短くして、先に回し終える
    [SerializeField] private float k_BackgroundSwitchTime = 0.25f;
    // カメラがとらえるプレイヤーの中心の高さ（足元からの高さ）
    [SerializeField] private float k_PlayerCenterHeight = 0.5f;
    // 落下中に下を見る量: 落下速度（Player.speed_.y、1回の物理更新あたり）× この値
    [SerializeField] private float k_LookDownPerSpeed = 10.0f;
    // 下を見る量の上限
    [SerializeField] private float k_MaxLookDown = 3.0f;
    // 下を見る量を目標へ近づける速さ（大きいほど速く追いつく）
    [SerializeField] private float k_LookDownLerpRate = 4.0f;
    // 加速が乗ったときに、さらに下を見る量（最高速で全部足される。速度の割合の2乗で増えるので、加速しきるほど効く）
    [SerializeField] private float k_FastFallExtraLookDown = 1.5f;

    [SerializeField] private GameObject cameraObj_;
    [SerializeField] private GameObject playerObj_;
    // 移動モードの参照先（未設定ならシーンから探す）
    [SerializeField] private PlayerMove playerMove_;
    // 背景（カメラと同じ角度を、カメラより速く、ステージの中心軸のまわりに回す）
    [SerializeField] private GameObject backgroundObj_;

    // 補間の開始角度・目標角度・現在の角度（正面から左回りに回り込んだ角度）
    private float fromYaw_;
    private float toYaw_;
    private float currentYaw_;
    // 現在のカメラの方向
    private Vector3 currentDir_;
    // 切り替え開始からの経過時間
    private float elapsed_;
    // 背景の補間の開始角度・現在の角度・切り替え開始からの経過時間（目標角度はカメラと同じ toYaw_）
    private float backgroundFromYaw_;
    private float backgroundYaw_;
    private float backgroundElapsed_;

    // プレイヤー（落下速度を読むため）と、今どれだけ下を見ているか
    private Player player_;
    private float lookDown_ = 0.0f;

    // 正面モードのカメラの向きと、そのときの背景の位置・回転
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

        player_ = playerObj_.GetComponent<Player>();

        // シーンに置いた背景の状態を、正面モードのときの基準として覚えておく
        frontDir_ = playerObj_.transform.forward;
        if (backgroundObj_ != null)
        {
            backgroundBasePos_ = backgroundObj_.transform.position;
            backgroundBaseRot_ = backgroundObj_.transform.rotation;
        }

        SnapToTarget();

        // 最高速で落ちているときに画面の両端に流れる集中線（カメラに付ける）
        if (cameraObj_ != null)
        {
            SpeedLineEffect speedLine = cameraObj_.GetComponent<SpeedLineEffect>();
            if (speedLine == null)
            {
                speedLine = cameraObj_.AddComponent<SpeedLineEffect>();
            }
            speedLine.Init(player_);
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

        float target = GetTargetYaw();

        // 目標の角度が変わったら、今の角度から補間し直す
        if (target != toYaw_)
        {
            fromYaw_ = currentYaw_;
            toYaw_ = target;
            elapsed_ = 0.0f;
            backgroundFromYaw_ = backgroundYaw_;
            backgroundElapsed_ = 0.0f;
        }

        elapsed_ += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(elapsed_ / k_switchTime);
        currentYaw_ = Mathf.Lerp(fromYaw_, toYaw_, t);
        currentDir_ = Quaternion.Euler(0.0f, currentYaw_, 0.0f) * frontDir_;

        // 背景はカメラより短い時間で同じ角度まで回す
        backgroundElapsed_ += Time.fixedDeltaTime;
        float backgroundT = Mathf.Clamp01(backgroundElapsed_ / k_BackgroundSwitchTime);
        backgroundYaw_ = Mathf.Lerp(backgroundFromYaw_, toYaw_, backgroundT);

        // 落下が速いほど下を見る。床に着く（speed_ が 0 になる）と目標が 0 になり、Lerp でプレイヤーの中心へ戻る
        float targetLookDown = 0.0f;
        if (player_ != null)
        {
            targetLookDown = Mathf.Clamp(-player_.speed_.y * k_LookDownPerSpeed, 0.0f, k_MaxLookDown);

            // 加速が乗るほど（最高速に近いほど）さらに下を見る
            float speedRate = Mathf.Clamp01(player_.speed_.y / player_.MaxFallSpeed);
            targetLookDown += k_FastFallExtraLookDown * speedRate * speedRate;
        }
        // フレームレートに左右されない補間率
        float lerpT = 1.0f - Mathf.Exp(-k_LookDownLerpRate * Time.fixedDeltaTime);
        lookDown_ = Mathf.Lerp(lookDown_, targetLookDown, lerpT);

        PlaceCamera();
        RotateBackground();
    }

    // プレイヤーの中心から currentDir_ 方向に離れた位置に置き、中心より lookDown_ だけ下を見る
    void PlaceCamera()
    {
        Vector3 center = playerObj_.transform.position + Vector3.up * k_PlayerCenterHeight;
        cameraObj_.transform.position = center + currentDir_ * k_distance;
        cameraObj_.transform.LookAt(center + Vector3.down * lookDown_);
    }

    // ステージのリセット: 補間せず、すぐに今のモードの向き（正面）へ戻す
    public void ResetView()
    {
        if (playerObj_ == null)
        {
            return;
        }

        SnapToTarget();
        lookDown_ = 0.0f;

        PlaceCamera();
        RotateBackground();
    }

    // 補間せず、目標の角度にそろえる
    void SnapToTarget()
    {
        currentYaw_ = GetTargetYaw();
        fromYaw_ = currentYaw_;
        toYaw_ = currentYaw_;
        elapsed_ = k_switchTime;
        backgroundYaw_ = currentYaw_;
        backgroundFromYaw_ = currentYaw_;
        backgroundElapsed_ = k_BackgroundSwitchTime;
        currentDir_ = Quaternion.Euler(0.0f, currentYaw_, 0.0f) * frontDir_;
    }

    // 背景を、backgroundYaw_ だけ回す（ステージの中心＝X=0, Z=0 の縦軸まわり）。回し終わるとカメラと同じ角度になる
    void RotateBackground()
    {
        if (backgroundObj_ == null)
        {
            return;
        }

        Quaternion turn = Quaternion.Euler(0.0f, backgroundYaw_, 0.0f);
        Vector3 pivot = new Vector3(0.0f, backgroundBasePos_.y, 0.0f);

        backgroundObj_.transform.position = pivot + turn * (backgroundBasePos_ - pivot);
        backgroundObj_.transform.rotation = turn * backgroundBaseRot_;
    }

    // 移動モードに応じたカメラの角度（切り替えるたびに左へ回る）
    float GetTargetYaw()
    {
        if (playerMove_ != null)
        {
            return playerMove_.CameraYaw;
        }
        return 0.0f;
    }
}
