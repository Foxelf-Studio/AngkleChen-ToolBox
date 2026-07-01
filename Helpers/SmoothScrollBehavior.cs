using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace 陈叔叔工具箱.Helpers;

/// <summary>
/// 平滑滚动行为 - 拦截鼠标滚轮事件，使用逐帧减速动画实现惯性滚动
/// </summary>
public static class SmoothScrollBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool),
            typeof(SmoothScrollBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private class ScrollState
    {
        public double Velocity;        // 当前速度（像素/帧）
        public bool IsAnimating;
        public DateTime LastWheelTime;
    }

    private static readonly Dictionary<ScrollViewer, ScrollState> _states = new();
    private static bool _renderingAttached;

    // 惯性参数
    private const double Friction = 0.85;
    private const double MinVelocity = 0.5;
    private const double WheelMultiplierSV = 0.5;    // ScrollViewer 倍数（卡片页）
    private const double WheelMultiplierLB = 0.15;   // ListBox 倍数（导航栏）
    private const double MaxVelocity = 120;

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            if (d is ScrollViewer sv)
                Bind(sv, WheelMultiplierSV);
            else if (d is ListBox lb)
                lb.Loaded += (s, _) => lb.Dispatcher.BeginInvoke(
                    new Action(() => { var sv = FindChild<ScrollViewer>(lb); if (sv != null) Bind(sv, WheelMultiplierLB); }),
                    System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private static void Bind(ScrollViewer sv, double multiplier)
    {
        if (_states.ContainsKey(sv)) return;
        var state = new ScrollState();
        _states[sv] = state;

        sv.PreviewMouseWheel += (_, args) =>
        {
            args.Handled = true;
            var delta = -args.Delta * multiplier;
            var now = DateTime.Now;
            var elapsed = (now - state.LastWheelTime).TotalMilliseconds;
            state.LastWheelTime = now;

            // 触控板连续滚动
            if (elapsed < 50 && Math.Abs(delta) < 100)
            {
                state.Velocity += delta * 0.3 * 0.6;
            }
            else
            {
                state.Velocity = delta;
            }

            state.Velocity = Math.Max(-MaxVelocity, Math.Min(MaxVelocity, state.Velocity));
            state.IsAnimating = true;

            // 动画开始时重新注册渲染事件
            if (!_renderingAttached)
            {
                CompositionTarget.Rendering += OnRendering;
                _renderingAttached = true;
            }
        };

        sv.Unloaded += (_, _) =>
        {
            _states.Remove(sv);
            if (_states.Count == 0 && _renderingAttached)
            {
                CompositionTarget.Rendering -= OnRendering;
                _renderingAttached = false;
            }
        };
    }

    private static void OnRendering(object? sender, EventArgs e)
    {
        bool anyAnimating = false;

        foreach (var (sv, state) in _states)
        {
            if (!state.IsAnimating) continue;

            anyAnimating = true;
            state.Velocity *= Friction;

            var newOffset = sv.VerticalOffset + state.Velocity;

            // 边界检测
            if (newOffset <= 0)
            {
                sv.ScrollToVerticalOffset(0);
                state.Velocity = 0;
                state.IsAnimating = false;
            }
            else if (newOffset >= sv.ScrollableHeight)
            {
                sv.ScrollToVerticalOffset(sv.ScrollableHeight);
                state.Velocity = 0;
                state.IsAnimating = false;
            }
            else
            {
                sv.ScrollToVerticalOffset(newOffset);

                if (Math.Abs(state.Velocity) < MinVelocity)
                {
                    state.Velocity = 0;
                    state.IsAnimating = false;
                }
                else
                {
                    anyAnimating = true;
                }
            }
        }

        // 没有动画时取消注册，减少每帧开销
        if (!anyAnimating && _renderingAttached)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderingAttached = false;
        }
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
