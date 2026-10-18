using System;
using System.IO;
using UnityEngine;

// ゲームの設定（BGM と SE の音量）。オプションで変えると、すぐファイルに保存される。
// 保存先は、セーブデータと同じフォルダの settings.json（エディターとビルドで別のフォルダ）
public static class GameSettings
{
    [Serializable]
    private class Data
    {
        // 0〜1。1 が、シーンに置いてある音量そのまま
        public float bgmVolume = 1.0f;
        public float seVolume = 1.0f;
    }

    private const string k_FileName = "settings.json";

    private static Data data_;

    // 音量が変わったとき（BGM の音量をすぐ反映するために使う）
    public static event Action Changed;

    public static float BgmVolume
    {
        get { Ensure(); return data_.bgmVolume; }
        set
        {
            Ensure();
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(v, data_.bgmVolume)) { return; }
            data_.bgmVolume = v;
            Save();
            if (Changed != null) { Changed(); }
        }
    }

    public static float SeVolume
    {
        get { Ensure(); return data_.seVolume; }
        set
        {
            Ensure();
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(v, data_.seVolume)) { return; }
            data_.seVolume = v;
            Save();
            if (Changed != null) { Changed(); }
        }
    }

    private static string FilePath
    {
        get { return Path.Combine(SaveData.FolderPath, k_FileName); }
    }

    private static void Ensure()
    {
        if (data_ != null)
        {
            return;
        }
        data_ = new Data();
        try
        {
            if (File.Exists(FilePath))
            {
                Data loaded = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath));
                if (loaded != null)
                {
                    data_ = loaded;
                    data_.bgmVolume = Mathf.Clamp01(data_.bgmVolume);
                    data_.seVolume = Mathf.Clamp01(data_.seVolume);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameSettings: 設定を読み込めませんでした: " + e.Message);
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(SaveData.FolderPath);
            File.WriteAllText(FilePath, JsonUtility.ToJson(data_, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameSettings: 設定を書き込めませんでした: " + e.Message);
        }
    }
}
