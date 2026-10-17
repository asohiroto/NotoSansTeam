using UnityEngine;

// 効果音（SE）を鳴らす。シーンに何も置かなくても動く
// 音源は最初に鳴らすときに自動で作られ、シーンを切り替えても残る（クリアSEが遷移で途切れない）
public static class SoundEffects
{
    public enum Se
    {
        // 死亡時（爆発）
        Bomb,
        // ステージ回転時
        Roll,
        // 復活時
        Revive,
        // ステージクリア時
        Clear,
    }

    // SeTable を置く場所（Resources フォルダの中の名前）
    private const string k_TableName = "SeTable";

    private static SeTable table_;
    private static AudioSource source_;

    public static void Play(Se se)
    {
        if (table_ == null)
        {
            table_ = Resources.Load<SeTable>(k_TableName);
            if (table_ == null)
            {
                Debug.LogWarning("Resources/" + k_TableName + " が見つからないので、SE を鳴らせません");
                return;
            }
        }

        AudioClip clip = null;
        switch (se)
        {
            case Se.Bomb: clip = table_.bomb; break;
            case Se.Roll: clip = table_.roll; break;
            case Se.Revive: clip = table_.revive; break;
            case Se.Clear: clip = table_.clear; break;
        }
        if (clip == null)
        {
            return;
        }

        if (source_ == null)
        {
            GameObject obj = new GameObject("SoundEffects");
            Object.DontDestroyOnLoad(obj);
            source_ = obj.AddComponent<AudioSource>();
            source_.playOnAwake = false;
            // 2D（画面のどこでも同じ音量）で鳴らす
            source_.spatialBlend = 0.0f;
        }

        // 重ねて鳴らせるよう PlayOneShot を使う
        source_.PlayOneShot(clip, table_.volume);
    }
}
