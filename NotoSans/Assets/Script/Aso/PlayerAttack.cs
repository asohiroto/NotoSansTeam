using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    // ジャンプ時の上向き加速度
    [SerializeField] private Vector3 k_JumpAccel = new Vector3(0.0f, 0.0f, 0.0f);

    [SerializeField] private GameObject playerObj_;
    [SerializeField] private GameObject bulletPrefab_;
    private Player player_;
    InputAction playerJump_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerObj_ != null)
        {
            player_ = playerObj_.GetComponent<Player>();
        }
        playerJump_ = InputSystem.actions.FindAction("Jump");
    }

    // Update is called once per frame
    void Update()
    {
        // プレイヤーが存在しない、または倒されて隠れている（リスポーン待ち）なら何もしない
        if (player_ == null || !player_.gameObject.activeInHierarchy)
        {
            return;
        }

        if (playerJump_.WasPressedThisFrame())
        {
            if (player_.remainAmmunition_ >= 1)
            {
                GenerateBullet(bulletPrefab_);
            }
        }

        // 特定の球を生成する関数
        void GenerateBullet(GameObject bullet)
        {
            Instantiate(bullet, player_.transform.position, Quaternion.identity);
            EffectManager.PlayMuzzle(player_.transform.position);
        }
    }
}