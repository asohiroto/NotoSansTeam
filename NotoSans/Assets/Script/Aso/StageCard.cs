using TMPro;
using UnityEngine;
using UnityEngine.UI;

// モード選択の、ステージ1つぶんのボタン。
// 上から順に「ステージ名の枠」「ステージの画像の枠」「死亡数」「クリア数」「クリア率」を表示する。
// まだ解放されていないステージは、暗くして「ロック中」と解放の条件を重ねて表示する
public class StageCard : MonoBehaviour
{
    // ステージ名の枠に入れる名前
    [SerializeField] private string k_StageName = "ステージ";
    // ステージの画像の枠に入れる画像。ここに入れても、画像の枠（Image）に直接入れてもよい（未設定の間は「画像なし」と表示する）
    [SerializeField] private Sprite k_StageImage;
    // 遷移先のシーン名（Build Settings に入れておくこと）。セーブデータのステージ名にも使う
    [SerializeField] private string k_SceneName;

    [SerializeField] private TMP_Text nameText_;
    [SerializeField] private Image image_;
    [SerializeField] private TMP_Text noImageText_;
    [SerializeField] private TMP_Text deathText_;
    [SerializeField] private TMP_Text clearText_;
    [SerializeField] private TMP_Text rateText_;

    // ロック中の表示（最初に必要になったときに作る）
    private GameObject lockOverlay_;
    private TMP_Text lockConditionText_;

    public string SceneName { get { return k_SceneName; } }
    public string StageName { get { return k_StageName; } }
    // ロック中か（選んでも遷移できない）
    public bool IsLocked { get; private set; }

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

    // ロックの表示を切り替える。condition は、ロック中に見せる解放の条件
    public void SetLocked(bool locked, string condition)
    {
        IsLocked = locked;
        if (locked)
        {
            EnsureLockOverlay();
            lockConditionText_.text = condition;
        }
        if (lockOverlay_ != null)
        {
            lockOverlay_.SetActive(locked);
        }
    }

    private void ApplyFrames()
    {
        SetText(nameText_, k_StageName);
        if (image_ != null)
        {
            // こちらの画像が入っているときだけ枠に入れる。空のときは、枠に直接入れた画像をそのまま使う
            // （空で上書きすると、枠に直接入れた画像が保存のたびに消えてしまう）
            if (k_StageImage != null)
            {
                image_.sprite = k_StageImage;
            }
            bool hasImage = image_.sprite != null;
            image_.color = hasImage ? Color.white : new Color(0.25f, 0.28f, 0.35f, 1.0f);
            if (noImageText_ != null)
            {
                noImageText_.text = "画像なし";
                noImageText_.gameObject.SetActive(!hasImage);
            }
        }
    }

    private void EnsureLockOverlay()
    {
        if (lockOverlay_ != null)
        {
            return;
        }

        GameObject overlay = new GameObject("LockOverlay");
        overlay.transform.SetParent(transform, false);
        Image back = overlay.AddComponent<Image>();
        back.color = new Color(0.04f, 0.06f, 0.14f, 0.82f);
        // 後ろのボタンの押す・選ぶ操作をじゃましない
        back.raycastTarget = false;
        RectTransform rect = back.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text title = CreateText(overlay.transform, "ロック中", 72, new Vector2(0.0f, 40.0f), new Vector2(460.0f, 100.0f));
        GameFonts.SetHeading(title);
        lockConditionText_ = CreateText(overlay.transform, "", 34, new Vector2(0.0f, -70.0f), new Vector2(440.0f, 130.0f));
        lockConditionText_.textWrappingMode = TextWrappingModes.Normal;
        lockOverlay_ = overlay;
    }

    private static TMP_Text CreateText(Transform parent, string value, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        if (GameFonts.Body != null)
        {
            text.font = GameFonts.Body;
        }
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = size;
        return text;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
