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
        EnsureOptionsButton();
        optionsPanel_ = OptionsPanel.Create(canvasRoot);
        GameFonts.ApplyBody(canvasRoot);

        for (int i = 0; i < k_Buttons.Length; i++)
        {
            // マウスでも選べる・押せるようにしておく
            MenuButtonPointer pointer = k_Buttons[i].gameObject.AddComponent<MenuButtonPointer>();
            pointer.Init(this, i);
        }
        Apply(true);
    }

    // 「オプション」のボタンを、モード選択のボタンの次に作る（既にあれば何もしない）
    void EnsureOptionsButton()
    {
        if (k_Buttons == null || k_Buttons.Length == 0)
        {
            return;
        }
        foreach (MenuAction action in k_Actions)
        {
            if (action == MenuAction.Options)
            {
                return;
            }
        }

        RectTransform source = k_Buttons[0];
        GameObject clone = Instantiate(source.gameObject, source.parent);
        clone.name = "Button_Options";
        TMP_Text label = clone.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = "オプション";
        }

        // モード選択の次に差し込む
        List<RectTransform> buttons = new List<RectTransform>(k_Buttons);
        List<MenuAction> actions = new List<MenuAction>(k_Actions);
        buttons.Insert(1, (RectTransform)clone.transform);
        actions.Insert(1, MenuAction.Options);
        k_Buttons = buttons.ToArray();
        k_Actions = actions.ToArray();

        // 3つのボタンを、上から等間隔に並べる
        float topY = 320.0f;
        float spacing = 115.0f;
        for (int i = 0; i < k_Buttons.Length; i++)
        {
            k_Buttons[i].anchoredPosition = new Vector2(source.anchoredPosition.x, topY - spacing * i);
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

        int move = 0;
        bool submit = false;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.up.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame) { move = -1; }
            if (pad.dpad.down.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame) { move = 1; }
            if (pad.buttonSouth.wasPressedThisFrame) { submit = true; }
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) { move = -1; }
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) { move = 1; }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { submit = true; }
        }

        if (move != 0 && k_Buttons.Length > 0)
        {
            selected_ = (selected_ + move + k_Buttons.Length) % k_Buttons.Length;
            SoundEffects.Play(SoundEffects.Se.Change);
        }
        if (submit)
        {
            Activate(selected_);
        }

        Apply(false);
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
