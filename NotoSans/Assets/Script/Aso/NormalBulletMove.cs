using UnityEngine;

public class NormalBulletMove : MonoBehaviour
{
    // 弾の速度
    [SerializeField] private Vector3 k_BulletSpeed = new Vector3(0.0f, -0.35f, 0.0f);
    // 弾の最大飛翔距離（計算式は速度*1秒当たりのフレーム数*秒数）
    [SerializeField] private float k_MaxBulletDistance = 0.35f * 60.0f * 2.0f;

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
        bullet_.transform.position += k_BulletSpeed;
        bulletDistance_ += k_BulletSpeed.y;

        if (bulletDistance_ > k_MaxBulletDistance)
        {
            Destroy(gameObject);
        }
    }
}
