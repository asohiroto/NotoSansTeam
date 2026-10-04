using UnityEngine;

public class Player : MonoBehaviour
{
    // 初期位置
    [SerializeField] private Vector3 kFirstPosition_ = new Vector3(0.0f, 10.0f, 0.0f);
    // プレイヤー下降速度の最大値
    [SerializeField] private Vector3 kPlayerMinSpeed_ = new Vector3(0.0f, -0.3f, 0.0f);
    // 重力加速度
    [SerializeField] private Vector3 kGravityAccel_ = new Vector3(0.0f, -0.003f, 0.0f);
    // プレイヤーの左右移動限界
    [SerializeField] private float kPlayerLimitXPos_ = 1.0f;
    [SerializeField] private float kPlayerLimitYPos_ = 1.0f;
    [SerializeField] private float kPlayerLimitZPos_ = 1.0f;
    // 行動制限の基準にする床（設定するとX・Zの制限をこの床の幅に合わせる）
    [SerializeField] private GameObject limitFloorObj_;
    // 最大弾薬数
    [SerializeField] public int k_MaxAmmunition = 3;
    private GameObject player_;

    // 残弾数
    public int remainAmmunition_ = 0;
    // 速度
    public Vector3 speed_ = Vector3.zero;
    // ジャンプ時の速度
    public Vector3 jumpSpeed_ = Vector3.zero;
    // 床の上にいるか
    public bool isGround_ = false;
    // 左右移動の限界値保存用
    private float xMoveLimit_ = 0.0f;
    private float yMoveLimit_ = 0.0f;

    enum PlayerState
    {
        // 空中
        Air,
        // 地面の上
        Ground,
        // ジャンプした瞬間
        JumpStart,
    }

    // 現在の状態
    private PlayerState state_ = PlayerState.Air;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = gameObject;
        player_.transform.position = kFirstPosition_;
        remainAmmunition_ = k_MaxAmmunition;

        if (limitFloorObj_ != null)
        {
            // 床の横幅の半分を左右の限界にする（左モードのZ方向も同じ幅）
            float halfWidth = limitFloorObj_.GetComponent<Collider>().bounds.extents.x;
            kPlayerLimitXPos_ = halfWidth;
            kPlayerLimitZPos_ = halfWidth;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        PositionLimitCorrection();

        switch (state_)
        {
            case PlayerState.Air:
                UpdateAir();
                break;
            case PlayerState.Ground:
                UpdateGround();
                break;
            case PlayerState.JumpStart:
                UpdateJumpStart();
                break;
        }

        // 接地判定は毎フレームPlayerFloorCollisionが立て直す
        isGround_ = false;
    }

    // ジャンプを開始する（PlayerAttackから呼ぶ）
    public void Jump(Vector3 jumpSpeed)
    {
        jumpSpeed_ = jumpSpeed;
        state_ = PlayerState.JumpStart;
    }

    // 空中：重力で落下し、落下中に床に触れたら着地
    void UpdateAir()
    {
        speed_ += kGravityAccel_;

        if (speed_.y <= kPlayerMinSpeed_.y)
        {
            speed_.y = kPlayerMinSpeed_.y;
        }

        // 上昇中は床に触れていても着地しない（ジャンプ直後の床接触で止まらないように）
        if (isGround_ && speed_.y <= 0.0f)
        {
            ChangeState(PlayerState.Ground);
            return;
        }

        player_.transform.position += speed_;
    }

    // 地面の上：止まって弾を補充し、床から離れたら空中へ
    void UpdateGround()
    {
        if (!isGround_)
        {
            ChangeState(PlayerState.Air);
        }
    }

    // ジャンプした瞬間：上向きの速度を1回だけ与えて空中へ
    void UpdateJumpStart()
    {
        speed_ = jumpSpeed_;
        jumpSpeed_ = Vector3.zero;
        player_.transform.position += speed_;
        ChangeState(PlayerState.Air);
    }

    void ChangeState(PlayerState next)
    {
        state_ = next;

        // 状態に入った瞬間の処理
        if (next == PlayerState.Ground)
        {
            speed_ = Vector3.zero;
            remainAmmunition_ = k_MaxAmmunition;
        }
    }

    void PositionLimitCorrection()
    {
        xMoveLimit_ = player_.transform.position.x;
        yMoveLimit_ = player_.transform.position.y;

        if (player_.transform.position.x > kPlayerLimitXPos_)
        {
            xMoveLimit_ = kPlayerLimitXPos_;
        }
        else if (player_.transform.position.x < -kPlayerLimitXPos_)
        {
            xMoveLimit_ = -kPlayerLimitXPos_;
        }

        if (player_.transform.position.y > kPlayerLimitYPos_)
        {
            yMoveLimit_ = kPlayerLimitYPos_;
        }
        else if (player_.transform.position.y < -kPlayerLimitYPos_)
        {
            yMoveLimit_ = -kPlayerLimitYPos_;
        }

        float zMoveLimit = Mathf.Clamp(player_.transform.position.z, -kPlayerLimitZPos_, kPlayerLimitZPos_);

        player_.transform.position = new Vector3(xMoveLimit_, player_.transform.position.y, zMoveLimit);
    }
}
