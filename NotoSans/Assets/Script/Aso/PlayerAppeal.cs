using UnityEngine;

// プレイヤーのアピール（ポーズ）を再生する（プレイヤーの Animator と同じオブジェクトに付ける）
// ステージが回転したときに PlayerMove から呼ばれ、3回転ポーズを決める
// ポーズが終わると、アニメーターコントローラーの遷移で待機に戻る
public class PlayerAppeal : MonoBehaviour
{
    // ステージが回転したときに決めるポーズ（アニメーターコントローラーのステート名と合わせる）
    [SerializeField] private string k_StageTurnState = "Appeal_Spin3";
    // 切り替えのなめらかさ（秒）
    [SerializeField] private float k_CrossFadeTime = 0.15f;

    private Animator animator_;

    void Start()
    {
        animator_ = GetComponent<Animator>();
    }

    // ステージが回転したとき（PlayerMove から呼ぶ）: 3回転ポーズを決める
    public void PlayStageTurnPose()
    {
        if (animator_ == null)
        {
            animator_ = GetComponent<Animator>();
        }
        if (animator_ != null)
        {
            Play(k_StageTurnState);
        }
    }

    // 指定したポーズを最初から再生する（再生中に呼ばれても最初から）
    void Play(string stateName)
    {
        animator_.CrossFadeInFixedTime(stateName, k_CrossFadeTime, 0, 0.0f);
    }
}
