using Avalonia;
using Avalonia.Controls;

namespace AvaUnion.Controls;

/// <summary>Shows a header with the row number before each row of a <see cref="TableView"/>.</summary>
public static class TableRowHeaders
{
    /// <summary>Defines the IsVisible attached property. Set it on a table view.</summary>
    public static readonly AttachedProperty<bool> IsVisibleProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsVisible", typeof(TableRowHeaders), inherits: true);

    /// <summary>Gets the value of the IsVisible attached property.</summary>
    public static bool GetIsVisible(Control control) => control.GetValue(IsVisibleProperty);

    /// <summary>Sets the value of the IsVisible attached property.</summary>
    public static void SetIsVisible(Control control, bool value) => control.SetValue(IsVisibleProperty, value);
}
