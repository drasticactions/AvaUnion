namespace AvaUnion.Presenters.Layout;

/// <summary>Margins on four sides.</summary>
/// <param name="Left">The left margin.</param>
/// <param name="Top">The top margin.</param>
/// <param name="Right">The right margin.</param>
/// <param name="Bottom">The bottom margin.</param>
public readonly record struct LayoutMargins(double Left, double Top, double Right, double Bottom)
{
    /// <summary>Makes margins with the same value on all sides.</summary>
    public static LayoutMargins Uniform(double v) => new(v, v, v, v);

    /// <summary>Makes margins from an Avalonia thickness.</summary>
    public static LayoutMargins From(Avalonia.Thickness t) => new(t.Left, t.Top, t.Right, t.Bottom);
}
