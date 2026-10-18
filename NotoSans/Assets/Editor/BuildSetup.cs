using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// ビルドまわりの設定。
//  ・出力される exe の名前は、プロダクト名（GururinDive.exe）
//  ・ビルドを始めるとき、ビルド版のセーブデータ（Save フォルダ）を消して、まっさらな状態にする
//    （エディターの試し遊びのセーブ Save_Editor は消さない）
[InitializeOnLoad]
public class BuildSetup : IPreprocessBuildWithReport
{
    private const string k_ProductName = "GururinDive";
    // SaveData.cs の、ビルド版のセーブフォルダ名
    private const string k_BuildSaveFolder = "Save";

    static BuildSetup()
    {
        if (PlayerSettings.productName != k_ProductName)
        {
            PlayerSettings.productName = k_ProductName;
        }
    }

    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        string folder = Path.Combine(Application.persistentDataPath, k_BuildSaveFolder);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            Debug.Log("BuildSetup: 前のビルドのセーブデータを消しました: " + folder);
        }
    }
}
