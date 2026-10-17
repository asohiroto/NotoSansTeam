using UnityEngine;

// タイトルのロゴを、切り替えアイテムの矢印と同じリズムでY軸中心に左回りさせる
// （90フレーム=1.5秒周期のうち、最初の66フレームは止まり、残り24フレームで2回転する）
public class LogoSpin : MonoBehaviour
{
    // 1周期の長さ（フレーム。Effekseer は 1秒=60フレーム）
    [SerializeField] private float k_PeriodFrames = 90.0f;
    // 止まっている長さ（フレーム）
    [SerializeField] private float k_HoldFrames = 66.0f;
    // 1周期で回る角度（度。マイナスが上から見て左回り）
    [SerializeField] private float k_SpinDegrees = -720.0f;

    private const float k_FramesPerSecond = 60.0f;

    private Quaternion baseRotation_;
    private float elapsed_;

    void Awake()
    {
        baseRotation_ = transform.localRotation;
    }

    void Update()
    {
        elapsed_ += Time.deltaTime;

        float frame = (elapsed_ * k_FramesPerSecond) % k_PeriodFrames;
        float spinFrames = k_PeriodFrames - k_HoldFrames;
        float rate = frame <= k_HoldFrames ? 0.0f : (frame - k_HoldFrames) / spinFrames;

        // 最初の向きを基準に、世界のY軸まわりに回す
        transform.localRotation = Quaternion.AngleAxis(k_SpinDegrees * rate, Vector3.up) * baseRotation_;
    }
}
