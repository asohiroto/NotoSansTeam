using UnityEngine;

// 移動モード切り替えアイテム。プレイヤーが触れると正面⇔左を切り替えて消える
// シーンに手で配置する（StageManager が開始時に集めて、リセットで元に戻す）
public class ModeSwitchItem : MonoBehaviour
{
    // このアイテムが示す、この後の床のよけ方（見た目の色で伝える。動きには影響しない）
    public enum DodgeHint
    {
        // 左右どちらによけてもいい（緑）
        Either,
        // 右によけないとぶつかる（青）
        Right,
        // 左によけないとぶつかる（赤）
        Left,
    }

    [SerializeField] private DodgeHint k_Hint = DodgeHint.Either;
    public DodgeHint Hint { get { return k_Hint; } }

    private PlayerMove playerMove_;

    void Awake()
    {
        // 見た目のエフェクトを、再生（EffekseerEmitter の Start）より先に確実に読み込んでおく
        // （Effekseer は読み込み前のアセットの再生に何も言わずに失敗することがあるため）
        var emitter = GetComponent<Effekseer.EffekseerEmitter>();
        if (emitter != null && emitter.effectAsset != null)
        {
            emitter.effectAsset.LoadEffect();
        }
    }

    // StageManager が開始時に呼ぶ
    public void Init(PlayerMove playerMove)
    {
        playerMove_ = playerMove;
    }

    void OnTriggerEnter(Collider other)
    {
        // プレイヤー以外（弾など）は無視
        if (other.GetComponent<NormalBulletMove>() == null && other.GetComponent<Player>() == null)
        {
            return;
        }

        // StageManager がシーンにない場合に備えて、自分でも探す
        if (playerMove_ == null)
        {
            playerMove_ = FindAnyObjectByType<PlayerMove>();
        }
        if (playerMove_ != null)
        {
            playerMove_.TurnLeft();
        }

        // ステージのリセットで元に戻せるよう、消さずに非表示にする
        gameObject.SetActive(false);
    }

    // ステージのリセット: 再び表示する
    public void ResetItem()
    {
        gameObject.SetActive(true);
    }
}
