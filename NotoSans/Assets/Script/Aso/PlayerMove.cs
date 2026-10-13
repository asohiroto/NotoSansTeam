using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    // 左右移動の速度
    [SerializeField] private Vector3 k_LRMoveSpeed = new Vector3(-10.0f, 0.0f, 0.0f);
    [SerializeField] GameObject playerObj_;
    Player player_;
    InputAction playerMove_;

    private Vector2 inputValue_;

    // ステージをどの向きから見ているか。切り替えるたびに左へ90度ずつ回る（Front→Left→Back→Right→Front）
    public enum MoveMode
    {
        // 正面から見る
        Front,
        // 正面から見て左から見る（左へ90度）
        Left,
        // 後ろから見る（左へ180度）
        Back,
        // 正面から見て右から見る（左へ270度）
        Right,
    }

    // 現在の移動モード
    private MoveMode moveMode_ = MoveMode.Front;
    // 現在の移動モード（カメラなど外部から参照する用）
    public MoveMode CurrentMoveMode { get { return moveMode_; } }

    // 1回の切り替え（左へ90度）のうち、ステージを回して見せる割合（残りはカメラが回り込む）
    [SerializeField, Range(0.0f, 1.0f)] private float k_StageTurnRatio = 0.5f;
    // 今までに切り替えた回数（角度を 270度→0度 と戻さず、回り続けるために数える）
    private int turnCount_ = 0;

    // カメラが正面から回り込む角度（左回り）
    public float CameraYaw { get { return 90.0f * turnCount_ * (1.0f - k_StageTurnRatio); } }
    // ステージを回す角度（カメラと逆向きに回して、残りの角度を見せる）
    public float StageYaw { get { return -90.0f * turnCount_ * k_StageTurnRatio; } }

    // カメラを置く方向（プレイヤーから見た向き）。正面の向きを CameraYaw だけ回したもの
    public Vector3 ViewDirection
    {
        get
        {
            Vector3 front = player_ != null ? player_.transform.forward : Vector3.forward;
            return Quaternion.Euler(0.0f, CameraYaw, 0.0f) * front;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerObj_ != null)
        {
            player_ = playerObj_.GetComponent<Player>();
        }
        playerMove_ = InputSystem.actions.FindAction("Move");
    }

    // 見る向きを左へ90度回す（モード切り替えアイテムを取ったときに呼ぶ）
    public void TurnLeft()
    {
        // プレイヤーが存在しない、または倒されて隠れている（リスポーン待ち）なら何もしない
        if (player_ == null || !player_.gameObject.activeInHierarchy)
        {
            return;
        }

        ChangeMoveMode((MoveMode)(((int)moveMode_ + 1) % 4));

        // ステージを回したら3回転ポーズを決める
        PlayerAppeal appeal = player_.GetComponent<PlayerAppeal>();
        if (appeal != null)
        {
            appeal.PlayStageTurnPose();
        }
    }

    void FixedUpdate()
    {
        // プレイヤーが存在しない、または倒されて隠れている（リスポーン待ち）なら何もしない
        if (player_ == null || !player_.gameObject.activeInHierarchy)
        {
            return;
        }

        // 重力はPlayerの空中状態で処理する
        inputValue_ = playerMove_.ReadValue<Vector2>();

        if (inputValue_.magnitude > 0.0f)
        {
            // 画面の右方向（カメラの右）。カメラは ViewDirection の位置からプレイヤーを見ている
            Vector3 screenRight = Vector3.Cross(Vector3.up, -ViewDirection).normalized;
            Vector3 move = screenRight * k_LRMoveSpeed.magnitude * inputValue_.x;

            // 横から床に当たったら、その手前で止まる
            RaycastHit hit;
            player_.MoveWithCollision(move, out hit);
        }
    }

    void ChangeMoveMode(MoveMode next)
    {
        Quaternion oldStage = Quaternion.Euler(0.0f, StageYaw, 0.0f);
        moveMode_ = next;
        turnCount_++;
        Quaternion newStage = Quaternion.Euler(0.0f, StageYaw, 0.0f);

        // ステージから見た位置で、奥行き方向（ステージから見たカメラの向き）の位置を0にし、
        // 回し終わったステージの上の位置に置く
        Vector3 local = Quaternion.Inverse(oldStage) * player_.transform.position;
        Vector3 depthAxis = Quaternion.Inverse(newStage) * ViewDirection;
        local -= depthAxis * Vector3.Dot(local, depthAxis);
        player_.transform.position = newStage * local;

        // 切り替えのエフェクト（プレイヤーについていく）
        EffectManager.PlayModeSwitch(player_.transform);
    }

    // ステージのリセット: 正面モードに戻す（エフェクトは出さない）
    public void ResetMode()
    {
        moveMode_ = MoveMode.Front;
        turnCount_ = 0;
    }
}
