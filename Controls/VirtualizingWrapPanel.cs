using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace 陈叔叔工具箱.Controls;

/// <summary>
/// 支持虚拟化的 WrapPanel。
///
/// WPF 自带的 <see cref="WrapPanel"/> 不是 IScrollInfo，放进 ScrollViewer 后会把所有子项一次性实例化——
/// 90 个工具就是 90 个 ToolCard（每个带 4 个 Storyboard + 一张解码后的位图），
/// 屏幕只显示十几个，其余全在内存里空转。
///
/// 本面板按"固定尺寸子项"计算行列，只实例化可视区域内的容器，
/// 并且对外用**像素**汇报滚动范围，这样 SmoothScrollBehavior 的惯性滚动可以原样复用。
/// </summary>
public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
{
    // ToolCard 是 205×140，DataTemplate 里 Margin=5 → 单元 215×150。
    // 首次布局后用实际子项尺寸校正，卡片改尺寸不需要改这里。
    private Size _itemSize = new(215, 150);

    private int _columns = 1;
    private Size _extent;
    private Size _viewport;
    private Size _lastExtent;
    private Size _lastViewport;
    private double _verticalOffset;
    private ScrollViewer? _scrollOwner;

    /// <summary>诊断用：最近一次布局实际实例化的子项数量</summary>
    public static int LastRealizedCount { get; private set; }

    // ── 测量 ──────────────────────────────────────
    protected override Size MeasureOverride(Size availableSize)
    {
        var itemCount = GetItemCount();

        // 有真实子项后，用它的 DesiredSize 校正单元尺寸
        if (InternalChildren.Count > 0 && InternalChildren[0] is FrameworkElement probe)
        {
            if (probe.DesiredSize.Width > 0 && probe.DesiredSize.Height > 0)
            {
                _itemSize = new Size(
                    Math.Max(_itemSize.Width, probe.DesiredSize.Width),
                    Math.Max(_itemSize.Height, probe.DesiredSize.Height));
            }
        }

        var width = double.IsInfinity(availableSize.Width) ? _itemSize.Width : availableSize.Width;
        _columns = Math.Max(1, (int)Math.Floor(width / _itemSize.Width));
        var rows = (int)Math.Ceiling(itemCount / (double)_columns);

        _extent = new Size(width, Math.Max(0, rows * _itemSize.Height));
        _viewport = new Size(width, double.IsInfinity(availableSize.Height) ? _extent.Height : availableSize.Height);

        CoerceOffsets();

        // 只在范围真的变了才通知 ScrollViewer。
        // 每次测量都调 InvalidateScrollInfo 会触发 ScrollChanged → 再次测量 → 布局死循环，
        // 表现为窗口先白屏十几秒再画出来。
        if (_extent != _lastExtent || _viewport != _lastViewport)
        {
            _lastExtent = _extent;
            _lastViewport = _viewport;
            _scrollOwner?.InvalidateScrollInfo();
        }

        return new Size(width, Math.Min(_extent.Height, _viewport.Height));
    }

    // ── 排布：只为可视行创建容器 ──────────────────
    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemCount = GetItemCount();
        if (itemCount == 0)
        {
            CleanUpItems(0, -1);
            LastRealizedCount = 0;
            return finalSize;
        }

        var firstRow = Math.Max(0, (int)Math.Floor(_verticalOffset / _itemSize.Height));
        var visibleRows = (int)Math.Ceiling(finalSize.Height / _itemSize.Height) + 1;

        var firstIndex = firstRow * _columns;
        var lastIndex = Math.Min(itemCount - 1, (firstRow + visibleRows) * _columns - 1);

        if (firstIndex > lastIndex)
        {
            CleanUpItems(0, -1);
            LastRealizedCount = 0;
            return finalSize;
        }

        CleanUpItems(firstIndex, lastIndex);

        var generator = ItemContainerGenerator;
        var startPosition = generator.GeneratorPositionFromIndex(firstIndex);
        var childIndex = startPosition.Offset == 0 ? startPosition.Index : startPosition.Index + 1;

        using (generator.StartAt(startPosition, GeneratorDirection.Forward, true))
        {
            for (var itemIndex = firstIndex; itemIndex <= lastIndex; itemIndex++, childIndex++)
            {
                if (generator.GenerateNext(out var isNewlyRealized) is not UIElement child)
                    break;

                if (isNewlyRealized)
                {
                    if (childIndex >= InternalChildren.Count)
                        AddInternalChild(child);
                    else
                        InsertInternalChild(childIndex, child);

                    generator.PrepareItemContainer(child);
                }

                child.Measure(_itemSize);

                var row = itemIndex / _columns;
                var column = itemIndex % _columns;
                child.Arrange(new Rect(
                    column * _itemSize.Width,
                    row * _itemSize.Height - _verticalOffset,
                    _itemSize.Width,
                    _itemSize.Height));
            }
        }

        LastRealizedCount = InternalChildren.Count;
        return finalSize;
    }

    /// <summary>回收可视范围之外的容器</summary>
    private void CleanUpItems(int firstIndex, int lastIndex)
    {
        var generator = ItemContainerGenerator;
        for (var i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
            if (itemIndex >= firstIndex && itemIndex <= lastIndex) continue;

            generator.Remove(new GeneratorPosition(i, 0), 1);
            RemoveInternalChildRange(i, 1);
        }
    }

    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        switch (args.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
            case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
            case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                break;

            case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                RemoveInternalChildRange(0, InternalChildren.Count);
                break;
        }

        InvalidateMeasure();
    }

    private int GetItemCount()
    {
        var owner = ItemsControl.GetItemsOwner(this);
        return owner?.Items.Count ?? 0;
    }

    private void CoerceOffsets()
    {
        var maxOffset = Math.Max(0, _extent.Height - _viewport.Height);
        if (_verticalOffset > maxOffset) _verticalOffset = maxOffset;
        if (_verticalOffset < 0) _verticalOffset = 0;
    }

    // ── IScrollInfo（单位：像素） ─────────────────
    public bool CanVerticallyScroll { get; set; } = true;
    public bool CanHorizontallyScroll { get; set; }

    public double ExtentWidth => _extent.Width;
    public double ExtentHeight => _extent.Height;
    public double ViewportWidth => _viewport.Width;
    public double ViewportHeight => _viewport.Height;
    public double HorizontalOffset => 0;
    public double VerticalOffset => _verticalOffset;

    public ScrollViewer? ScrollOwner
    {
        get => _scrollOwner;
        set => _scrollOwner = value;
    }

    public void LineUp() => SetVerticalOffset(_verticalOffset - 16);
    public void LineDown() => SetVerticalOffset(_verticalOffset + 16);
    public void LineLeft() { }
    public void LineRight() { }
    public void PageUp() => SetVerticalOffset(_verticalOffset - _viewport.Height);
    public void PageDown() => SetVerticalOffset(_verticalOffset + _viewport.Height);
    public void PageLeft() { }
    public void PageRight() { }
    public void MouseWheelUp() => SetVerticalOffset(_verticalOffset - _itemSize.Height);
    public void MouseWheelDown() => SetVerticalOffset(_verticalOffset + _itemSize.Height);
    public void MouseWheelLeft() { }
    public void MouseWheelRight() { }

    public void SetHorizontalOffset(double offset) { }

    public void SetVerticalOffset(double offset)
    {
        var maxOffset = Math.Max(0, _extent.Height - _viewport.Height);
        var clamped = Math.Max(0, Math.Min(offset, maxOffset));

        if (Math.Abs(clamped - _verticalOffset) < 0.1) return;

        _verticalOffset = clamped;
        _scrollOwner?.InvalidateScrollInfo();
        InvalidateArrange();
    }

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        // 卡片区域只需要纵向滚动，键盘焦点定位用得很少，这里给出保守实现
        if (visual is UIElement element && InternalChildren.Contains(element))
        {
            var index = InternalChildren.IndexOf(element);
            var top = (index / _columns) * _itemSize.Height;
            if (top < _verticalOffset) SetVerticalOffset(top);
            else if (top + _itemSize.Height > _verticalOffset + _viewport.Height)
                SetVerticalOffset(top + _itemSize.Height - _viewport.Height);
        }
        return rectangle;
    }
}
