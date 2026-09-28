using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    [Header("ターゲット")]
    // インスペクターで追従対象を割り当てる変数
    [SerializeField] private Transform playerTransform;
    [Header("速度設定")]
    // 敵の移動速度
    [SerializeField] private float moveSpeed = 4.0f;
    // 敵の旋回速度
    [SerializeField] private float turnSpeed = 7.0f;
    [Header("距離設定")]
    // プレイヤーを検知する範囲(検知範囲)
    [SerializeField] private float detectionRange = 10.0f;
    // プレイヤーを見失う距離
    [SerializeField] private float loseRange = 15.0f;
    // プレイヤーに接近したときに立ち止まる距離(停止距離)
    [SerializeField] private float stoppingDistance = 1.5f;

    private bool isChasing = false;

    void Update()
    {
        if (playerTransform == null) return;

        // 敵からプレイヤーへ向かうベクトルを計算（目的地の座標 - 現在地の座標）
        Vector3 toPlayerVec = playerTransform.position - transform.position;
        // 敵とプレイヤーの間の直線距離（ベクトルの長さ）を計算
        float distance = toPlayerVec.magnitude;

        if (!isChasing)
        {
            if (distance <= detectionRange)
            {
                isChasing = true;
            }
        }
        else
        {
            if (distance >= loseRange)
            {
                isChasing = false;
            }
        }

        // プレイヤーとのベクトルの距離がdetectionRangeより多い・小さい場合に、追跡・停止をする
        bool shouldChase = isChasing && (distance > stoppingDistance);

        if (shouldChase)
        {
            Searching(toPlayerVec);
        }
    }

    // 索敵範囲(黄)と停止距離(赤)を円で可視化
    private void OnDrawGizmosSelected()
    {
        // 発見する範囲
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // あきらめる範囲
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, loseRange);

        // 停止距離
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }

    private void Searching(Vector3 toPlayerVec)
    {
        // プレイヤーと重なってしまったときのエラー対策
        if (toPlayerVec != Vector3.zero)
        {
            // プレイヤーに向く角度の計算をしている
            Quaternion targetRotation = Quaternion.LookRotation(toPlayerVec);

            // 現在の角度から目標の角度に、滑らか回転（現在の角度、目標の角度、1フレームに進める角度）
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // 現在の位置からプレイヤーの位置への、１フレームの移動量（現在の位置、目標の位置、1フレームに進む距離(速度＊経過時間))
        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
    }
}