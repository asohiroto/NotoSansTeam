using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    // 左右移動の速度
    [SerializeField] private Vector3 k_LRMoveSpeed = new Vector3(-10.0f, 0.0f, 0.0f);
    [SerializeField] GameObject playerObj_;
    Player player_;
    InputAction playerMove_;
    InputAction playerPitch_;

    private Vector2 inputValue_;

    public enum MoveMode
    {
        // 正面モード：左右入力でX軸方向に移動
        Front,
        // 左モード：左右入力でZ軸方向に移動
        Left,
    }

    // 現在の移動モード
    private MoveMode moveMode_ = MoveMode.Front;
    // 現在の移動モード（カメラなど外部から参照する用）
    public MoveMode CurrentMoveMode { get { return moveMode_; } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = playerObj_.GetComponent<Player>();
        playerMove_ = InputSystem.actions.FindAction("Move");
        playerPitch_ = InputSystem.actions.FindAction("Pitch");
    }

    // Update is called once per frame
    void Update()
    {
        // 押した瞬間を取りこぼさないようにUpdateで判定する
        if (playerPitch_.WasPressedThisFrame())
        {
            if (moveMode_ == MoveMode.Front)
            {
                ChangeMoveMode(MoveMode.Left);
            }
            else
            {
                ChangeMoveMode(MoveMode.Front);
            }
        }
    }

    void FixedUpdate()
    {
        // 重力はPlayerの空中状態で処理する
        inputValue_ = playerMove_.ReadValue<Vector2>();

        if (inputValue_.magnitude > 0.0f)
        {
            switch (moveMode_)
            {
                case MoveMode.Front:
                    player_.transform.position += k_LRMoveSpeed * inputValue_.x;
                    break;
                case MoveMode.Left:
                    // カメラが左から見ているので、画面の右がZのプラスになるよう符号を反転する
                    player_.transform.position += new Vector3(0.0f, 0.0f, -k_LRMoveSpeed.x) * inputValue_.x;
                    break;
            }
        }
    }

    void ChangeMoveMode(MoveMode next)
    {
        moveMode_ = next;

        Vector3 pos = player_.transform.position;
        switch (next)
        {
            case MoveMode.Front:
                // 正面に切り替えるときはZを0に
                pos.z = 0.0f;
                break;
            case MoveMode.Left:
                // 左に切り替えるときはXを0に
                pos.x = 0.0f;
                break;
        }
        player_.transform.position = pos;
    }
}
