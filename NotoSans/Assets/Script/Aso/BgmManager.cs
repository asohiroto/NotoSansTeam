using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// BGM をまとめて面倒を見る（シーンに置く必要はない。ゲームの起動時に自動で作られ、シーンをまたいで残る）。
// シーンに置いてある「長い音の AudioSource（playOnAwake）」を BGM とみなして、次のことをする。
//  ・必ずループさせる
//  ・頭の無音を飛ばして、シーンが読み込まれた瞬間から音が出る位置（StartTime）で鳴らし始める
//  ・オプションの BGM 音量を掛ける（シーンに置いてある音量が 1 のときの割合）
public class BgmManager : MonoBehaviour
{
    // これより短い音は、BGM ではなく効果音とみなして触らない（秒）
    private const float k_MinBgmLength = 20.0f;

    private class Track
    {
        public AudioSource source;
        // シーンに置いてあったときの音量
        public float baseVolume;
        // 鳴らし始める位置（秒）。ループで戻ったときも、ここまで飛ばす
        public float startTime;
    }

    private static BgmManager instance_;
    private readonly List<Track> tracks_ = new List<Track>();

    // BGM ごとの、音が鳴り出す位置（秒）。曲の頭にある無音を飛ばすための値（音が鳴り出す位置の少し手前）。
    // 新しい BGM を足したときは、ここに曲名（ファイル名から拡張子を除いたもの）と位置を足す
    private static float StartTimeOf(string clipName)
    {
        switch (clipName)
        {
            case "TitleScene": return 0.65f;   // タイトル
            case "mayoigo": return 1.79f;      // ステージ選択
            case "kirastage": return 1.25f;    // kiraScene
            case "ibykiScene": return 1.85f;   // ibukiScene1
            case "ryouScene": return 1.00f;
            case "hatenko": return 1.60f;      // クリア・スタッフロール
            default: return 0.0f;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance_ != null)
        {
            return;
        }
        GameObject obj = new GameObject("BgmManager");
        DontDestroyOnLoad(obj);
        instance_ = obj.AddComponent<BgmManager>();
        // 最初のシーンは、起動時にすでに読み込まれているので、ここで処理する
        instance_.Scan();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameSettings.Changed += ApplyVolumes;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameSettings.Changed -= ApplyVolumes;
        if (instance_ == this)
        {
            instance_ = null;
        }
    }

    // シーンが読み込まれたらすぐ（最初のフレームより前に）、BGM を整える
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Scan();
    }

    private void Scan()
    {
        tracks_.Clear();
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
        {
            if (source.clip == null || !source.playOnAwake || source.clip.length < k_MinBgmLength)
            {
                continue;
            }
            // 効果音用などの、シーンをまたいで残る音源は触らない
            if (source.gameObject.scene.name == "DontDestroyOnLoad")
            {
                continue;
            }

            Track track = new Track();
            track.source = source;
            track.baseVolume = source.volume;
            track.startTime = StartTimeOf(source.clip.name);
            tracks_.Add(track);

            source.loop = true;
            source.volume = track.baseVolume * GameSettings.BgmVolume;
            // いったん止めて、頭の無音を飛ばした位置から鳴らし直す
            source.Stop();
            source.time = Mathf.Min(track.startTime, Mathf.Max(0.0f, source.clip.length - 0.1f));
            source.Play();
        }
    }

    void Update()
    {
        // ループで曲の頭に戻ったときも、頭の無音を飛ばす
        for (int i = tracks_.Count - 1; i >= 0; i--)
        {
            Track track = tracks_[i];
            if (track.source == null)
            {
                tracks_.RemoveAt(i);
                continue;
            }
            if (track.startTime > 0.0f && track.source.isPlaying && track.source.time < track.startTime)
            {
                track.source.time = track.startTime;
            }
        }
    }

    private void ApplyVolumes()
    {
        float volume = GameSettings.BgmVolume;
        foreach (Track track in tracks_)
        {
            if (track.source != null)
            {
                track.source.volume = track.baseVolume * volume;
            }
        }
    }
}
