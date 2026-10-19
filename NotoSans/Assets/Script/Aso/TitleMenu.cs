using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// タイトルのメニュー（モード選択へ / オプション / ゲームをやめる）。
// 十字キー（キーボードの矢印キーも可）で選び、選んでいるボタンは大きくなる。決定は A ボタン（Enter / スペース）
public class TitleMenu : MonoBehaviour, IMenuSelector
{
    public enum MenuAction
    {
        // モード選択のシーンへ
        ModeSelect,
        // オプション
        Options,
        // ゲームをやめる
        Quit,
        // 操作説明の画像を出す（シーンに保存されている番号がずれないよう、いちばん後ろに足している）
        Controls,
        // 実績の一覧を出す（同じく、いちばん後ろに足している）
        Achievements,
    }

    // 選べるボタン（順番に）と、そのボタンを押したときの動き（同じ順番）
    [SerializeField] private RectTransform[] k_Buttons;
    [SerializeField] private MenuAction[] k_Actions;
    // モード選択のシーン名（Build Settings に入れておくこと）
    [SerializeField] private string k_ModeSelectScene = "ModeSelectScene";

    // 選んでいるときの大きさ（選んでいないときは 1）
    [SerializeField] private float k_SelectedScale = 1.25f;
    // 大きさが変わる速さ
    [SerializeField] private float k_ScaleLerpRate = 14.0f;

    // ボタンの画像（選んでいるものと、選んでいないもの）。どちらも入っているときは画像を入れ替える
    [SerializeField] private Sprite k_NormalSprite;
    [SerializeField] private Sprite k_SelectedSprite;
    // 画像が無いときは、色の明るさで選んでいるかを見せる
    [SerializeField] private Color k_SelectedColor = Color.white;
    [SerializeField] private Color k_NormalColor = new Color(0.65f, 0.65f, 0.65f, 1.0f);
    // ボタンの文字の色（選んでいるものと、選んでいないもの）
    [SerializeField] private Color k_SelectedTextColor = new Color(0.25f, 0.15f, 0.0f, 1.0f);
    [SerializeField] private Color k_NormalTextColor = Color.white;

    private int selected_ = 0;
    // オプション画面
    private OptionsPanel optionsPanel_;
    // 操作説明の画像
    private ControlsGuideView guideView_;
    // 実績の一覧
    private AchievementsPanel achievementsPanel_;

    void Start()
    {
        // 日本語のフォントにする
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : transform;

        // 歩き回るUnityちゃん（カメラから 5.5）が、ボタンの手前を通れるよう、画面の文字やボタンを、カメラから 6 の距離に置く。
        // ロゴ（カメラから 7）よりは手前なので、オプション画面の暗い背景は、ロゴにもかかる
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && Camera.main != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 6.0f;
        }

        // オプションのボタンが無ければ、他のボタンを複製して差し込む（シーンの設定が古くても、必ず出る）
        EnsureExtraButton(MenuAction.Options, "Button_Options", "オプション", 1);
        EnsureExtraButton(MenuAction.Achievements, "Button_Achievements", "実績", 2);
        EnsureExtraButton(MenuAction.Controls, "Button_Controls", "操作説明", 3);
        LayoutButtons();
        optionsPanel_ = OptionsPanel.Create(canvasRoot);
        achievementsPanel_ = AchievementsPanel.Create(canvasRoot);
        guideView_ = ControlsGuideView.Create(canvasRoot);
        GameFonts.ApplyBody(canvasRoot);

        for (int i = 0; i < k_Buttons.Length; i++)
        {
            // マウスでも選べる・押せるようにしておく
            MenuButtonPointer pointer = k_Buttons[i].gameObject.AddComponent<MenuButtonPointer>();
            pointer.Init(this, i);
        }
        Apply(true);
    }

    // 動きが action のボタンが無ければ、モード選択のボタンを複製して、index 番目に差し込む（シーンの設定が古くても、必ず出る）
    void EnsureExtraButton(MenuAction newAction, string buttonName, string text, int index)
    {
        if (k_Buttons == null || k_Buttons.Length == 0)
        {
            return;
        }
        foreach (MenuAction action in k_Actions)
        {
            if (action == newAction)
            {
                return;
            }
        }

        RectTransform source = k_Buttons[0];
        GameObject clone = Instantiate(source.gameObject, source.parent);
        clone.name = buttonName;
        TMP_Text label = clone.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = text;
        }

        List<RectTransform> buttons = new List<RectTransform>(k_Buttons);
        List<MenuAction> actions = new List<MenuAction>(k_Actions);
        index = Mathf.Min(index, buttons.Count);
        buttons.Insert(index, (RectTransform)clone.transform);
        actions.Insert(index, newAction);
        k_Buttons = buttons.ToArray();
        k_Actions = actions.ToArray();

    }

    // ボタンを並べる。最初の4つ（ステージ選択・オプション／実績・操作説明）は、2つずつ横並びの2行にして、
    // 残り（ゲームをやめる）は、その下に1行ずつ中央に置く（画面に収めるため）
    void LayoutButtons()
    {
        if (k_Buttons == null || k_Buttons.Length == 0)
        {
            return;
        }
        float topY = 320.0f;
        float spacing = 115.0f;
        // 並べ替える前の、ボタンの中央の位置（最後のボタンはそのまま中央にある）
        float centerX = k_Buttons[k_Buttons.Length - 1].anchoredPosition.x;
        float width = k_Buttons[0].sizeDelta.x;
        // 他のボタンと同じ大きさのまま、左右に並べる（選んで大きくなっても重ならないよう、間を空ける）
        float offset = width * 0.6f;

        const int pairedCount = 4;
        for (int i = 0; i < k_Buttons.Length; i++)
        {
            if (k_Buttons.Length > pairedCount && i < pairedCount)
            {
                float x = centerX + (i % 2 == 0 ? -offset : offset);
                k_Buttons[i].anchoredPosition = new Vector2(x, topY - spacing * (i / 2));
            }
            else
            {
                int row = k_Buttons.Length > pairedCount ? 2 + (i - pairedCount) : i;
                k_Buttons[i].anchoredPosition = new Vector2(centerX, topY - spacing * row);
            }
        }
    }

    void Update()
    {
        // 遷移の演出中は操作を受け付けない
        if (SceneTransition.IsBusy)
        {
            Apply(false);
            return;
        }

        // オプション画面が開いている間（と、閉じたフレーム）は、タイトルのボタンを操作しない
        if (optionsPanel_ != null && (optionsPanel_.IsOpen || Time.frameCount <= optionsPanel_.LastChangeFrame))
        {
            Apply(false);
            return;
        }

        // 実績の一覧が開いている間（と、閉じたフレーム）も同じ
        if (achievementsPanel_ != null && (achievementsPanel_.IsOpen || Time.frameCount <= achievementsPanel_.LastChangeFrame))
        {
            Apply(false);
            return;
        }

        // 操作説明の画像が開いている間（と、閉じたフレーム）も同じ
        if (guideView_ != null && (guideView_.IsOpen || Time.frameCount <= guideView_.LastChangeFrame))
        {
            Apply(false);
            return;
        }

        int moveX = 0;
        int moveY = 0;
        bool submit = false;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.up.wasPressedThisFrame) { moveY = 1; }
            if (pad.dpad.down.wasPressedThisFrame) { moveY = -1; }
            if (pad.dpad.left.wasPressedThisFrame) { moveX = -1; }
            if (pad.dpad.right.wasPressedThisFrame) { moveX = 1; }
            if (pad.buttonSouth.wasPressedThisFrame) { submit = true; }
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame) { moveY = 1; }
            if (keyboard.downArrowKey.wasPressedThisFrame) { moveY = -1; }
            if (keyboard.leftArrowKey.wasPressedThisFrame) { moveX = -1; }
            if (keyboard.rightArrowKey.wasPressedThisFrame) { moveX = 1; }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { submit = true; }
        }

        if ((moveX != 0 || moveY != 0) && k_Buttons.Length > 0)
        {
            int next = FindNext(selected_, moveX, moveY);
            if (next != selected_)
            {
                selected_ = next;
                SoundEffects.Play(SoundEffects.Se.Change);
            }
        }
        if (submit)
        {
            Activate(selected_);
        }

        Apply(false);
    }

    // ボタンの並び（画面上の位置）に合わせて、十字キーを押した方向にあるボタンを選ぶ。
    // 左右は同じ行の中で、上下は隣の行へ（行の中では、いまの位置にいちばん近いボタン）。端では、反対側へまわる
    private int FindNext(int from, int dx, int dy)
    {
        Vector2 origin = k_Buttons[from].anchoredPosition;
        const float rowTolerance = 1.0f;
        int best = from;
        float bestScore = float.MaxValue;

        // 方向にあるボタンの中で、いちばん近いものを探す
        for (int i = 0; i < k_Buttons.Length; i++)
        {
            if (i == from)
            {
                continue;
            }
            Vector2 d = k_Buttons[i].anchoredPosition - origin;
            bool sameRow = Mathf.Abs(d.y) < rowTolerance;
            float score;
            if (dx != 0)
            {
                if (!sameRow || d.x * dx <= 0.0f) { continue; }
                score = Mathf.Abs(d.x);
            }
            else
            {
                if (sameRow || d.y * dy <= 0.0f) { continue; }
                // 行の近さを優先して、同じ行の中では、横の位置が近いものを選ぶ
                score = Mathf.Abs(d.y) * 10.0f + Mathf.Abs(d.x);
            }
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        if (best != from)
        {
            return best;
        }

        // 行き止まり：反対側の端へまわる
        bestScore = float.MaxValue;
        for (int i = 0; i < k_Buttons.Length; i++)
        {
            if (i == from)
            {
                continue;
            }
            Vector2 d = k_Buttons[i].anchoredPosition - origin;
            bool sameRow = Mathf.Abs(d.y) < rowTolerance;
            float score;
            if (dx != 0)
            {
                if (!sameRow) { continue; }
                // 反対側の、いちばん遠いもの
                score = -Mathf.Abs(d.x);
            }
            else
            {
                if (sameRow) { continue; }
                // 反対の端の行（遠い行）で、横の位置が近いもの
                score = -Mathf.Abs(d.y) * 10.0f + Mathf.Abs(d.x);
            }
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }

    // マウスが乗ったとき
    public void Select(int index)
    {
        if (index != selected_)
        {
            selected_ = index;
            SoundEffects.Play(SoundEffects.Se.Change);
        }
    }

    public void Activate(int index)
    {
        if (SceneTransition.IsBusy || index < 0 || index >= k_Actions.Length)
        {
            return;
        }
        switch (k_Actions[index])
        {
            case MenuAction.ModeSelect:
                SoundEffects.Play(SoundEffects.Se.Select);
                SceneTransition.Go(k_ModeSelectScene);
                break;
            case MenuAction.Options:
                SoundEffects.Play(SoundEffects.Se.Select);
                if (optionsPanel_ != null)
                {
                    optionsPanel_.Open();
                }
                break;
            case MenuAction.Achievements:
                SoundEffects.Play(SoundEffects.Se.Select);
                if (achievementsPanel_ != null)
                {
                    achievementsPanel_.Open();
                }
                break;
            case MenuAction.Controls:
                SoundEffects.Play(SoundEffects.Se.Select);
                if (guideView_ != null)
                {
                    guideView_.Open();
                }
                break;
            case MenuAction.Quit:
                SoundEffects.Play(SoundEffects.Se.Select);
                Quit();
                break;
        }
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 選んでいるボタンを大きく・目立たせる
    void Apply(bool immediate)
    {
        bool useSprites = k_NormalSprite != null && k_SelectedSprite != null;
        for (int i = 0; i < k_Buttons.Length; i++)
        {
            bool on = i == selected_;
            float target = on ? k_SelectedScale : 1.0f;
            float scale = immediate ? target : Mathf.Lerp(k_Buttons[i].localScale.x, target, 1.0f - Mathf.Exp(-k_ScaleLerpRate * Time.unscaledDeltaTime));
            k_Buttons[i].localScale = new Vector3(scale, scale, 1.0f);

            Image image = k_Buttons[i].GetComponent<Image>();
            if (image != null)
            {
                if (useSprites)
                {
                    image.sprite = on ? k_SelectedSprite : k_NormalSprite;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                }
                else
                {
                    image.color = on ? k_SelectedColor : k_NormalColor;
                }
            }

            TMP_Text label = k_Buttons[i].GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = on ? k_SelectedTextColor : k_NormalTextColor;
            }
        }
    }
}
