using UnityEngine;
using UnityEngine.SceneManagement;

// ステージクリア用アイテム。プレイヤーが触れるとクリアSEを鳴らして、選んだシーンへ遷移する
// 遷移先は Inspector の「遷移先シーン」で選ぶ（遷移先は Build Settings のシーン一覧に入れておくこと）
public class StageClearItem : MonoBehaviour
{
#if UNITY_EDITOR
    // 遷移先のシーン（エディター上で選ぶ用。実際の遷移には下の名前を使う）
    [SerializeField] private UnityEditor.SceneAsset k_NextScene;
#endif
    // 遷移先のシーン名（遷移先シーンを選ぶと自動で入る。ビルドではこちらを使う）
    [SerializeField, HideInInspector] private string nextSceneName_;

    // 見た目：ゆっくり回す速さ（度/秒）
    [SerializeField] private float k_SpinSpeed = 90.0f;

    // 二重に遷移しないための印
    private bool cleared_ = false;

#if UNITY_EDITOR
    void OnValidate()
    {
        nextSceneName_ = k_NextScene != null ? k_NextScene.name : string.Empty;
    }
#endif

    void Update()
    {
        transform.Rotate(0.0f, k_SpinSpeed * Time.deltaTime, 0.0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        // プレイヤー以外（弾など）は無視
        if (cleared_ || other.GetComponent<Player>() == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(nextSceneName_))
        {
            Debug.LogWarning("StageClearItem: 遷移先シーンが選ばれていません", this);
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName_))
        {
            Debug.LogWarning("StageClearItem: シーン「" + nextSceneName_ + "」が Build Settings に入っていません", this);
            return;
        }

        cleared_ = true;
        // 音源はシーンをまたいで残るので、遷移しても最後まで鳴る
        SoundEffects.Play(SoundEffects.Se.Clear);
        SceneManager.LoadScene(nextSceneName_);
    }
}
