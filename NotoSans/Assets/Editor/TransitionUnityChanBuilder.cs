using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ステージ遷移で降ってくるUnityちゃん（Resources/TransitionUnityChan）を、ゲーム内のプレイヤー（player.prefab）から作る。
// プレイヤーの衣装や素材を変えたら、自動で作り直されるので、いつもゲーム内の見た目と同じになる。
// 手動で作り直したいときは、メニュー「Aso → 遷移用のUnityちゃんを作り直す」
public static class TransitionUnityChanBuilder
{
    private const string k_PlayerPath = "Assets/GameObject/aso/player.prefab";
    private const string k_OutputPath = "Assets/Resources/TransitionUnityChan.prefab";

    [MenuItem("Aso/遷移用のUnityちゃんを作り直す")]
    public static void Rebuild()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(k_PlayerPath);
        if (source == null)
        {
            Debug.LogWarning("TransitionUnityChanBuilder: " + k_PlayerPath + " が見つかりません");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // 開いているシーンに触れないよう、プレビュー用のシーンで作る
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "TransitionUnityChan";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            RemoveGameplayComponents(instance);
            PrefabUtility.SaveAsPrefabAsset(instance, k_OutputPath);
            Debug.Log("TransitionUnityChanBuilder: " + k_OutputPath + " を作り直しました");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    // 操作や当たり判定のための部品を外す。見た目（メッシュ・アニメーター・髪のゆれ）だけ残す
    private static void RemoveGameplayComponents(GameObject root)
    {
        // 部品どうしが依存しているので、外せるものから順に、外せなくなるまで繰り返す
        for (int pass = 0; pass < 6; pass++)
        {
            bool removed = false;
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component is Transform)
                {
                    continue;
                }
                if (!ShouldRemove(component))
                {
                    continue;
                }
                if (!CanRemove(component))
                {
                    continue;
                }
                Object.DestroyImmediate(component);
                removed = true;
            }
            if (!removed)
            {
                break;
            }
        }
    }

    private static bool ShouldRemove(Component component)
    {
        if (component is Collider || component is Rigidbody)
        {
            return true;
        }
        // スクリプトは、髪のゆれ（SpringManager）だけ残す
        if (component is MonoBehaviour)
        {
            return component.GetType().Name != "SpringManager";
        }
        return false;
    }

    // 他の部品から必要とされている間は、外せない
    private static bool CanRemove(Component component)
    {
        foreach (Component other in component.gameObject.GetComponents<Component>())
        {
            if (other == null || other == component)
            {
                continue;
            }
            foreach (RequireComponent attribute in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
            {
                if (attribute.m_Type0 == component.GetType() || attribute.m_Type1 == component.GetType() || attribute.m_Type2 == component.GetType())
                {
                    return false;
                }
            }
        }
        return true;
    }

    // player.prefab が保存・取り込みされたら、自動で作り直す
    private class Watcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (path == k_PlayerPath)
                {
                    // 取り込みの最中に別の取り込みを始めないよう、少し遅らせる
                    EditorApplication.delayCall += Rebuild;
                    return;
                }
            }
        }
    }
}
