using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    // 移動先のシーン名（インスペクターで変更可能）
    [SerializeField] private string nextSceneName = "Stage2";

    // プレイヤーがこのエリアに入った瞬間に呼ばれる
    private void OnTriggerEnter(Collider other)
    {
        // ぶつかってきたオブジェクトのTagが「Player」かチェック
        if (other.CompareTag("Player"))
        {
            Debug.Log(nextSceneName + " へ移動します！");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}