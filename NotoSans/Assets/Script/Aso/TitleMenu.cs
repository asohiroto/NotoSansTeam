using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// タイトルのメニュー（モード選択へ / ゲームをやめる）。
// 十字キー（キーボードの矢印キーも可）で選び、選んでいるボタンは大きくなる。決定は A ボタン（Enter / スペース）
public class TitleMenu : MonoBehaviour
{
    // 選べるボタン（順番に。1つ目＝モード選択へ、2つ目＝ゲームをやめる）
    [SerializeField] private RectTransform[] k_Buttons;
    // モード選択のシーン名（Build Settings に入れておくこと）
    [SerializeField] private string k_ModeSelectScene = "ModeSelectScene";

    // 選んでいるときの大きさ（選んでいないときは 1）
    [SerializeField] private float k_SelectedScale = 1.25f;
    // 大きさが変わる速さ
    [SerializeField] private float k_ScaleLerpRate = 14.0f;
    [SerializeField] private Color k_SelectedColor = Color.white;
    [SerializeField] private Color k_NormalColor = new Color(0.65f, 0.65f, 0.65f, 1.0f);

    private int selected_ = 0;

    void Start()
    {
        for (int i = 0; i < k_Buttons.Length; i++)
        {
            // マウスでも選べる・押せるようにしておく
            MenuButtonPointer pointer = k_Buttons[i].gameObject.AddComponent<MenuButtonPointer>();
            pointer.Init(this, i);
        }
        Apply(true);
    }

    void Update()
    {
        // 遷移の演出中は操作を受け付けない
        if (SceneTransition.IsBusy)
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
        selected_ = index;
    }

    public void Activate(int index)
    {
        if (SceneTransition.IsBusy)
        {
            return;
        }
        if (index == 0)
        {
            SceneTransition.Go(k_ModeSelectScene);
        }
        else
        {
            Quit();
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

    // 選んでいるボタンを大きく・明るくする
    void Apply(bool immediate)
    {
        for (int i = 0; i < k_Buttons.Length; i++)
        {
            bool on = i == selected_;
            float target = on ? k_SelectedScale : 1.0f;
            float scale = immediate ? target : Mathf.Lerp(k_Buttons[i].localScale.x, target, 1.0f - Mathf.Exp(-k_ScaleLerpRate * Time.unscaledDeltaTime));
            k_Buttons[i].localScale = new Vector3(scale, scale, 1.0f);

            Image image = k_Buttons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = on ? k_SelectedColor : k_NormalColor;
            }
        }
    }
}

// マウスでボタンを選ぶ・押すための補助
public class MenuButtonPointer : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private TitleMenu menu_;
    private int index_;

    public void Init(TitleMenu menu, int index)
    {
        menu_ = menu;
        index_ = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        menu_.Select(index_);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        menu_.Activate(index_);
    }
}
