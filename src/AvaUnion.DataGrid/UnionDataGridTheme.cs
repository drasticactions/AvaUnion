using Avalonia.Controls;
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
    }
}
