using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] private float moveSpeed = 2.0f;

    [Header("壁・床の判定")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float groundCheckDistance = 1.0f;

    [SerializeField] private LayerMask groundLayer;

    // 1 = 右、-1 = 左
    private int direction = 1;


    void Update()
    {
        Move();
        CheckWall();
        CheckGround();

        // Dキーを押したら死亡(デバッグ用)
        if (Input.GetKeyDown(KeyCode.D))
        {
            Die();
        }
    }


    // ① 左右移動
    private void Move()
    {
        transform.position += Vector3.right
                            * direction
                            * moveSpeed
                            * Time.deltaTime;
    }


    // ② 壁があったら反転
    private void CheckWall()
    {
        Vector3 moveDirection = Vector3.right * direction;

        bool hitWall = Physics.Raycast(
            transform.position,
            moveDirection,
            wallCheckDistance,
            groundLayer
        );

        if (hitWall)
        {
            Turn();
        }
    }


    // ③ 崖だったら反転
    private void CheckGround()
    {
        Vector3 moveDirection = Vector3.right * direction;

        // 敵より少し前の位置
        Vector3 checkPosition =
            transform.position + moveDirection * 0.5f;

        bool groundExists = Physics.Raycast(
            checkPosition,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );

        // 前方に床がなかった
        if (!groundExists)
        {
            Turn();
        }
    }


    // 進行方向を逆にする
    private void Turn()
    {
        direction *= -1;
    }


    // RaycastをScene画面で確認する
    private void OnDrawGizmos()
    {
        Vector3 moveDirection = Vector3.right * direction;

        // 壁チェック
        Gizmos.DrawRay(
            transform.position,
            moveDirection * wallCheckDistance
        );

        // 床チェック
        Vector3 checkPosition =
            transform.position + moveDirection * 0.5f;

        Gizmos.DrawRay(
            checkPosition,
            Vector3.down * groundCheckDistance
        );
    }

    // 敵を死亡させる
    private void Die()
    {
        Destroy(gameObject);
        Debug.Log("敵が死亡した");
    }
}