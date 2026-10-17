using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ステージのリセットとプレイヤーのリスポーンをまとめるクラス（シーンに1つ置く）
public class StageManager : MonoBehaviour
{
    // 倒れてからリスポーンするまでの時間（秒）。倒れるエフェクトを見せる間
    [SerializeField] private float k_RespawnDelay = 0.8f;

    // 参照先（未設定ならシーンから探す）
    [SerializeField] private Player player_;
    [SerializeField] private PlayerMove playerMove_;
    [SerializeField] private CameraMove cameraMove_;

    // ステージ上のすべての床ブロック（壊れて非表示のものも含む）
    private List<FloorBlock> blocks_ = new List<FloorBlock>();
    // シーンに置いた切り替えアイテム（取られて非表示のものも含む）
    private List<ModeSwitchItem> switchItems_ = new List<ModeSwitchItem>();
    // リセット中か（二重に始めないため）
    private bool resetting_ = false;
    // プレイヤーが死んだ回数（ステージのリセットでは戻さない）
    private int deathCount_ = 0;
    // プレイヤーが死んだ回数（外から読む用）
    public static int DeathCount
    {
        get
        {
            StageManager self = Instance;
            return self != null ? self.deathCount_ : 0;
        }
    }

    private static StageManager instance_;

    // シーンの StageManager。プレイ中にスクリプトが再読み込みされて static が消えた場合は探し直す
    private static StageManager Instance
    {
        get
        {
            if (instance_ == null)
            {
                instance_ = FindAnyObjectByType<StageManager>();
            }
            return instance_;
        }
    }

    void Awake()
    {
        instance_ = this;
        // 前のシーンのクリア演出の目印を戻す
        StageClearItem.Cleared = false;
    }

    void OnDestroy()
    {
        if (instance_ == this)
        {
            instance_ = null;
        }
    }

    void Start()
    {
        if (player_ == null)
        {
            player_ = FindAnyObjectByType<Player>();
        }
        if (playerMove_ == null)
        {
            playerMove_ = FindAnyObjectByType<PlayerMove>();
        }
        if (cameraMove_ == null)
        {
            cameraMove_ = FindAnyObjectByType<CameraMove>();
        }

        // 開始時点の床ブロックをすべて覚えておく
        blocks_.AddRange(FindObjectsByType<FloorBlock>(FindObjectsInactive.Include));

        // シーンに手で置いた切り替えアイテムをすべて覚えておく（リセットで元に戻すため）
        switchItems_.AddRange(FindObjectsByType<ModeSwitchItem>(FindObjectsInactive.Include));
        foreach (var item in switchItems_)
        {
            item.Init(playerMove_);
        }
    }

    // プレイヤーの体力が 0 になったときに Player から呼ぶ
    public static void OnPlayerDied()
    {
        StageManager self = Instance;
        // クリアしたあとの演出中に倒されても、死亡として数えず、リセットもしない
        if (self == null || self.resetting_ || StageClearItem.Cleared)
        {
            return;
        }
        // 1回の死につき1つだけ数える（リセット中に重ねて呼ばれた分は上で弾いている）
        self.deathCount_++;
        // 死んだ場所に墓を立てる（プレイヤーを隠すより前に、位置を取っておく）
        if (self.player_ != null)
        {
            Gravestone.Spawn(self.player_.transform.position);
        }
        // ステージごとの累計死亡数をセーブする（モード選択で表示する）
        SaveData.AddDeath(SceneManager.GetActiveScene().name);
        SoundEffects.Play(SoundEffects.Se.Bomb);
        self.StartCoroutine(self.ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        resetting_ = true;

        // 倒れるエフェクトを見せる間、プレイヤーを隠して操作できなくする
        if (player_ != null)
        {
            player_.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(k_RespawnDelay);

        ResetStage();

        if (player_ != null)
        {
            player_.Respawn();
            if (playerMove_ != null)
            {
                playerMove_.ResetMode();
            }            player_.gameObject.SetActive(true);
            if (cameraMove_ != null)
            {
                cameraMove_.ResetView();
            }
            EffectManager.PlayRespawn(player_.transform.position);
            SoundEffects.Play(SoundEffects.Se.Revive);
        }

        resetting_ = false;
    }

    // 壊れたブロックと取られたアイテムを元に戻し、飛んでいる弾を消す
    void ResetStage()
    {
        foreach (var block in blocks_)
        {
            if (block != null)
            {
                block.ResetBlock();
            }
        }

        foreach (var item in switchItems_)
        {
            if (item != null)
            {
                item.ResetItem();
            }
        }

        foreach (var bullet in FindObjectsByType<NormalBulletMove>())
        {
            Destroy(bullet.gameObject);
        }
    }
}
