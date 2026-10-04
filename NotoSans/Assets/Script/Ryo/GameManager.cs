using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("時間")]
    [SerializeField] private float timeLimit = 240.0f;
    [SerializeField] private TMP_Text timeText;

    private float remainingTime;

    // Result画面に渡すスコア
    public static int resultScore;

    void Start()
    {
        remainingTime = timeLimit;
        UpdateTimeText();
    }

    void Update()
    {
        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0.0f)
        {
            remainingTime = 0.0f;

            UpdateTimeText();
            GameOver();

            return;
        }

        UpdateTimeText();

        // テスト用
        // Fキーを押したらステージクリア(デバッグ用)
        if (Input.GetKeyDown(KeyCode.F))
        {
            StageClear();
        }
    }

    private void UpdateTimeText()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60.0f);
        int seconds = Mathf.FloorToInt(remainingTime % 60.0f);

        timeText.text = $"TIME {minutes:00}:{seconds:00}";
    }

    public void StageClear()
    {
        // 残り時間を秒にする
        int remainingSeconds = Mathf.FloorToInt(remainingTime);

        // 残り秒数 × 50
        resultScore = remainingSeconds * 50;

        // Resultシーンへ
        SceneManager.LoadScene("RyoStage3Scene");
    }

    private void GameOver()
    {
        SceneManager.LoadScene("GameOver");
    }
}