using UnityEngine;

public class P : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.0f;   // 水平移動の速度
    [SerializeField] private float turnSpeed = 10.0f;  // 振り向く速度
    [SerializeField] private float flySpeed = 4.0f;    // 浮上・下降の速度

    void Update()
    {
        // 1. キーボードの入力を取得（A/D で左右、W/S で前後）
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 2. 水平方向の移動ベクトルを作る
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // キーが押されている場合のみ向きを変えて移動
        if (moveDirection.magnitude > 0.1f)
        {
            // 移動する方向へ体を滑らかに向ける
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            // 前後左右に移動
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // 3. スペースキーを押している間、上へ浮上（空を飛ぶ）
        if (Input.GetKey(KeyCode.Space))
        {
            transform.position += Vector3.up * flySpeed * Time.deltaTime;
        }

        // 4. 左Shiftキーを押している間、下へ下降（着地用）
        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.position += Vector3.down * flySpeed * Time.deltaTime;
        }
    }
}