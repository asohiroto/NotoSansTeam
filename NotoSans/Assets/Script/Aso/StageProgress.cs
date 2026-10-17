using System;

// ステージの並び順と、解放のルール。
//  ・ステージ1 → ステージ2 の順に進む。ステージ2をクリアすると、クリアシーンへ行く
//  ・ステージ2は、ステージ1を一度クリアしていないと選べない
//  ・エクストラステージのボタンは、一度でもクリアシーンに行くまで隠れている。
//    出たあとも、ステージ1・2の両方のクリア率が 70% 以上になるまでは選べない
//  ・「一度クリアした」「エクストラを解放した」記録は、スコアを削除しても残る
// ステージのシーン名を変えたときは、モード選択のボタンの「遷移先シーン名」と、ここを同じにすること
public static class StageProgress
{
    // 通常のステージの並び順（先頭＝ステージ1、次＝ステージ2）
    public static readonly string[] Stages = { "kiraScene", "HirakiNewScene" };

    // エクストラステージ
    public const string ExtraStage = "ibukiScene1";

    // 最後の通常ステージ（ステージ2）をクリアしたときに行くシーン
    public const string ClearScene = "GameClearScene";

    // 次がないときに戻る先
    public const string StageSelectScene = "ModeSelectScene";

    // エクストラの解放に必要な、通常ステージのクリア率（%）
    public const int ExtraClearRatePercent = 70;

    // クリアしたあとに行くシーン。次のステージがあればそれ、最後ならクリアシーン、並びに無ければステージ選択
    public static string GetNextScene(string current)
    {
        int index = Array.IndexOf(Stages, current);
        if (index < 0)
        {
            return StageSelectScene;
        }
        return index >= Stages.Length - 1 ? ClearScene : Stages[index + 1];
    }

    // ステージ選択にボタンを出すか。エクストラだけ、一度でもクリアシーンに行くまで隠す
    public static bool IsVisible(string stage)
    {
        return stage != ExtraStage || SaveData.ClearSceneSeen;
    }

    // 選べるか（並びに無いシーンは、いつでも選べる）
    public static bool IsUnlocked(string stage)
    {
        if (stage == ExtraStage)
        {
            return IsExtraUnlocked();
        }

        // 通常のステージは、前のステージをすべて一度クリアしていれば選べる
        int index = Array.IndexOf(Stages, stage);
        for (int i = 0; i < index; i++)
        {
            if (!SaveData.IsCleared(Stages[i]))
            {
                return false;
            }
        }
        return true;
    }

    // エクストラを選べるか。通常のステージすべてのクリア率が 70% 以上になったら解放し、そのまま記録する
    public static bool IsExtraUnlocked()
    {
        if (SaveData.ExtraUnlocked)
        {
            return true;
        }
        foreach (string stage in Stages)
        {
            if (!SaveData.ClearRateAtLeast(stage, ExtraClearRatePercent))
            {
                return false;
            }
        }
        SaveData.UnlockExtra();
        return true;
    }
}
