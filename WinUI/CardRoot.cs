using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AngkleChen;

/// <summary>
/// 卡片根容器。
///
/// 两件事：
/// 1. 钉住光标形状 —— 右键弹菜单后不动鼠标时，光标会停在拖拽/命中测试
///    留下的形状，容器不再负责恢复。ProtectedCursor 是 protected 的，
///    只能靠继承设置。
/// 2. 在最内层截获滚轮 —— WPF 版靠 PreviewMouseWheel（隧道事件）抢在
///    ScrollViewer 之前处理，WinUI 3 只有冒泡（从内向外），挂在容器上
///    永远晚一步。卡片是命中的最内层元素，它的类处理器就是冒泡链第一个，
///    在这里拦等价于 WPF 的 Preview。
/// </summary>
public sealed class CardRoot : Grid
{
    /// <summary>滚轮被截获时触发，参数为原始 delta（一格 ±120）</summary>
    public static event Action<int>? WheelScrolled;

    public CardRoot()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

        AddHandler(PointerWheelChangedEvent, new PointerEventHandler(OnWheel), true);
    }

    // WinUI 的 UIElement 没有可重写的 OnPointerWheelChanged（WPF 才有），
    // 但卡片是事件源，挂在自身上仍然是冒泡链的第一站，效果等价
    private void OnWheel(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        if (delta == 0) return;

        e.Handled = true;
        WheelScrolled?.Invoke(delta);
    }
}
