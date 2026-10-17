using UnityEngine;

// クリアのロゴの動き。最初にぽんっと大きく登場して、そのあとはゆっくり左右に揺れながら、少し上下に浮く
public class LogoSway : MonoBehaviour
{
    // 登場にかける時間（秒）。少し大きくなりすぎてから、ちょうどの大きさに落ち着く
    [SerializeField] private float k_PopTime = 0.7f;
    // 左右に揺れる角度（度）と、1往復の時間（秒）
    [SerializeField] private float k_YawAmplitude = 10.0f;
    [SerializeField] private float k_YawPeriod = 4.0f;
    // 上下に浮く幅と、1往復の時間（秒）
    [SerializeField] private float k_BobAmplitude = 0.08f;
    [SerializeField] private float k_BobPeriod = 2.5f;

    private Vector3 basePosition_;
    private Vector3 baseScale_;
    private Quaternion baseRotation_;
    private float elapsed_;

    void Awake()
    {
        basePosition_ = transform.localPosition;
        baseScale_ = transform.localScale;
        baseRotation_ = transform.localRotation;
    }

    void Update()
    {
        elapsed_ += Time.unscaledDeltaTime;

        // 登場：0 から大きくなって、1.15倍まで行き過ぎてから 1 に戻る（バネのような動き）
        float scale = 1.0f;
        if (elapsed_ < k_PopTime)
        {
            float t = elapsed_ / k_PopTime;
            scale = EaseOutBack(t);
        }
        transform.localScale = baseScale_ * Mathf.Max(0.0f, scale);

        float yaw = Mathf.Sin(elapsed_ * Mathf.PI * 2.0f / k_YawPeriod) * k_YawAmplitude;
        float bob = Mathf.Sin(elapsed_ * Mathf.PI * 2.0f / k_BobPeriod) * k_BobAmplitude;
        transform.localRotation = Quaternion.AngleAxis(yaw, Vector3.up) * baseRotation_;
        transform.localPosition = basePosition_ + new Vector3(0.0f, bob, 0.0f);
    }

    // 0→1 の動き。終わりに少し行き過ぎてから戻る
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.9f;
        const float c3 = c1 + 1.0f;
        float u = t - 1.0f;
        return 1.0f + c3 * u * u * u + c1 * u * u;
    }
}
