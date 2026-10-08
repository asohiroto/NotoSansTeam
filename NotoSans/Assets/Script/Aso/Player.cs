using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // 初期位置
    [SerializeField] private Vector3 kFirstPosition_ = new Vector3(0.0f, 10.0f, 0.0f);
    // プレイヤー下降速度の最大値
    [SerializeField] private Vector3 kPlayerMinSpeed_ = new Vector3(0.0f, -0.3f, 0.0f);
    // 重力加速度
    [SerializeField] private Vector3 kGravityAccel_ = new Vector3(0.0f, -0.003f, 0.0f);
    // 最大弾薬数
    [SerializeField] public int k_MaxAmmunition = 3;
    // 最大体力
    [SerializeField] public int k_MaxHp = 3;
    // 壁や床の手前で止まるときに空ける隙間
    [SerializeField] private float kSkinWidth_ = 0.01f;
    // 足元に床があるか調べる距離
    [SerializeField] private float kGroundCheckDistance_ = 0.05f;
    private GameObject player_;
    private Rigidbody rigidbody_;

    // 残弾数
    public int remainAmmunition_ = 0;
    // 体力
    public int hp_ = 0;
    InputAction playerDamage_;
    // 速度
    public Vector3 speed_ = Vector3.zero;
    // ジャンプ時の速度
    public Vector3 jumpSpeed_ = Vector3.zero;
    // 床の上にいるか
    public bool isGround_ = false;

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
        rigidbody_ = GetComponent<Rigidbody>();
        player_.transform.position = kFirstPosition_;
        remainAmmunition_ = k_MaxAmmunition;
        hp_ = k_MaxHp;
        playerDamage_ = InputSystem.actions.FindAction("Damage");
    }

    void Update()
    {

    }

    // 初期位置・体力・弾薬・速度・状態を最初の状態に戻す（StageManager から呼ぶ）
    public void Respawn()
    {
        transform.position = kFirstPosition_;
        hp_ = k_MaxHp;
        remainAmmunition_ = k_MaxAmmunition;
        speed_ = Vector3.zero;
        jumpSpeed_ = Vector3.zero;
        isGround_ = false;
        state_ = PlayerState.Air;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        UpdateAir();
    }

    // delta だけ移動する。途中に何かあれば、その手前で止まって true を返す
    public bool MoveWithCollision(Vector3 delta, out RaycastHit hit)
    {
        hit = new RaycastHit();
        float distance = delta.magnitude;
        if (distance <= 0.0f)
        {
            return false;
        }

        // 直前に動かしたTransformを物理側に反映してから調べる
        Physics.SyncTransforms();

        Vector3 dir = delta / distance;
        if (rigidbody_.SweepTest(dir, out hit, distance + kSkinWidth_, QueryTriggerInteraction.Ignore))
        {
            player_.transform.position += dir * Mathf.Max(hit.distance - kSkinWidth_, 0.0f);
            return true;
        }

        player_.transform.position += delta;
        return false;
    }


    // 空中：重力で落下し、落下中に床に触れたら着地
    void UpdateAir()
    {
        speed_ += kGravityAccel_;

        if (speed_.y <= kPlayerMinSpeed_.y)
        {
            speed_.y = kPlayerMinSpeed_.y;
        }

        RaycastHit hit;
        if (MoveWithCollision(new Vector3(0.0f, speed_.y, 0.0f), out hit))
        {
            // エフェクトは体の中心（足元から 0.5 上）で再生する
            Vector3 bodyCenter = transform.position + Vector3.up * 0.5f;

            // 落下中に床に当たったら着地
            if (speed_.y <= 0.0f && hit.collider.CompareTag("Floor"))
            {
                EffectManager.PlayDeath(bodyCenter);
                // プレイヤーを隠し、少し待ってからステージをリセットして初期位置に戻す
                StageManager.OnPlayerDied();
                return;
            }

            // 上昇中に床の下から当たったら、頭をぶつけて上昇をやめる
            speed_.y = 0.0f;
        }
    }

}
