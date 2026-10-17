using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    // ジャンプ時の上向き加速度
    [SerializeField] private Vector3 k_JumpAccel = new Vector3(0.0f, 0.0f, 0.0f);
    // 発射（弾の生成）を有効にするか。今は×ボタンをアピールに使うため無効
    [SerializeField] private bool k_EnableAttack = false;

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

        // 発射が無効なら、弾を出さない
        if (!k_EnableAttack)
        {
            return;
        }

        // ポーズ中、およびポーズを解除したフレームは撃たない（解除の決定ボタンで弾が出ないように）
        if (GameHud.IsPaused || Time.frameCount <= GameHud.LastChangeFrame)
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