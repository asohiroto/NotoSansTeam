using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiSceneTester : MonoBehaviour
{
    // インスペクターで1つのセットとして表示するためのクラス
    [Serializable]
    public class SceneBind
    {
        public KeyCode key;        // 押すキー（Alpha1, Alpha2, Spaceなど）
        public string sceneName;   // 切り替え先のシーン名
    }

    // ★「List」にすることで、インスペクターに「＋」が出て何個でも増やせる
    [SerializeField] private List<SceneBind> sceneList = new List<SceneBind>();

    void Update()
    {
        // 登録されているキーが押されたか順番にチェック
        foreach (var bind in sceneList)
        {
            if (Input.GetKeyDown(bind.key))
            {
                Debug.Log($"{bind.key} が押されました。{bind.sceneName} へ切り替えます！");
                SceneManager.LoadScene(bind.sceneName);
                break;
            }
        }
    }
}