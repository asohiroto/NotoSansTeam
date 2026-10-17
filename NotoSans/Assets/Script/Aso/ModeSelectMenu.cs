using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// モード選択のメニュー。ステージのボタン（StageCard）を十字キー（矢印キーも可）で選び、A ボタンで遷移する。
// B ボタン（Esc）でタイトルへ戻る。選んでいるボタンは大きくなる
public class ModeSelectMenu : MonoBehaviour, IMenuSelector
{
    // 並べたステージのボタン（左から順に）
    [SerializeField] private StageCard[] k_Cards;
    // 戻る先のシーン名
    [SerializeField] private string k_TitleScene = "TitleScene";

    // 選んでいるときの大きさ（選んでいないときは 1）
    [SerializeField] private float k_SelectedScale = 1.12f;
    // 大きさが変わる速さ
    [SerializeField] private float k_ScaleLerpRate = 14.0f;
    [SerializeField] private Color k_SelectedColor = Color.white;
    [SerializeField] private Color k_NormalColor = new Color(0.62f, 0.64f, 0.72f, 1.0f);

    private int selected_ = 0;

    void Start()
    {
        for (int i = 0; i < k_Cards.Length; i++)
        {
            // マウスでも選べる・押せるようにしておく
            MenuButtonPointer pointer = k_Cards[i].gameObject.AddComponent<MenuButtonPointer>();
            pointer.Init(this, i);
            k_Cards[i].Refresh();
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
        bool back = false;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.left.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame) { move = -1; }
            if (pad.dpad.right.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame) { move = 1; }
            if (pad.buttonSouth.wasPressedThisFrame) { submit = true; }
            if (pad.buttonEast.wasPressedThisFrame) { back = true; }
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) { move = -1; }
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) { move = 1; }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { submit = true; }
            if (keyboard.escapeKey.wasPressedThisFrame) { back = true; }
        }

        if (move != 0 && k_Cards.Length > 0)
        {
            selected_ = (selected_ + move + k_Cards.Length) % k_Cards.Length;
        }
        if (submit)
        {
            Activate(selected_);
        }
        else if (back)
        {
            SceneTransition.Go(k_TitleScene);
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
        if (SceneTransition.IsBusy || index < 0 || index >= k_Cards.Length)
        {
            return;
        }
        SceneTransition.Go(k_Cards[index].SceneName);
    }

    // 選んでいるボタンを大きく・明るくする
    void Apply(bool immediate)
    {
        for (int i = 0; i < k_Cards.Length; i++)
        {
            bool on = i == selected_;
            RectTransform rect = (RectTransform)k_Cards[i].transform;
            float target = on ? k_SelectedScale : 1.0f;
            float scale = immediate ? target : Mathf.Lerp(rect.localScale.x, target, 1.0f - Mathf.Exp(-k_ScaleLerpRate * Time.unscaledDeltaTime));
            rect.localScale = new Vector3(scale, scale, 1.0f);

            Image image = k_Cards[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = on ? k_SelectedColor : k_NormalColor;
            }
        }
    }
}
