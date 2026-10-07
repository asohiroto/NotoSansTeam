using UnityEngine;

public class FloorBlock : MonoBehaviour
{
    // 壊れるまでに弾が当たる回数
    [SerializeField] private int k_MaxHit = 2;
    // 壊せないブロックか（弾は当たって消えるが、ブロックは壊れない）
    [SerializeField] private bool k_Unbreakable = false;

    // 弾が当たった回数
    private int hitCount_ = 0;

    void OnTriggerEnter(Collider other)
    {
        // プレイヤーの弾以外は無視
        if (other.GetComponent<NormalBulletMove>() == null)
        {
            return;
        }

        // 壊せないブロックは、当たったエフェクトだけ出して終わり
        if (k_Unbreakable)
        {
            EffectManager.PlayHit(other.transform.position);
            return;
        }

        hitCount_++;

        if (hitCount_ >= k_MaxHit)
        {
            EffectManager.PlayBlockBreak(transform.position);
            // ステージのリセットで元に戻せるよう、消さずに非表示にする
            gameObject.SetActive(false);
        }
        else
        {
            // まだ壊れないときは、弾が当たった場所にヒットのエフェクト
            EffectManager.PlayHit(other.transform.position);
        }
    }

    // ステージのリセット: 当たった回数を戻して、再び表示する
    public void ResetBlock()
    {
        hitCount_ = 0;
        gameObject.SetActive(true);
    }
}
