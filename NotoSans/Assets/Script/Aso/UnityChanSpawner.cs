using UnityEngine;
using UnityEngine.SceneManagement;

// 演出用のUnityちゃん（UnityChanActor）を、シーンごとに自動で出す（シーンに置く必要はない）。
//  ・タイトル：あっちこっちへ歩いて、あたりを見回す
//  ・クリア：喜び続ける
//  ・スタッフロール：ときどき出てきて、喜んだり拍手したりする
// 出す場所と大きさは、ここの数字で変えられる（画面の左下が (0,0)、右上が (1,1)。heightRate は、画面の高さに対する背の高さの割合）
public static class UnityChanSpawner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        // エディターで「ドメインの再読み込みをしない」設定のときは、前の再生の登録が残る。
        // 先に外してから登録して、いつも1つだけにする（登録が重なると、シーンを読み込むたびにUnityちゃんが増える）
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        // 最初のシーンは、起動時にすでに読み込まれている
        Spawn(SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Spawn(scene.name);
    }

    private static void Spawn(string sceneName)
    {
        switch (sceneName)
        {
            case "TitleScene":
                EnsureLight();
                UnityChanActor.Spawn(UnityChanActor.Behavior.Wander, 0.2f, 0.06f, 5.5f, 0.30f);
                break;
            case "GameClearScene":
                EnsureLight();
                UnityChanActor.Spawn(UnityChanActor.Behavior.Cheer, 0.86f, 0.07f, 6.0f, 0.34f);
                break;
            case "StaffRollScene":
                EnsureLight();
                UnityChanActor.Spawn(UnityChanActor.Behavior.Visit, 0.5f, 0.07f, 6.0f, 0.36f);
                break;
        }
    }

    // 光が1つも無いシーンでは、キャラクターが真っ暗にならないよう、光を足す
    private static void EnsureLight()
    {
        if (Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Length > 0)
        {
            return;
        }
        GameObject obj = new GameObject("ActorLight");
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        obj.transform.rotation = Quaternion.Euler(40.0f, 20.0f, 0.0f);
    }
}
