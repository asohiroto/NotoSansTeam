using System;

// ステージの並び順と、解放のルール。
//  ・クリアすると、この並びの次のステージへ進む（最後のステージ＝エクストラは、クリアするとステージ選択へ戻る）
//  ・前のステージをすべて一度クリアしていないと、そのステージは選べない（エクストラは、その前の全ステージのクリアで解放）
//  ・「一度クリアした」記録はスコアを削除しても残るので、解放したステージは閉じない
// ステージのシーン名を変えたときは、モード選択のボタンの「遷移先シーン名」と、ここを同じにすること
public static class StageProgress
{
    // 並び順（先頭＝ステージ1、2番目＝ステージ2、最後＝エクストラ）
    public static readonly string[] Stages = { "kiraScene", "HirakiNewScene", "ibukiScene1" };

    // クリアしたあと、次がないときに戻る先
    public const string StageSelectScene = "ModeSelectScene";

    // クリアしたあとに行くシーン。次のステージがあればそれ、無ければステージ選択
    public static string GetNextScene(string current)
    {
        int index = Array.IndexOf(Stages, current);
        if (index < 0 || index >= Stages.Length - 1)
        {
            return StageSelectScene;
        }
        return Stages[index + 1];
    }

    // 選べるか。前のステージをすべて一度クリアしていれば true（並びに無いシーンは、いつでも選べる）
    public static bool IsUnlocked(string stage)
    {
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
}
