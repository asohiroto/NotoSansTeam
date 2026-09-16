using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // インスペクターで追従対象を割り当てる変数
    [SerializeField] private Transform playerTransform;
    // 敵の移動速度
    [SerializeField] private float moveSpeed = 4.0f;
    // 敵の旋回速度
    [SerializeField] private float turnSpeed = 7.0f;
    // プレイヤーに接近したときに立ち止まる距離
    [SerializeField] private float stoppingDistance = 1.5f;

    void Update()
    {
        // 敵からプレイヤーへ向かうベクトルを計算（目的地の座標 - 現在地の座標）
        Vector3 diff = playerTransform.position - transform.position;

        // 高さ(Y軸)をゼロにする
        diff.y = 0f;

        // 敵とプレイヤーの間の直線距離（ベクトルの長さ）を計算
        float distance = diff.magnitude;

        if (distance > stoppingDistance)
        {
            if (diff != Vector3.zero)
            {

                Quaternion targetRotation = Quaternion.LookRotation(diff);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
        }
    }
}

