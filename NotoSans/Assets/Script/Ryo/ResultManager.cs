using UnityEngine;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    void Start()
    {
        scoreText.text = $"SCORE {GameManager.resultScore}";
    }
}