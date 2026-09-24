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

        // 敵とプレイヤーの間の直線距離（ベクトルの長さ）を計算
        float distance = diff.magnitude;

        // プレイヤーが、停止距離外にいる場合に以下を実行
        if (distance > stoppingDistance)
        {
            // プレイヤーと重なってしまったときのエラー対策
            if (diff != Vector3.zero)
            {
                // プレイヤーに向く角度の計算をしている
                Quaternion targetRotation = Quaternion.LookRotation(diff);

                // 現在の角度から目標の角度に、滑らか回転（現在の角度、目標の角度、1フレームに進める角度）
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            // 現在の位置からプレイヤーの位置への、１フレームの移動量（現在の位置、目標の位置、1フレームに進む距離(速度＊経過時間))
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
        }
    }
}

