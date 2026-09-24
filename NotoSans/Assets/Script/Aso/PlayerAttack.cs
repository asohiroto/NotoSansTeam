using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    static private readonly Vector3 k_JumpAccel = new Vector3(0.0f, 1.0f, 0.0f);

    [SerializeField] private GameObject playerObj_;
    private Player player_;

    InputAction playerJump_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = playerObj_.GetComponent<Player>();
        playerJump_ = InputSystem.actions.FindAction("Jump");
    }

    // Update is called once per frame
    void Update()
    {
        if (playerJump_.WasPressedThisFrame())
        {
            player_.speed_ += k_JumpAccel;
        }
    }
}
