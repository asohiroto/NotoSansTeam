using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // プレイヤーの移動速度
    private static readonly Vector3 k_GravityAccel = new Vector3(0.0f, -0.01f, 0.0f);

    [SerializeField] GameObject playerObj_;
    Player player_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = playerObj_.GetComponent<Player>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        player_.speed_ += k_GravityAccel;
    }
}
