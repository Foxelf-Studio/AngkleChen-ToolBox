using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace AngkleChen;

/// <summary>
/// 卡片根容器。
///
/// 存在的唯一理由：鼠标右键卡片、弹菜单后不动鼠标时，光标会停在
/// 拖拽/命中测试留下的形状（忙、调整大小箭头）直到移动才恢复。
/// GridView 开了 CanDragItems 之后容器会自己改光标，而我们换掉了
/// GridViewItem 模板，它不再负责恢复。
///
/// UIElement.ProtectedCursor 是 protected 的，只能靠继承来设置，
/// 所以这里用一个只做一件事的 Grid 子类，把光标钉死成箭头。
/// </summary>
public sealed class CardRoot : Grid
{
    public CardRoot()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }
}
