using UnityEngine;
using UnityEngine.SceneManagement;

// ステージクリアの判定オブジェクト（光のゲート）。プレイヤーが触れると、クリアSEを鳴らして次へ進む。
// 遷移先は、Inspector の「遷移先シーン」で選べる。空のままなら、StageProgress の並びの次のステージへ進む
// （次が無ければステージ選択へ戻る）。遷移先は Build Settings のシーン一覧に入れておくこと
public class StageClearItem : MonoBehaviour
{
#if UNITY_EDITOR
    // 遷移先のシーン（エディター上で選ぶ用。空なら自動で次のステージ）
    [SerializeField] private UnityEditor.SceneAsset k_NextScene;
#endif
    // 遷移先のシーン名（遷移先シーンを選ぶと自動で入る。ビルドではこちらを使う）
    [SerializeField, HideInInspector] private string nextSceneName_;

    // 見た目：ゆっくり回す速さ（度/秒）。0なら回さない（光のゲートは回さない）
    [SerializeField] private float k_SpinSpeed = 90.0f;

    // 二重に遷移しないための印
    private bool cleared_ = false;

    // クリアしたあとの演出中か（演出中に倒されても、死亡として数えないための目印）
    public static bool Cleared { get; set; }

#if UNITY_EDITOR
    void OnValidate()
    {
        nextSceneName_ = k_NextScene != null ? k_NextScene.name : string.Empty;
    }
#endif

    void Update()
    {
        if (k_SpinSpeed != 0.0f)
        {
            transform.Rotate(0.0f, k_SpinSpeed * Time.deltaTime, 0.0f, Space.World);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // プレイヤー以外（弾など）は無視
        if (cleared_ || other.GetComponent<Player>() == null)
        {
            return;
        }

        string current = SceneManager.GetActiveScene().name;
        string next = string.IsNullOrEmpty(nextSceneName_) ? StageProgress.GetNextScene(current) : nextSceneName_;
        if (!Application.CanStreamedLevelBeLoaded(next))
        {
            Debug.LogWarning("StageClearItem: シーン「" + next + "」が Build Settings に入っていません", this);
            return;
        }

        cleared_ = true;
        Cleared = true;
        // ステージごとの累計クリア数をセーブする（モード選択で表示・次のステージの解放に使う）
        // 1度も落ちずにクリアしたかは、クリアを数える前の死亡数ではなく、このステージでの死亡数で決める
        bool noDeath = StageManager.DeathCount == 0;
        SaveData.AddClear(current);
        if (noDeath)
        {
            Achievements.OnNoDeathClear(current);
        }
        // クリアシーンへ行くなら、「行ったことがある」を記録する（エクストラステージのボタンが出る）
        if (next == StageProgress.ClearScene)
        {
            SaveData.MarkClearSceneSeen();
        }
        // 音源はシーンをまたいで残るので、遷移しても最後まで鳴る
        SoundEffects.Play(SoundEffects.Se.Clear);
        // 上からUnityちゃんが降ってくる演出つきで、次へ進む
        SceneTransition.Go(next);
    }
}
