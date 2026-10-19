using System.Collections.Generic;

// 実績。一覧は、下の static コンストラクターで作っている（増やすときは、ここに足して、達成する場所から Unlock を呼ぶ）。
// 達成した実績はセーブデータに残る。達成すると、左上に知らせが出る（AchievementToast）。
// 一覧の画面（AchievementsPanel）は、PageTitles・PageSizes の区切りでページを分ける（All の並びと合わせること）
public static class Achievements
{
    public class Def
    {
        public readonly string id;
        public readonly string name;
        public readonly string description;

        public Def(string id, string name, string description)
        {
            this.id = id;
            this.name = name;
            this.description = description;
        }
    }

    // ステージの情報。実績の ID の頭に付く名前（key）と、シーン名と、画面に出す名前
    private class Stage
    {
        public readonly string key;
        public readonly string scene;
        public readonly string label;
        // クリアの実績の ID（前からある名前のまま）
        public readonly string clearId;

        public Stage(string key, string scene, string label, string clearId)
        {
            this.key = key;
            this.scene = scene;
            this.label = label;
            this.clearId = clearId;
        }
    }

    private static readonly Stage[] k_Stages =
    {
        new Stage("stage1", "kiraScene", "ステージ1", "clear_stage1"),
        new Stage("stage2", "HirakiNewScene", "ステージ2", "clear_stage2"),
        new Stage("extra", "ibukiScene1", "エクストラ", "clear_extra"),
    };

    // 各ステージの実績の条件の数字（クリアの回数は、実績の説明の文にも使う）
    private const int k_StageDeathGoal = 20;
    private const int k_StageClearGoal = 3;
    // 全ステージ合計の死亡数の実績（数字と、名前）
    private static readonly int[] k_DeathGoals = { 1, 10, 50, 100, 300, 500, 1000 };
    private static readonly string[] k_DeathNames =
    {
        "はじめの一歩", "七転び八起き", "まだまだこれから", "不屈の挑戦者", "落下の達人", "地面とお友達", "伝説の落ち人",
    };

    // 一覧の画面のページ（見出しと、そのページの実績の数。All の並びと同じ）
    public static readonly string[] PageTitles = { "全般", "ステージ1・ステージ2", "エクストラ" };
    public static readonly int[] PageSizes = { 12, 12, 6 };

    // 「同じ場所で何度も落ちる」の条件：この距離の中で、この回数
    private const int k_SamePlaceCount = 5;
    private const float k_SamePlaceRadius = 2.5f;
    // いまのステージで落ちた場所（ステージを変えたら、忘れる）
    private static readonly List<UnityEngine.Vector3> k_DeathSpots = new List<UnityEngine.Vector3>();
    private static string k_DeathSpotScene = string.Empty;

    public static readonly Def[] All = BuildAll();

    private static Def[] BuildAll()
    {
        List<Def> list = new List<Def>();

        // ページ1：全般（死亡数・クリア・そのほか）
        for (int i = 0; i < k_DeathGoals.Length; i++)
        {
            string id = i == 0 ? "first_fall" : "deaths_" + k_DeathGoals[i];
            string description = i == 0 ? "はじめて落ちる" : "合計で" + k_DeathGoals[i] + "回落ちる";
            list.Add(new Def(id, k_DeathNames[i], description));
        }
        list.Add(new Def("clear_all", "全ステージ制覇", "すべてのステージをクリアする"));
        list.Add(new Def("same_place", "またここか…", "同じ場所で" + k_SamePlaceCount + "回落ちる"));
        list.Add(new Def("no_input", "ただ落ちるだけ", "一切操作せずに落ちる"));
        list.Add(new Def("hint", "ヒントを見る", "ポーズ画面から、ヒントの動画を見る"));
        list.Add(new Def("staff_roll", "最後まで見届けた", "スタッフロールを最後まで見る"));

        // ページ2・3：ステージごと（6つずつ）
        foreach (Stage stage in k_Stages)
        {
            list.Add(new Def(stage.key + "_half", stage.label + " 中間地点", stage.label + "で、ゴールまでの半分まで進む"));
            list.Add(new Def(stage.key + "_almost", stage.label + " ゴール目前", stage.label + "で、ゴールまでの9割まで進む"));
            list.Add(new Def(stage.clearId, stage.label + " クリア", stage.label + "をクリアする"));
            list.Add(new Def(stage.key + "_nodeath", stage.label + " ノーミス", stage.label + "で、1度も落ちずにクリアする"));
            list.Add(new Def(stage.key + "_deaths", stage.label + " 修行中", stage.label + "で、" + k_StageDeathGoal + "回落ちる"));
            list.Add(new Def(stage.key + "_clear3", stage.label + " 常連", stage.label + "を" + k_StageClearGoal + "回クリアする"));
        }
        return list.ToArray();
    }

    public static Def Find(string id)
    {
        foreach (Def def in All)
        {
            if (def.id == id)
            {
                return def;
            }
        }
        return null;
    }

    // シーン名からステージの key（実績の ID の頭に付く名前）。ステージでなければ null
    public static string StageKey(string scene)
    {
        foreach (Stage stage in k_Stages)
        {
            if (stage.scene == scene)
            {
                return stage.key;
            }
        }
        return null;
    }

    // 達成数
    public static int UnlockedCount
    {
        get
        {
            int count = 0;
            foreach (Def def in All)
            {
                if (SaveData.IsAchievementUnlocked(def.id))
                {
                    count++;
                }
            }
            return count;
        }
    }

    // 実績を達成にする。はじめて達成したときだけ、左上に知らせを出す
    public static void Unlock(string id)
    {
        Def def = Find(id);
        if (def == null || !SaveData.AddAchievement(id))
        {
            return;
        }
        AchievementToast.Show(def);
    }

    // ステージで、ゴールまでの何割まで進んだか（0〜1）から決まる実績
    public static void OnStageProgress(string scene, float rate)
    {
        string key = StageKey(scene);
        if (key == null)
        {
            return;
        }
        if (rate >= 0.5f) { Unlock(key + "_half"); }
        if (rate >= 0.9f) { Unlock(key + "_almost"); }
    }

    // プレイヤーが落ちたとき。operated は、今回の命で、なにか操作をしたか
    public static void OnPlayerDied(string scene, UnityEngine.Vector3 position, bool operated)
    {
        if (StageKey(scene) == null)
        {
            return;
        }
        if (!operated)
        {
            Unlock("no_input");
        }

        if (scene != k_DeathSpotScene)
        {
            k_DeathSpots.Clear();
            k_DeathSpotScene = scene;
        }
        k_DeathSpots.Add(position);
        int near = 0;
        foreach (UnityEngine.Vector3 spot in k_DeathSpots)
        {
            if (UnityEngine.Vector2.Distance(spot, position) <= k_SamePlaceRadius)
            {
                near++;
            }
        }
        if (near >= k_SamePlaceCount)
        {
            Unlock("same_place");
        }
    }

    // ステージをノーミス（1度も落ちずに）クリアしたとき
    public static void OnNoDeathClear(string scene)
    {
        string key = StageKey(scene);
        if (key != null)
        {
            Unlock(key + "_nodeath");
        }
    }

    // セーブデータの数字から決まる実績を調べる（死亡数・クリアが変わるたびに、SaveData から呼ばれる）
    public static void Evaluate()
    {
        int deaths = SaveData.TotalDeaths;
        for (int i = 0; i < k_DeathGoals.Length; i++)
        {
            if (deaths >= k_DeathGoals[i])
            {
                Unlock(i == 0 ? "first_fall" : "deaths_" + k_DeathGoals[i]);
            }
        }

        bool all = true;
        foreach (Stage stage in k_Stages)
        {
            if (SaveData.IsCleared(stage.scene)) { Unlock(stage.clearId); }
            else { all = false; }
            if (SaveData.GetDeaths(stage.scene) >= k_StageDeathGoal) { Unlock(stage.key + "_deaths"); }
            if (SaveData.GetClears(stage.scene) >= k_StageClearGoal) { Unlock(stage.key + "_clear3"); }
        }
        if (all) { Unlock("clear_all"); }
    }
}
