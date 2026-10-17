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

    // ボタンの操作：カーソルの移動
    public AudioClip change;
    // ボタンの操作：決定
    public AudioClip select;
    // ボタンの操作：戻る・キャンセル
    public AudioClip cancel;

    // 頭の無音を飛ばして鳴らし始める位置（秒）。音が鳴り出す位置のぎりぎり手前にしてある。0 なら最初から鳴らす
    public float bombStartTime = 0.299f;
    public float rollStartTime = 0.471f;

    // 全体の音量
    [Range(0.0f, 1.0f)] public float volume = 1.0f;
}
