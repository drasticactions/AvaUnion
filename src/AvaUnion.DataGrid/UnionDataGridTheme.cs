using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;

namespace AvaUnion.DataGrid;

/// <summary>Control themes for DataGrid. Add it after <see cref="UnionTheme"/>.</summary>
public class UnionDataGridTheme : Styles
{
    static UnionDataGridTheme() => DataGridHints.Register();

    /// <summary>Creates the theme.</summary>
    public UnionDataGridTheme()
    {
        Resources.MergedDictionaries.Add(new Themes.ControlThemes());
        var cell = new ResourceDictionary();
        cell.AddDeferred("DataGridCellTextBoxTheme", _ => CellTextBoxTheme());
        Resources.MergedDictionaries.Add(cell);
    }

    private ControlTheme CellTextBoxTheme()
    {
        object? textBox = null;
        _ = Owner?.TryGetResource(typeof(TextBox), null, out textBox) == true || Application.Current?.TryGetResource(typeof(TextBox), null, out textBox) == true;
        return new ControlTheme(typeof(TextBox))
        {
            BasedOn = textBox as ControlTheme,
            Setters =
            {
                new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Stretch),
                new Setter(Layoutable.MinHeightProperty, 0.0),
            },
        };
    }
}
