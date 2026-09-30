using UnityEngine;

public class Player : MonoBehaviour
{
    // 初期位置
    [SerializeField] private Vector3 kFirstPosition_ = new Vector3(0.0f, 10.0f, 0.0f);
    // プレイヤー下降速度の最大値
    [SerializeField] private Vector3 kPlayerMinSpeed_ = new Vector3(0.0f, -0.3f, 0.0f);
    // プレイヤーの左右移動限界
    [SerializeField] private float kPlayerLimitXPos_ = 1.0f;
    // 最大弾薬数
    [SerializeField] public int k_MaxAmmunition = 3;
    private GameObject player_;

    // 残弾数
    public int remainAmmunition_ = 0;
    // 速度
    public Vector3 speed_ = Vector3.zero;
    // 床の上にいるか
    public bool isGround_ = false;
    // 左右移動の限界値保存用
    private float xMoveLimit_ = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = gameObject;
        player_.transform.position = kFirstPosition_;
        remainAmmunition_ = k_MaxAmmunition;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (speed_.y <= kPlayerMinSpeed_.y)
        {
            speed_.y = kPlayerMinSpeed_.y;
        }

        xMoveLimit_ = player_.transform.position.x;
        if (player_.transform.position.x > kPlayerLimitXPos_)
        {
            xMoveLimit_ = kPlayerLimitXPos_;
        }
        else if (player_.transform.position.x < -kPlayerLimitXPos_)
        {
            xMoveLimit_ = -kPlayerLimitXPos_;
        }
        player_.transform.position = new Vector3(xMoveLimit_, player_.transform.position.y, player_.transform.position.z);

        if (isGround_)
        {
            speed_.y = 0.0f;
            isGround_ = false;
        }
        player_.transform.position += speed_;
    }
}
