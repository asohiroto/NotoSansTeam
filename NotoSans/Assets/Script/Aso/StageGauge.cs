using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 右上の「ステージの進み具合」ゲージ（見た目は実行時に作る。GameHud が作る）。
//  ・縦のゲージの上がスタート、下がゴール。今いる位置に、白い目印が出る
//  ・これまでの最高到達点（死んでも残る。セーブされる）には、赤いピンが立つ
//  ・ゴールまでの残りのメートル数を、数字でカウントダウンする
// スタートの高さは Player、ゴールの高さは シーンの StageClearItem（いちばん低いもの）から取る。1 ユニット = 1 メートル
public class StageGauge : MonoBehaviour
{
    private const float k_BarWidth = 36.0f;
    private const float k_BarHeight = 480.0f;
    // ゲージ全体の大きさ（1 が元の大きさ）
    private const float k_Scale = 0.7f;
    // セーブに書き込む最小の更新幅（メートル）。細かい更新で、何度も書き込まないため
    private const float k_CommitStep = 0.5f;

    private static readonly Color k_FillColor = new Color(0.3f, 0.85f, 1.0f, 1.0f);
    private static readonly Color k_PinColor = new Color(1.0f, 0.3f, 0.3f, 1.0f);

    private Player player_;
    private float goalY_;
    private bool hasGoal_;
    private float startY_;
    private string stage_;

    // これまでの最高到達点（スタートからの落ちた距離）
    private float best_;
    private float committedBest_;
    private float lastDepth_;

    private RectTransform fill_;
    private RectTransform marker_;
    private RectTransform pin_;
    private TMP_Text remainText_;
    private CanvasGroup group_;

    public static StageGauge Create(Transform canvasRoot)
    {
        GameObject obj = new GameObject("StageGauge", typeof(RectTransform));
        obj.transform.SetParent(canvasRoot, false);
        StageGauge gauge = obj.AddComponent<StageGauge>();
        gauge.Build();
        return gauge;
    }

    void Start()
    {
        stage_ = SceneManager.GetActiveScene().name;
        best_ = SaveData.GetBestDepth(stage_);
        committedBest_ = best_;
    }

    void Update()
    {
        // プレイヤーかゴールが無いシーンでは、見えなくしておく（自分自身は止めない）
        bool ready = Prepare();
        group_.alpha = ready ? 1.0f : 0.0f;
        if (!ready)
        {
            return;
        }

        float total = startY_ - goalY_;
        float depth = Mathf.Clamp(startY_ - player_.transform.position.y, 0.0f, total);

        // 死んで戻った（上に戻った）ときと、最高を更新したときの、セーブ
        if (depth < lastDepth_ - 2.0f)
        {
            Commit();
        }
        lastDepth_ = depth;
        if (depth > best_)
        {
            best_ = depth;
        }

        // 進み具合の実績
        Achievements.OnStageProgress(stage_, depth / total);

        float remain = total - depth;
        remainText_.text = Mathf.CeilToInt(remain).ToString();

        SetFill(depth / total);
        SetMarker(marker_, depth / total);
        // 最高到達点（まだ1度も進んでいなければ、ピンは出さない）
        pin_.gameObject.SetActive(best_ > 0.5f);
        SetMarker(pin_, Mathf.Clamp01(best_ / total));
    }

    void OnDestroy()
    {
        Commit();
    }

    void OnApplicationQuit()
    {
        Commit();
    }

    // 最高到達点をセーブする（変わったときだけ）
    private void Commit()
    {
        if (stage_ == null || best_ < committedBest_ + k_CommitStep)
        {
            return;
        }
        SaveData.SetBestDepth(stage_, best_);
        committedBest_ = best_;
    }

    // プレイヤーとゴールを探す（見つかっていなければ、毎回探し直す）
    private bool Prepare()
    {
        if (player_ == null)
        {
            player_ = FindAnyObjectByType<Player>();
            if (player_ == null)
            {
                return false;
            }
            startY_ = player_.StartY;
        }
        if (!hasGoal_)
        {
            bool found = false;
            float lowest = 0.0f;
            foreach (StageClearItem gate in FindObjectsByType<StageClearItem>(FindObjectsInactive.Exclude))
            {
                float y = gate.transform.position.y;
                if (!found || y < lowest)
                {
                    lowest = y;
                    found = true;
                }
            }
            if (!found)
            {
                return false;
            }
            goalY_ = lowest;
            hasGoal_ = true;
        }
        return startY_ - goalY_ > 1.0f;
    }

    // ---- 見た目 ----

    private void SetFill(float rate)
    {
        fill_.anchorMax = new Vector2(1.0f, 1.0f);
        fill_.anchorMin = new Vector2(0.0f, 1.0f - Mathf.Clamp01(rate));
    }

    // 目印を、ゲージの上から rate の高さに置く
    private static void SetMarker(RectTransform marker, float rate)
    {
        float y = 1.0f - Mathf.Clamp01(rate);
        marker.anchorMin = new Vector2(0.5f, y);
        marker.anchorMax = new Vector2(0.5f, y);
        marker.anchoredPosition = Vector2.zero;
    }

    private void Build()
    {
        group_ = gameObject.AddComponent<CanvasGroup>();
        group_.alpha = 0.0f;
        group_.blocksRaycasts = false;
        RectTransform rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(1.0f, 1.0f);
        rect.anchorMax = new Vector2(1.0f, 1.0f);
        rect.pivot = new Vector2(1.0f, 1.0f);
        rect.anchoredPosition = new Vector2(-30.0f, -30.0f);
        rect.sizeDelta = new Vector2(260.0f, k_BarHeight + 190.0f);
        // 全体を小さくする（右上の角を基準に縮む）
        rect.localScale = Vector3.one * k_Scale;

        // 全体の薄い背景
        Image panel = CreateImage("Panel", transform, new Color(0.0f, 0.0f, 0.0f, 0.45f));
        Stretch(panel.rectTransform);

        // 残りのメートル数
        TMP_Text label = CreateText("RemainLabel", transform, "ゴールまで", 30, new Color(1.0f, 1.0f, 1.0f, 0.8f));
        SetRect(label.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(0.0f, -12.0f), new Vector2(240.0f, 40.0f));
        remainText_ = CreateText("RemainText", transform, "0", 76, Color.white);
        GameFonts.SetHeading(remainText_);
        // 数字は右に寄せて、その右に「m」を、重ならないよう間を空けて置く
        SetRect(remainText_.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(-30.0f, -48.0f), new Vector2(190.0f, 90.0f));
        remainText_.alignment = TextAlignmentOptions.Right;
        TMP_Text unit = CreateText("RemainUnit", transform, "m", 40, Color.white);
        SetRect(unit.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(94.0f, -70.0f), new Vector2(50.0f, 60.0f));
        unit.alignment = TextAlignmentOptions.Left;

        // ゲージ（上がスタート、下がゴール）
        GameObject bar = new GameObject("Bar", typeof(RectTransform));
        bar.transform.SetParent(transform, false);
        RectTransform barRect = (RectTransform)bar.transform;
        barRect.anchorMin = new Vector2(0.5f, 0.0f);
        barRect.anchorMax = new Vector2(0.5f, 0.0f);
        barRect.pivot = new Vector2(0.5f, 0.0f);
        barRect.anchoredPosition = new Vector2(0.0f, 24.0f);
        barRect.sizeDelta = new Vector2(k_BarWidth, k_BarHeight);
        Image barBack = bar.AddComponent<Image>();
        barBack.color = new Color(0.15f, 0.18f, 0.28f, 1.0f);
        barBack.raycastTarget = false;

        Image fill = CreateImage("Fill", bar.transform, k_FillColor);
        fill_ = fill.rectTransform;
        fill_.offsetMin = Vector2.zero;
        fill_.offsetMax = Vector2.zero;

        // スタートとゴールの文字
        TMP_Text start = CreateText("StartLabel", bar.transform, "START", 22, new Color(1.0f, 1.0f, 1.0f, 0.7f));
        start.alignment = TextAlignmentOptions.Left;
        SetRect(start.rectTransform, new Vector2(1.0f, 1.0f), new Vector2(10.0f, 0.0f), new Vector2(100.0f, 30.0f));
        start.rectTransform.pivot = new Vector2(0.0f, 1.0f);
        TMP_Text goal = CreateText("GoalLabel", bar.transform, "GOAL", 22, new Color(1.0f, 0.85f, 0.3f, 1.0f));
        goal.alignment = TextAlignmentOptions.Left;
        SetRect(goal.rectTransform, new Vector2(1.0f, 0.0f), new Vector2(10.0f, 0.0f), new Vector2(100.0f, 30.0f));
        goal.rectTransform.pivot = new Vector2(0.0f, 0.0f);

        // 今いる位置の目印（ゲージを横切る白い線）
        Image marker = CreateImage("Marker", bar.transform, Color.white);
        marker_ = marker.rectTransform;
        marker_.sizeDelta = new Vector2(k_BarWidth + 24.0f, 6.0f);

        // 最高到達点のピン（赤いひし形と、BEST の文字）
        GameObject pinRoot = new GameObject("BestPin", typeof(RectTransform));
        pinRoot.transform.SetParent(bar.transform, false);
        pin_ = (RectTransform)pinRoot.transform;
        pin_.sizeDelta = Vector2.zero;
        Image diamond = CreateImage("Diamond", pinRoot.transform, k_PinColor);
        diamond.rectTransform.sizeDelta = new Vector2(28.0f, 28.0f);
        diamond.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
        Outline outline = diamond.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2.0f, 2.0f);
        TMP_Text best = CreateText("BestLabel", pinRoot.transform, "BEST", 24, k_PinColor);
        best.alignment = TextAlignmentOptions.Right;
        SetRect(best.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-(k_BarWidth * 0.5f + 52.0f), 0.0f), new Vector2(90.0f, 30.0f));
        pinRoot.SetActive(false);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, int size, Color color)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        if (GameFonts.Body != null)
        {
            text.font = GameFonts.Body;
        }
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 上(または下)の中央を基準に置く
    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
