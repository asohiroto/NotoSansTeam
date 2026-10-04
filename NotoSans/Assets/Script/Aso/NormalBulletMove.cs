using UnityEngine;

public class NormalBulletMove : MonoBehaviour
{
    // 弾の速度（1秒あたりの移動量）
    [SerializeField] private Vector3 k_BulletSpeed = new Vector3(0.0f, -17.5f, 0.0f);
    // 弾の最大飛翔距離（計算式は速さ*秒数）
    [SerializeField] private float k_MaxBulletDistance = 17.5f * 2.0f;

    private GameObject bullet_;
    // 弾の飛翔距離
    private float bulletDistance_ = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bullet_ = gameObject;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 move = k_BulletSpeed * Time.fixedDeltaTime;
        bullet_.transform.position += move;
        // 向きに関係なく移動量（長さ）を足す
        bulletDistance_ += move.magnitude;

        if (bulletDistance_ > k_MaxBulletDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 床ブロックに当たったら弾は消える（ブロック側で当たった回数を数える）
        if (other.GetComponent<FloorBlock>() != null)
        {
            Destroy(gameObject);
        }
    }
}
