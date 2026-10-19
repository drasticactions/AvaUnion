using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;

namespace AvaUnion.Elements;

/// <summary>
/// Sets hints and attributes from control properties.
/// </summary>
public static class AutoHints
{
    private static readonly HashSet<AvaloniaProperty> s_watched =
    [
        WindowBase.IsActiveProperty,
        Button.FlyoutProperty,
        ContentControl.ContentProperty,
        ComboBox.IsEditableProperty,
        TextBox.AcceptsReturnProperty,
        ScrollBar.OrientationProperty,
        Slider.OrientationProperty,
        ProgressBar.OrientationProperty,
        ProgressBar.IsIndeterminateProperty,
        ScrollViewer.ExtentProperty,
        ScrollViewer.ViewportProperty,
        ScrollViewer.IsExpandedProperty,
        MenuItem.ToggleTypeProperty,
        MenuItem.IsSubMenuOpenProperty,
        Expander.ExpandDirectionProperty,
        Expander.IsExpandedProperty,
        TabItem.TabStripPlacementProperty,
        TabControl.TabStripPlacementProperty,
        NumericUpDown.MinimumProperty,
        NumericUpDown.MaximumProperty,
        NumericUpDown.IsReadOnlyProperty,
        NumericUpDown.FormatStringProperty,
        Layoutable.WidthProperty,
        Layoutable.MaxWidthProperty,
        ToggleButton.IsCheckedProperty,
        Window.WindowStateProperty,
        Separator.HorizontalAlignmentProperty,
        DataValidationErrors.HasErrorsProperty,
        GridSplitter.ResizeDirectionProperty,
        ListBoxItem.IsSelectedProperty,
        CommandBarSeparator.IsInOverflowProperty,
        CommandBarButton.IconProperty,
        CommandBarButton.LabelProperty,
        CommandBarButton.LabelPositionProperty,
        CommandBarButton.IsCompactProperty,
        CommandBarButton.IsInOverflowProperty,
        CommandBarToggleButton.IconProperty,
        CommandBarToggleButton.LabelProperty,
        CommandBarToggleButton.LabelPositionProperty,
        CommandBarToggleButton.IsCompactProperty,
        CommandBarToggleButton.IsInOverflowProperty,
        TreeViewItem.IsExpandedProperty,
        SplitView.PanePlacementProperty,
    ];

    /// <summary>True when a change to <paramref name="property"/> can change the automatic hints.</summary>
    public static bool IsWatched(AvaloniaProperty property) => s_watched.Contains(property) || property == ItemsControl.ItemCountProperty || UnionHintProviders.IsWatched(property);

    /// <summary>Adds the automatic hints and attributes of <paramref name="element"/>.</summary>
    public static void Compute(StyledElement element, string type, List<string> hints, List<KeyValuePair<string, object>> attributes)
    {
        switch (element)
        {
            case WindowBase window:
                Hint(hints, "inactive", !window.IsActive);
                if (window is Window w)
                {
                    Hint(hints, "maximized", w.WindowState == WindowState.Maximized);
                }

                break;

            case PopupRoot or OverlayPopupHost:
                if (ChainBuilder.OwningPopup((Visual)element) is { } popup)
                {
                    AddClasses(popup, hints);
                    AddSpaced(popup.GetValue(Union.HintsProperty), hints);
                    if (popup.Child is ContextMenu or MenuFlyoutPresenter or Menu)
                    {
                        Hint(hints, "menu", true);
                    }
                }
                else if (element is OverlayPopupHost host && host.Content is MenuFlyoutPresenter or ContextMenu)
                {
                    Hint(hints, "menu", true);
                }

                break;

            case Avalonia.Controls.Primitives.CalendarDayButton or Avalonia.Controls.Primitives.CalendarButton:
                Hint(hints, "current-month", !element.Classes.Contains(":inactive"));
                break;

            case CheckBox or RadioButton:
            {
                var toggle = (ToggleButton)element;
                Hint(hints, "with-icon", HasIcon(toggle.Content));
                attributes.Add(new("display", DisplayOf(toggle.Content)));
                break;
            }

            case CommandBarButton or CommandBarToggleButton:
            {
                var command = (ICommandBarElement)element;
                var (icon, label, position) = element is CommandBarButton b
                    ? (b.Icon, b.Label, b.LabelPosition)
                    : (((CommandBarToggleButton)element).Icon, ((CommandBarToggleButton)element).Label, ((CommandBarToggleButton)element).LabelPosition);
                var showLabel = !string.IsNullOrEmpty(label) && (command.IsInOverflow || (!command.IsCompact && position != CommandBarDefaultLabelPosition.Collapsed));
                Hint(hints, "with-icon", icon is not null);
                attributes.Add(new("display", icon is null ? "text-only"
                    : !showLabel ? "icon-only"
                    : command.IsInOverflow || position == CommandBarDefaultLabelPosition.Right ? "text-beside-icon"
                    : "text-under-icon"));
                break;
            }

            case ToggleButton toggle when type is "button" or "toolbutton":
                Hint(hints, "with-icon", HasIcon(toggle.Content));
                attributes.Add(new("display", DisplayOf(toggle.Content)));
                break;

            case Button button when type is "button" or "toolbutton":
                Hint(hints, "with-icon", HasIcon(button.Content));
                if (button is DropDownButton)
                {
                    Hint(hints, "with-menu", true);
                }
                else
                {
                    Hint(hints, "with-menu", button.Flyout is not null && type == "button");
                }

                if (button is HyperlinkButton)
                {
                    Hint(hints, "flat", true);
                }

                Hint(hints, "changed", DataValidationErrors.GetHasErrors(button));
                attributes.Add(new("display", DisplayOf(button.Content)));
                break;

            case SplitButton split:
                Hint(hints, "with-icon", HasIcon(split.Content));
                Hint(hints, "with-menu-button", true);
                attributes.Add(new("display", DisplayOf(split.Content)));
                break;

            case ComboBox combo:
                Hint(hints, "editable", combo.IsEditable);
                break;

            case TabStripItem stripItem:
                Hint(hints, "with-icon", HasIcon(stripItem.Content));
                attributes.Add(new("direction", "top"));
                break;

            case ListBoxItem pip when type == "pageindicatordelegate":
                Hint(hints, "current", pip.IsSelected);
                break;

            case ComboBoxItem or ListBoxItem:
                Hint(hints, "inside-list", true);
                Hint(hints, "hover-enabled", true);
                Hint(hints, "last-in-list", IsLastInList((Control)element));
                if (element is TableViewRow tableRow && ItemsControl.ItemsControlFromItemContainer(tableRow) is { } table)
                {
                    Hint(hints, "alternating-colors", !table.Classes.Contains("no-alternating-colors") && table.IndexFromContainer(tableRow) % 2 == 1);
                }

                break;

            case Control row when type == "treeviewdelegate" && row.TemplatedParent is TreeViewItem tree:
                Hint(hints, "expanded", tree.IsExpanded);
                Hint(hints, "hover-enabled", true);
                Hint(hints, "alternating-colors", TreeRow(tree) % 2 == 1);
                break;

            case TextBox box:
                Hint(hints, "changed", DataValidationErrors.GetHasErrors(box));
                break;

            case ScrollBar bar:
                Hint(hints, bar.Orientation == Orientation.Horizontal ? "horizontal" : "vertical", true);
                Hint(hints, "noninteractive", bar.AllowAutoHide && !bar.IsExpanded);
                break;

            case Slider slider:
                Hint(hints, slider.Orientation == Orientation.Horizontal ? "horizontal" : "vertical", true);
                break;

            case ProgressBar progress:
                Hint(hints, "indeterminate", progress.IsIndeterminate);
                Hint(hints, progress.Orientation == Orientation.Horizontal ? "horizontal" : "vertical", true);
                break;

            case ListBox or TreeView when type == "scrollview":
                Hint(hints, "framed-background", true);
                break;

            case ScrollViewer viewer:
                Hint(hints, "vertical-scroll", viewer.Extent.Height > viewer.Viewport.Height + 0.5);
                Hint(hints, "horizontal-scroll", viewer.Extent.Width > viewer.Viewport.Width + 0.5);
                break;

            case MenuItem item:
                Hint(hints, "with-submenu", item.HasSubMenu && type == "menuitem");
                Hint(hints, "exclusive", item.ToggleType == MenuItemToggleType.Radio);
                break;

            case Expander expander:
                Hint(hints, "expanded", expander.IsExpanded);
                attributes.Add(new("direction", expander.ExpandDirection.ToString().ToLowerInvariant()));
                break;

            case TabItem tab:
                Hint(hints, "with-icon", HasIcon(tab.Header));
                if (ItemsControl.ItemsControlFromItemContainer(tab) is { } tabs && tabs.IndexFromContainer(tab) is var index and >= 0)
                {
                    Hint(hints, "first", index == 0);
                    Hint(hints, "last", index == tabs.ItemCount - 1);
                }

                attributes.Add(new("direction", TabPosition(tab.TabStripPlacement ?? Dock.Top)));
                break;

            case TabStrip when type == "tabbar":
                attributes.Add(new("tab-position", "top"));
                break;

            case NumericUpDown spin:
                Hint(hints, "editable", !spin.IsReadOnly);
                Hint(hints, "constrained", IsSpaceConstrained(spin));
                break;

            case CommandBarSeparator commandSeparator:
                Hint(hints, commandSeparator.IsInOverflow ? "horizontal" : "vertical", true);
                break;

            case Separator separator:
                if (type == "toolseparator")
                {
                    var horizontal = separator.Parent is StackPanel { Orientation: Orientation.Vertical } || separator.HorizontalAlignment == HorizontalAlignment.Stretch && separator.Parent is not StackPanel { Orientation: Orientation.Horizontal };
                    Hint(hints, horizontal ? "horizontal" : "vertical", true);
                }

                break;

            case GridSplitter splitter:
                var rows = splitter.ResizeDirection == GridResizeDirection.Rows
                    || (splitter.ResizeDirection == GridResizeDirection.Auto && splitter.HorizontalAlignment == HorizontalAlignment.Stretch && splitter.VerticalAlignment != VerticalAlignment.Stretch);
                Hint(hints, rows ? "vertical" : "horizontal", true);
                break;

            case NotificationCard card:
                var kind = card.NotificationType switch
                {
                    NotificationType.Success => "positive",
                    NotificationType.Warning => "warning",
                    NotificationType.Error => "error",
                    _ => null,
                };
                if (kind is not null)
                {
                    attributes.Add(new("type", kind));
                }

                attributes.Add(new("position", "inline"));
                break;

            case SplitView split:
                attributes.Add(new("edge", split.PanePlacement == SplitViewPanePlacement.Right ? "right" : "left"));
                break;
        }

        UnionHintProviders.Apply(element, type, hints, attributes);
    }

    private static int TreeRow(TreeViewItem target)
    {
        if (target.FindLogicalAncestorOfType<TreeView>() is not { } tree)
        {
            return -1;
        }

        var row = 0;
        return Walk(tree) ? row : -1;

        bool Walk(ItemsControl parent)
        {
            for (var i = 0; i < parent.ItemCount; i++)
            {
                if (parent.ContainerFromIndex(i) is not TreeViewItem item)
                {
                    continue;
                }

                if (ReferenceEquals(item, target))
                {
                    return true;
                }

                row++;
                if (item.IsExpanded && Walk(item))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal static void RefreshTabs(TabControl tabs)
    {
        foreach (var tab in tabs.GetRealizedContainers())
        {
            Union.GetNode(tab)?.Refresh();
        }
    }

    private static readonly HashSet<TreeView> s_pendingTrees = [];

    internal static void RefreshTreeRows(TreeViewItem changed)
    {
        if (changed.FindLogicalAncestorOfType<TreeView>() is not { } tree)
        {
            return;
        }

        RefreshTreeRows(tree);
        if (s_pendingTrees.Add(tree))
        {
            Dispatcher.UIThread.Post(() =>
            {
                s_pendingTrees.Remove(tree);
                RefreshTreeRows(tree);
            }, DispatcherPriority.Loaded);
        }
    }

    private static void RefreshTreeRows(TreeView tree)
    {
        foreach (var row in tree.GetVisualDescendants())
        {
            if (row is StyledElement { TemplatedParent: TreeViewItem } element && Union.GetNode(element) is { Type: "treeviewdelegate" } node)
            {
                node.Refresh();
            }
        }
    }

    private static bool IsSpaceConstrained(NumericUpDown spin)
    {
        var limit = !double.IsNaN(spin.Width) ? spin.Width : spin.MaxWidth;
        if (double.IsInfinity(limit) || double.IsNaN(limit))
        {
            return false;
        }

        var spacing = (Union.GetStyle(spin) ?? Engine.ResolvedStyle.Unmatched).Layout.Spacing ?? 0.0;
        return LimitTextWidth(spin) + (UnconstrainedIndicatorWidth() * 2) + (spacing * 2) > limit;
    }

    internal static double LimitTextWidth(NumericUpDown spin)
    {
        var to = spin.Maximum.ToString(spin.FormatString, spin.NumberFormat);
        var from = spin.Minimum.ToString(spin.FormatString, spin.NumberFormat);
        var text = to.Length >= from.Length ? to : from;
        using var layout = new Avalonia.Media.TextFormatting.TextLayout(
            text, new Typeface(spin.FontFamily, spin.FontStyle, spin.FontWeight), spin.FontSize, null);
        return layout.Width;
    }

    private static (int Epoch, double Width)? s_indicatorWidth;

    private static double UnconstrainedIndicatorWidth()
    {
        var engine = UnionEnvironment.Engine;
        if (s_indicatorWidth is { } cached && cached.Epoch == engine.Epoch)
        {
            return cached.Width;
        }

        using var spinbox = engine.CreateElement();
        using var indicator = engine.CreateElement();
        spinbox.SetType("spinbox");
        indicator.SetType("indicator");
        var width = engine.Query(engine.DefaultStyle, [spinbox, indicator]).Layout.Width ?? 0.0;
        s_indicatorWidth = (engine.Epoch, width);
        return width;
    }

    /// <summary>True when button content shows an icon.</summary>
    public static bool HasIcon(object? content) => IsIcon(content) || (content is Panel panel && ContainsIcon(panel));

    internal static bool IsIcon(object? content) =>
        content is Image or PathIcon or IconElement or Avalonia.Controls.Shapes.Shape or Presenters.UnionIcon;

    private static bool ContainsIcon(Panel panel)
    {
        foreach (var child in panel.Children)
        {
            if (IsIcon(child))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The Union display of button content: icon-only, text-under-icon or text-beside-icon.</summary>
    public static string DisplayOf(object? content)
    {
        if (IsIcon(content))
        {
            return "icon-only";
        }

        if (content is StackPanel { Orientation: Orientation.Vertical } panel && ContainsIcon(panel))
        {
            return "text-under-icon";
        }

        return "text-beside-icon";
    }

    private static string TabPosition(Dock placement) => placement switch
    {
        Dock.Bottom => "bottom",
        Dock.Left => "left",
        Dock.Right => "right",
        _ => "top",
    };

    private static bool IsLastInList(Control container)
    {
        var owner = ItemsControl.ItemsControlFromItemContainer(container);
        if (owner is null)
        {
            return false;
        }

        var index = owner.IndexFromContainer(container);
        return index >= 0 && index == owner.ItemCount - 1;
    }

    private static void AddClasses(StyledElement element, List<string> hints)
    {
        foreach (var c in element.Classes)
        {
            if (c.Length > 0 && c[0] != ':')
            {
                Hint(hints, c, true);
            }
        }
    }

    internal static void AddSpaced(string? text, List<string> hints)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (var part in text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
        {
            Hint(hints, part, true);
        }
    }

    private static void Hint(List<string> hints, string hint, bool present)
    {
        if (present && !hints.Contains(hint))
        {
            hints.Add(hint);
        }
    }
}
