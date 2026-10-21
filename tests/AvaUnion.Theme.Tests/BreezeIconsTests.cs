using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using AvaUnion.BreezeIcons;
using AvaUnion.Icons;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class BreezeIconsTests
{
    private static readonly BreezeIconProvider s_provider = new();

    private static DrawingGroup Drawing(string name, double size, Color? tint) =>
        (DrawingGroup)((DrawingImage)s_provider.GetIcon(name, tint, new Size(size, size), 1.0)!).Drawing!;

    private static IEnumerable<GeometryDrawing> Parts(string name, double size, Color? tint) =>
        Flatten(Drawing(name, size, tint)).Skip(1);

    private static IEnumerable<GeometryDrawing> Flatten(Drawing drawing) => drawing switch
    {
        GeometryDrawing geometry => [geometry],
        DrawingGroup group => group.Children.SelectMany(Flatten),
        _ => [],
    };

    private sealed class Marker(string name) : IIconProvider
    {
        public IImage? GetIcon(string iconName, Color? tint, Size size, double scale) =>
            iconName == name ? new DrawingImage(new GeometryDrawing { Geometry = new RectangleGeometry(new Rect(0, 0, 1, 1)), Brush = Brushes.Red }) : null;
    }

    [Fact]
    public void Every_Listed_Name_Is_Bundled()
    {
        var listed = File.ReadAllLines(Path.Combine(TestData.RepoRoot, "src", "AvaUnion.BreezeIcons", "icons.txt")).Where(l => l.Length > 0);
        Assert.Empty(listed.Except(s_provider.Names));
    }

    [AvaloniaFact]
    public void Every_Bundled_Icon_Reads()
    {
        var archive = BreezeArchive.Load();
        Assert.True(archive.Icons.Count > 7000, archive.Icons.Count.ToString());
        var failed = archive.Icons
            .SelectMany(icon => icon.Value.Select(size => (Name: $"{size.Key}/{icon.Key}", size.Value.Blob)))
            .Where(entry =>
            {
                using var stream = archive.Read(entry.Blob);
                return SvgIcon.Read(stream) is null;
            })
            .Select(entry => entry.Name)
            .ToList();
        Assert.Empty(failed);
    }

    [AvaloniaFact]
    public void The_Nearest_Size_At_Least_As_Large_Is_Used()
    {
        Assert.Equal(16, Drawing("document-save", 16, null).GetBounds().Width, 0.5);
        Assert.Equal(22, Drawing("document-save", 20, null).GetBounds().Width, 0.5);
        Assert.Equal(32, Drawing("document-save", 64, null).GetBounds().Width, 0.5);
        Assert.Null(s_provider.GetIcon("no-such-icon", null, new Size(16, 16), 1.0));
    }

    [AvaloniaFact]
    public void Text_Parts_Take_The_Tint_And_Other_Parts_Keep_Their_Color()
    {
        var tint = Colors.Lime;
        var save = Parts("document-save", 16, tint).Single();
        Assert.Equal(tint, ((ISolidColorBrush)save.Brush!).Color);

        var colors = Parts("emblem-error", 16, tint).Select(d => ((ISolidColorBrush)d.Brush!).Color).ToList();
        Assert.Contains(Color.Parse("#da4453"), colors);
        Assert.Contains(Colors.White, colors);
        Assert.Contains(colors, c => c.R == tint.R && c.G == tint.G && c.B == tint.B && c.A < 255);
    }

    [AvaloniaFact]
    public void Parts_Without_A_Fill_Are_Black_As_In_Svg()
    {
        var colors = Parts("folder", 32, Colors.White).Select(d => ((ISolidColorBrush)d.Brush!).Color).ToList();
        Assert.Equal(Color.Parse("#3daee9"), colors[0]);
        Assert.Equal(Color.FromArgb(84, 0, 0, 0), colors[1]);
        Assert.Equal(Color.FromArgb(51, 255, 255, 255), colors[2]);
        Assert.Equal(Color.FromArgb(51, 0, 0, 0), colors[3]);
    }

    [AvaloniaFact]
    public void Group_Transforms_Are_Applied()
    {
        var bounds = Drawing("text-x-generic", 22, null).Children[1].GetBounds();
        Assert.True(bounds.Width > 10 && new Rect(0, 0, 22, 22).Contains(bounds), bounds.ToString());
    }

    [AvaloniaFact]
    public void Fallbacks_Come_After_The_System_Theme_And_Before_The_Glyphs()
    {
        IconProviders.RegisterFallback(new Marker("breeze-test-only"));
        IconProviders.RegisterFallback(new Marker("folder"));
        Assert.NotNull(IconProviders.Get("breeze-test-only", null, new Size(16, 16), 1.0));
        Assert.NotNull(IconProviders.Get("folder", null, new Size(16, 16), 1.0));
        BreezeIconSet.Register();
        BreezeIconSet.Register();
        Assert.NotNull(IconProviders.Get("password-show-on", null, new Size(16, 16), 1.0));
    }
}
