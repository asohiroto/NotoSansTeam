using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    // 重力加速度
    [SerializeField] private Vector3 k_GravityAccel = new Vector3(0.0f, -0.003f, 0.0f);
    // 左右移動の速度
    [SerializeField] private Vector3 k_LRMoveSpeed = new Vector3(-10.0f, 0.0f, 0.0f);
    [SerializeField] GameObject playerObj_;
    Player player_;
    InputAction playerMove_;

    private Vector2 inputValue_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = playerObj_.GetComponent<Player>();
        playerMove_ = InputSystem.actions.FindAction("Move");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        player_.speed_ += k_GravityAccel;

        inputValue_ = playerMove_.ReadValue<Vector2>();

        if (inputValue_.magnitude > 0.0f)
        {
            player_.transform.position += k_LRMoveSpeed * inputValue_.x;
        }
    }
}
