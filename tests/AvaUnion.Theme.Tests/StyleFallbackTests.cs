using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Markup;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class StyleFallbackTests
{
    [AvaloniaFact]
    public void Unset_Text_Color_Falls_Back_To_The_Palette()
    {
        var engine = UnionEnvironment.Engine;
        foreach (var (spec, group, role) in new[]
                 {
                     ("View.Foreground.Text", ColorGroup.Active, (int)ForegroundRole.Text),
                     ("View.Foreground.Inactive", ColorGroup.Active, (int)ForegroundRole.Inactive),
                     ("Disabled.View.Foreground.Text", ColorGroup.Disabled, (int)ForegroundRole.Text),
                 })
        {
            var converter = new StyleValueConverter("Text.Color", fallbackColor: spec);
            var brush = Assert.IsAssignableFrom<ISolidColorBrush>(converter.Convert(ResolvedStyle.Unmatched, typeof(IBrush), null, CultureInfo.InvariantCulture));
            Assert.Equal(engine.SchemeColor(group, ColorSet.View, ColorRoleKind.Foreground, role), brush.Color);
        }
    }

    [AvaloniaFact]
    public void Alignment_Converts_As_Union_Does_For_Qt()
    {
        Assert.Equal(VerticalAlignment.Top, StyleValueConverter.ConvertLeaf(UnionAlignment.Start, typeof(VerticalAlignment)));
        Assert.Equal(VerticalAlignment.Center, StyleValueConverter.ConvertLeaf(UnionAlignment.Unspecified, typeof(VerticalAlignment)));
        Assert.Equal(VerticalAlignment.Bottom, StyleValueConverter.ConvertLeaf(UnionAlignment.End, typeof(VerticalAlignment)));
        Assert.Equal(TextAlignment.Start, StyleValueConverter.ConvertLeaf(UnionAlignment.Unspecified, typeof(TextAlignment)));
        Assert.Equal(TextAlignment.Center, StyleValueConverter.ConvertLeaf(UnionAlignment.Center, typeof(TextAlignment)));
        Assert.Equal(TextAlignment.End, StyleValueConverter.ConvertLeaf(UnionAlignment.End, typeof(TextAlignment)));
    }

    [AvaloniaFact]
    public void Default_Button_Is_Highlighted()
    {
        Assert.True(StateMapper.Map(new Button { IsDefault = true }, "button", new List<string>()).HasFlag(UnionStates.Highlighted));
        Assert.False(StateMapper.Map(new Button(), "button", new List<string>()).HasFlag(UnionStates.Highlighted));
    }
}
