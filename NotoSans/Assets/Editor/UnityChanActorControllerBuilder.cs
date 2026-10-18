using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 演出用のUnityちゃん（UnityChanActor）が使うアニメーターを作る。
// ステートの名前は、UnityChanActor.cs の State と同じにしてある。作り直したいときは、メニュー「Aso → 演出用Unityちゃんのアニメーターを作る」
public static class UnityChanActorControllerBuilder
{
    private const string k_OutputPath = "Assets/Resources/UnityChanActor.controller";
    private const string k_AnimDir = "Assets/Haons SD series Pack/Animation/";

    // ステート名, FBX のファイル, FBX の中のクリップ名（空なら最初のクリップ）
    private static readonly string[][] k_States =
    {
        new string[] { "Idle", "Common/StandA_idleA.FBX", "" },
        new string[] { "Walk", "Common/WalkA_Front.FBX", "" },
        new string[] { "Clap", "Action Adventure(Adv)/Emotion_Clap(StandC).FBX", "" },
        new string[] { "Spin", "Action Adventure(Adv)/VictoryPose_UTCStyleSpin.fbx", "VictoryPose_UTCStyleSpin_4spin" },
        new string[] { "Hold", "Action Adventure(Adv)/VictoryPose_UTCStyleSpin.fbx", "VictoryPose_UTCStyleSpin_Hold@loop" },
        new string[] { "Jump", "Action Adventure(Adv)/Jump_One-cycle-Type(root).FBX", "Jump_Medium root" },
    };

    [MenuItem("Aso/演出用Unityちゃんのアニメーターを作る")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        AssetDatabase.DeleteAsset(k_OutputPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(k_OutputPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        List<string> report = new List<string>();
        for (int i = 0; i < k_States.Length; i++)
        {
            string stateName = k_States[i][0];
            AnimationClip clip = LoadClip(k_AnimDir + k_States[i][1], k_States[i][2]);
            AnimatorState state = machine.AddState(stateName, new Vector3(260.0f, 60.0f * i, 0.0f));
            state.motion = clip;
            state.writeDefaultValues = true;
            if (i == 0)
            {
                machine.defaultState = state;
            }
            report.Add(stateName + "=" + (clip != null ? clip.name + "(" + clip.length.ToString("0.00") + "s, loop=" + clip.isLooping + ")" : "NOT FOUND"));
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("UnityChanActorControllerBuilder: " + k_OutputPath + " を作りました: " + string.Join(", ", report.ToArray()));
    }

    private static AnimationClip LoadClip(string path, string clipName)
    {
        AnimationClip first = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            AnimationClip clip = asset as AnimationClip;
            // FBX に自動で付く、プレビュー用のクリップは使わない
            if (clip == null || clip.name.StartsWith("__preview__"))
            {
                continue;
            }
            if (clipName.Length == 0)
            {
                return clip;
            }
            if (clip.name == clipName)
            {
                return clip;
            }
            if (first == null)
            {
                first = clip;
            }
        }
        return clipName.Length == 0 ? first : null;
    }
}
