using UnityEngine;

// 移動モード切り替えアイテム。プレイヤーが触れると正面⇔左を切り替えて消える
// シーンに手で配置する（StageManager が開始時に集めて、リセットで元に戻す）
public class ModeSwitchItem : MonoBehaviour
{
    private PlayerMove playerMove_;

    // StageManager が開始時に呼ぶ
    public void Init(PlayerMove playerMove)
    {
        playerMove_ = playerMove;
    }

    void OnTriggerEnter(Collider other)
    {
        // プレイヤー以外（弾など）は無視
        if (other.GetComponent<Player>() == null)
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
            playerMove_.ToggleMode();
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
