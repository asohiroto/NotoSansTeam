using UnityEngine;
using UnityEngine.EventSystems;

// メニュー側が実装する、選ぶ・決定する操作
public interface IMenuSelector
{
    void Select(int index);
    void Activate(int index);
}

// マウスでボタンを選ぶ・押すための補助（ボタンに付けて使う）
public class MenuButtonPointer : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private IMenuSelector menu_;
    private int index_;

    public void Init(IMenuSelector menu, int index)
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
