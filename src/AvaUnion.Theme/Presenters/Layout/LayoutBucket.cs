using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaUnion.Presenters.Layout;

/// <summary>Items with the same horizontal alignment in one container.</summary>
public sealed class LayoutBucket
{
    /// <summary>The position.</summary>
    public LayoutSize Position { get; set; }

    /// <summary>The size.</summary>
    public LayoutSize Size { get; set; }

    /// <summary>The implicit size.</summary>
    public LayoutSize ImplicitSize { get; set; }

    /// <summary>The minimum size.</summary>
    public LayoutSize MinimumSize { get; set; }

    /// <summary>The spacing between items.</summary>
    public double Spacing { get; set; }

    /// <summary>True when one or more items are stack-center.</summary>
    public bool StackCenter { get; set; }

    /// <summary>True when one or more items are stack-fill.</summary>
    public bool StackFill { get; set; }

    /// <summary>The items.</summary>
    public List<LayoutItem> Items { get; private set; } = [];

    /// <summary>True when the bucket has no items.</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>Sorts the items by order. The sort is stable.</summary>
    public void Sort() => Items = [.. Items.OrderBy(i => i.Order)];

    /// <summary>Puts the items in a row. When the items stack, puts them in a stack.</summary>
    public void Layout()
    {
        var x = 0.0;
        var minWidth = 0.0;
        var minHeight = 0.0;
        var maxWidth = 0.0;
        var maxHeight = 0.0;
        var totalHeight = 0.0;

        foreach (var item in Items)
        {
            var m = item.Margins;
            if (StackCenter || StackFill)
            {
                item.Position = item.Position with { Width = 0.0 };
                totalHeight += m.Top + m.Bottom + item.ImplicitSize.Height;

                minWidth = Math.Max(minWidth, m.Left + item.MinimumSize.Width + m.Right);
                minHeight += m.Top + item.MinimumSize.Height + m.Bottom;

                maxWidth = Math.Max(maxWidth, m.Left + item.ImplicitSize.Width + m.Right);
                maxHeight = totalHeight;
            }
            else
            {
                if (IsFuzzyNull(item.ImplicitSize.Width))
                {
                    continue;
                }

                if (x > 0.0)
                {
                    x += Spacing;
                }

                x += m.Left;
                item.Position = item.Position with { Width = x };
                x += item.ImplicitSize.Width + m.Right;

                minWidth += m.Left + item.MinimumSize.Width + m.Right;
                minHeight = Math.Max(minHeight, m.Top + item.MinimumSize.Height + m.Bottom);

                maxWidth = x;
                maxHeight = Math.Max(maxHeight, m.Top + item.ImplicitSize.Height + m.Bottom);
            }
        }

        ImplicitSize = new LayoutSize(Math.Ceiling(Math.Max(maxWidth, 0.0)), Math.Ceiling(Math.Max(maxHeight, 0.0)));
        MinimumSize = new LayoutSize(minWidth, minHeight);
    }

    internal static bool IsFuzzyNull(double d) => Math.Abs(d) <= 0.000000000001;
}
