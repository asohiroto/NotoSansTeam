using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ステージごとの累計死亡数・クリア数のセーブデータ。
// ゲームのデータフォルダ（Application.persistentDataPath）の中の Save フォルダに JSON で保存する。
// 数が変わるたびにすぐ書き込むので、ゲームを終了しても残る
public static class SaveData
{
    [Serializable]
    private class StageRecord
    {
        // ステージ名（シーン名）
        public string stage;
        // 累計死亡数
        public int deaths;
        // 累計クリア数
        public int clears;
    }

    [Serializable]
    private class SaveFile
    {
        public List<StageRecord> stages = new List<StageRecord>();
    }

    // データフォルダの中のセーブ用フォルダ名・ファイル名
    private const string k_FolderName = "Save";
    private const string k_FileName = "stage_records.json";

    private static SaveFile file_;

    // セーブフォルダの場所
    public static string FolderPath
    {
        get { return Path.Combine(Application.persistentDataPath, k_FolderName); }
    }

    private static string FilePath
    {
        get { return Path.Combine(FolderPath, k_FileName); }
    }

    public static int GetDeaths(string stage)
    {
        StageRecord record = Find(stage, false);
        return record != null ? record.deaths : 0;
    }

    public static int GetClears(string stage)
    {
        StageRecord record = Find(stage, false);
        return record != null ? record.clears : 0;
    }

    // クリア率（0〜1）。挑戦の終わり方は「死亡」か「クリア」なので、クリア数 ÷（クリア数＋死亡数）。
    // まだ1回も遊んでいなければ 0
    public static float GetClearRate(string stage)
    {
        int clears = GetClears(stage);
        int total = clears + GetDeaths(stage);
        return total > 0 ? (float)clears / total : 0.0f;
    }

    public static void AddDeath(string stage)
    {
        Find(stage, true).deaths++;
        Save();
    }

    public static void AddClear(string stage)
    {
        Find(stage, true).clears++;
        Save();
    }

    // そのステージの死亡数とクリア数を消す（0に戻る）
    public static void DeleteStage(string stage)
    {
        // まだ読み込んでいなければ、先に読み込む（Find が読み込む）
        Find(stage, false);
        file_.stages.RemoveAll(record => record.stage == stage);
        Save();
    }

    private static StageRecord Find(string stage, bool create)
    {
        if (file_ == null)
        {
            Load();
        }
        foreach (StageRecord record in file_.stages)
        {
            if (record.stage == stage)
            {
                return record;
            }
        }
        if (!create)
        {
            return null;
        }
        StageRecord added = new StageRecord();
        added.stage = stage;
        file_.stages.Add(added);
        return added;
    }

    private static void Load()
    {
        file_ = new SaveFile();
        try
        {
            if (File.Exists(FilePath))
            {
                SaveFile loaded = JsonUtility.FromJson<SaveFile>(File.ReadAllText(FilePath));
                if (loaded != null && loaded.stages != null)
                {
                    file_ = loaded;
                }
            }
        }
        catch (Exception e)
        {
            // 読めないときは空のデータで始める（壊れたファイルは次のセーブで上書きされる）
            Debug.LogWarning("SaveData: セーブデータを読み込めませんでした: " + e.Message);
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            // 書き込み中に止まってもデータが壊れないよう、一度別ファイルに書いてから置き換える
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(file_, true));
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
            File.Move(temp, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("SaveData: セーブデータを書き込めませんでした: " + e.Message);
        }
    }
}
