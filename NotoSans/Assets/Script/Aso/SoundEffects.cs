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
        // ボタン：カーソルの移動
        Change,
        // ボタン：決定
        Select,
        // ボタン：戻る・キャンセル
        Cancel,
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
        // 頭の無音を飛ばして鳴らし始める位置（秒）。0 なら最初から鳴らす
        float startTime = 0.0f;
        switch (se)
        {
            case Se.Bomb: clip = table_.bomb; startTime = table_.bombStartTime; break;
            case Se.Roll: clip = table_.roll; startTime = table_.rollStartTime; break;
            case Se.Revive: clip = table_.revive; break;
            case Se.Clear: clip = table_.clear; break;
            case Se.Change: clip = table_.change; break;
            case Se.Select: clip = table_.select; break;
            case Se.Cancel: clip = table_.cancel; break;
        }
        if (clip == null)
        {
            return;
        }

        if (source_ == null)
        {
            GameObject obj = new GameObject("SoundEffects");
            Object.DontDestroyOnLoad(obj);
            source_ = Configure(obj.AddComponent<AudioSource>());
        }

        if (startTime > 0.0f)
        {
            PlayFromTime(clip, startTime);
            return;
        }

        // 重ねて鳴らせるよう PlayOneShot を使う
        source_.PlayOneShot(clip, table_.volume);
    }

    // 途中から鳴らすための音源（PlayOneShot は途中からは鳴らせない）。同時に鳴らせる数だけ用意する
    private const int k_TimedSourceCount = 4;
    private static AudioSource[] timedSources_;
    private static int timedNext_;

    private static void PlayFromTime(AudioClip clip, float startTime)
    {
        if (timedSources_ == null || timedSources_[0] == null)
        {
            timedSources_ = new AudioSource[k_TimedSourceCount];
            for (int i = 0; i < timedSources_.Length; i++)
            {
                timedSources_[i] = Configure(source_.gameObject.AddComponent<AudioSource>());
            }
        }

        // 鳴っていない音源を使う。全部鳴っていれば、いちばん古いものを使い回す
        AudioSource source = null;
        foreach (AudioSource candidate in timedSources_)
        {
            if (!candidate.isPlaying)
            {
                source = candidate;
                break;
            }
        }
        if (source == null)
        {
            source = timedSources_[timedNext_];
            timedNext_ = (timedNext_ + 1) % timedSources_.Length;
        }

        source.clip = clip;
        source.volume = table_.volume;
        source.time = Mathf.Clamp(startTime, 0.0f, Mathf.Max(0.0f, clip.length - 0.01f));
        source.Play();
    }

    private static AudioSource Configure(AudioSource source)
    {
        source.playOnAwake = false;
        // 2D（画面のどこでも同じ音量）で鳴らす
        source.spatialBlend = 0.0f;
        // ポーズ中（AudioListener.pause）でも、ボタンの音は鳴らす
        source.ignoreListenerPause = true;
        return source;
    }
}
