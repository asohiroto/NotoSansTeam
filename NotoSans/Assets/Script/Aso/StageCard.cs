using TMPro;
using UnityEngine;
using UnityEngine.UI;

// モード選択の、ステージ1つぶんのボタン。
// 上から順に「ステージ名の枠」「ステージの画像の枠」「死亡数」「クリア数」「クリア率」を表示する
public class StageCard : MonoBehaviour
{
    // ステージ名の枠に入れる名前
    [SerializeField] private string k_StageName = "STAGE";
    // ステージの画像の枠に入れる画像（未設定の間は「NO IMAGE」と表示する）
    [SerializeField] private Sprite k_StageImage;
    // 遷移先のシーン名（Build Settings に入れておくこと）。セーブデータのステージ名にも使う
    [SerializeField] private string k_SceneName;

    [SerializeField] private TMP_Text nameText_;
    [SerializeField] private Image image_;
    [SerializeField] private TMP_Text noImageText_;
    [SerializeField] private TMP_Text deathText_;
    [SerializeField] private TMP_Text clearText_;
    [SerializeField] private TMP_Text rateText_;

    public string SceneName { get { return k_SceneName; } }
    public string StageName { get { return k_StageName; } }

    void Start()
    {
        Refresh();
    }

#if UNITY_EDITOR
    // Inspector で名前や画像を変えたとき、すぐに見た目へ反映する
    void OnValidate()
    {
        ApplyFrames();
    }
#endif

    // 名前と画像、セーブデータの数字を表示し直す
    public void Refresh()
    {
        ApplyFrames();

        int deaths = SaveData.GetDeaths(k_SceneName);
        int clears = SaveData.GetClears(k_SceneName);
        float rate = SaveData.GetClearRate(k_SceneName);
        SetText(deathText_, "死亡数   " + deaths);
        SetText(clearText_, "クリア数   " + clears);
        SetText(rateText_, "クリア率   " + (rate * 100.0f).ToString("0.0") + "%");
    }

    private void ApplyFrames()
    {
        SetText(nameText_, k_StageName);
        if (image_ != null)
        {
            image_.sprite = k_StageImage;
            image_.color = k_StageImage != null ? Color.white : new Color(0.25f, 0.28f, 0.35f, 1.0f);
        }
        if (noImageText_ != null)
        {
            noImageText_.text = "画像なし";
            noImageText_.gameObject.SetActive(k_StageImage == null);
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
