using UnityEngine;

public class Player : MonoBehaviour
{
    // 初期位置
    [SerializeField] private Vector3 k_firstPosition = new Vector3(0.0f, 10.0f, 0.0f);
    // プレイヤー下降速度の最大値
    [SerializeField] private Vector3 k_PlayerMinSpeed = new Vector3(0.0f, -0.3f, 0.0f);
    // 最大弾薬数
    [SerializeField] private int k_MaxAmmunition = 3;
    private GameObject player_;

    // 残弾数
    public int remainAmmunition_ = 0;
    // 速度
    public Vector3 speed_ = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = gameObject;
        player_.transform.position = k_firstPosition;
        remainAmmunition_ = k_MaxAmmunition;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (speed_.y <= k_PlayerMinSpeed.y)
        {
            speed_.y = k_PlayerMinSpeed.y;
        }
        player_.transform.position += speed_;
    }
}
