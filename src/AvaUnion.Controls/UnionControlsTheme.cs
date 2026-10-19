using Avalonia.Styling;

namespace AvaUnion.Controls;

/// <summary>Control themes for the controls of this library. Add it after <see cref="UnionTheme"/>.</summary>
public class UnionControlsTheme : Styles
{
    /// <summary>Creates the theme.</summary>
    public UnionControlsTheme()
    {
        Resources.MergedDictionaries.Add(new Themes.ControlThemes());
        Add(new Themes.ControlStyles());
    }
}
