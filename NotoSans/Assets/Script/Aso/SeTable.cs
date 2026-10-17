using UnityEngine;

// ゲームで鳴らす効果音（SE）の置き場。Assets/Resources/SeTable に1つだけ置く（SoundEffects が読み込む）
[CreateAssetMenu(fileName = "SeTable", menuName = "Aso/SeTable")]
public class SeTable : ScriptableObject
{
    // 死亡時（爆発）
    public AudioClip bomb;
    // ステージ回転時
    public AudioClip roll;
    // 復活時
    public AudioClip revive;
    // ステージクリア時（シーン遷移をまたいで鳴らす）
    public AudioClip clear;

    // 全体の音量
    [Range(0.0f, 1.0f)] public float volume = 1.0f;
}
