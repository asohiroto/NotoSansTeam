using UnityEngine;
using Effekseer;

// エフェクトの再生をまとめるクラス（シーンに1つ置き、各エフェクトのアセットを設定する）
public class EffectManager : MonoBehaviour
{
    // ブロックが壊れたときのエフェクト
    [SerializeField] private EffekseerEffectAsset blockBreak_;
    // 弾がブロックに当たった（まだ壊れない）ときのエフェクト
    [SerializeField] private EffekseerEffectAsset hit_;
    // 弾を発射したときのエフェクト（足元から下向き）
    [SerializeField] private EffekseerEffectAsset muzzle_;
    // 着地したときのエフェクト（足元）
    [SerializeField] private EffekseerEffectAsset land_;
    // 被ダメージのエフェクト（体の中心）
    [SerializeField] private EffekseerEffectAsset damage_;
    // 倒れたときのエフェクト（体の中心）
    [SerializeField] private EffekseerEffectAsset death_;
    // 移動モードを切り替えたときのエフェクト（足元。プレイヤーについていく）
    [SerializeField] private EffekseerEffectAsset modeSwitch_;
    // リスポーンしたときのエフェクト（足元）
    [SerializeField] private EffekseerEffectAsset respawn_;
    // 弾のエフェクト（再生は弾のプレハブの EffekseerEmitter が行う。ここでは読み込みだけ）
    [SerializeField] private EffekseerEffectAsset bullet_;
    // 切り替えアイテムの見た目（再生はアイテムのプレハブの EffekseerEmitter が行う。ここでは読み込みだけ）
    [SerializeField] private EffekseerEffectAsset switchItem_;

    // モード切替のエフェクトを追いかけさせるための情報
    private EffekseerHandle switchHandle_;
    private Transform switchTarget_;

    private static EffectManager instance_;

    // シーンの EffectManager。プレイ中にスクリプトが再読み込みされて static が消えた場合は探し直す
    private static EffectManager Instance
    {
        get
        {
            if (instance_ == null)
            {
                instance_ = FindAnyObjectByType<EffectManager>();
            }
            return instance_;
        }
    }

    void Awake()
    {
        instance_ = this;
    }

    void Start()
    {
        // Effekseer は、システムの準備前に有効になったアセットを読み込まないことがあり、
        // その場合 PlayEffect が何も言わずに失敗する。使うエフェクトをここで確実に読み込んでおく
        EffekseerEffectAsset[] effects = { blockBreak_, hit_, muzzle_, land_, damage_, death_, modeSwitch_, respawn_, bullet_, switchItem_ };
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.LoadEffect();
            }
        }
    }

    void OnDestroy()
    {
        if (instance_ == this)
        {
            instance_ = null;
        }
    }

    // ブロック破壊のエフェクトを再生する
    public static void PlayBlockBreak(Vector3 position)
    {
        if (Instance == null)
        {
            return;
        }
        Play(Instance.blockBreak_, position);
    }

    // 弾が当たったときのエフェクトを再生する
    public static void PlayHit(Vector3 position)
    {
        if (Instance == null)
        {
            return;
        }
        Play(Instance.hit_, position);
    }

    // 発射のエフェクトを再生する（position は足元＝弾が出る位置）
    public static void PlayMuzzle(Vector3 position)
    {
        if (Instance == null)
        {
            return;
        }
        Play(Instance.muzzle_, position);
    }

    // 着地のエフェクトを再生する（position は足元）
    public static void PlayLand(Vector3 position)
    {
        if (Instance == null)
        {
            return;
        }
        Play(Instance.land_, position);
    }

    // 被ダメージのエフェクトを再生する（position は体の中心）
    public static void PlayDamage(Vector3 position)
    {
        if (Instance == null)
        {
            return;
        }
        Play(Instance.damage_, position);
    }

    // 倒れたときのエフェクトを再生する（position は体の中心）
    public static void PlayDeath(Vector3 position)
    {
        if (Instance == null)
        {
            return;
        }
        Play(Instance.death_, position);
    }

    // リスポーンのエフェクトを再生する（position は足元）
    public static void PlayRespawn(Vector3 position)
    {
        if (Instance == null)
        {
            return;
        }
        Play(Instance.respawn_, position);
    }

    // モード切替のエフェクトを再生し、終わるまで target についていかせる
    public static void PlayModeSwitch(Transform target)
    {
        if (Instance == null || Instance.modeSwitch_ == null || target == null)
        {
            return;
        }
        Instance.switchHandle_ = EffekseerSystem.PlayEffect(Instance.modeSwitch_, target.position);
        Instance.switchTarget_ = target;
    }

    void LateUpdate()
    {
        // 再生中のモード切替エフェクトを、プレイヤーの位置に合わせる（プレイヤーが消えていたら何もしない）
        if (switchTarget_ == null)
        {
            return;
        }
        if (switchHandle_.exists)
        {
            switchHandle_.SetLocation(switchTarget_.position);
        }
        else
        {
            // 再生が終わったら追いかけるのをやめる
            switchTarget_ = null;
        }
    }

    static EffekseerHandle Play(EffekseerEffectAsset effect, Vector3 position)
    {
        // アセットが未設定なら何もしない
        if (effect == null)
        {
            return new EffekseerHandle(-1);
        }
        return EffekseerSystem.PlayEffect(effect, position);
    }
}
