using UnityEngine;

public class FloorBlock : MonoBehaviour
{
    // 壊れるまでに弾が当たる回数
    [SerializeField] private int k_MaxHit = 2;

    // 弾が当たった回数
    private int hitCount_ = 0;

    void OnTriggerEnter(Collider other)
    {
        // プレイヤーの弾以外は無視
        if (other.GetComponent<NormalBulletMove>() == null)
        {
            return;
        }

        hitCount_++;

        if (hitCount_ >= k_MaxHit)
        {
            Destroy(gameObject);
        }
    }
}
