using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AvaUnion.Icons;

/// <summary>
/// Finds icons in the freedesktop icon theme. It searches the theme from
/// <c>kdeglobals [Icons] Theme</c>, then its <c>Inherits</c> chain, then
/// <c>hicolor</c>. An SVG icon becomes single-color geometry with a tint. A PNG
/// icon is drawn without changes.
/// </summary>
public sealed class FreedesktopIconProvider : IIconProvider
{
    private readonly List<string> _baseDirs = [];
    private readonly List<string> _themes = [];
    private readonly Dictionary<string, string?> _files = [];
    private readonly Dictionary<string, (Geometry Geometry, Rect Box)?> _geometries = [];

    /// <summary>Creates the provider for the icon theme of the current user.</summary>
    public FreedesktopIconProvider(string? theme = null)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } dh ? dh : Path.Combine(home, ".local/share");
        _baseDirs.Add(Path.Combine(home, ".icons"));
        _baseDirs.Add(Path.Combine(dataHome, "icons"));
        var dataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS") is { Length: > 0 } dd ? dd : "/usr/local/share:/usr/share";
        foreach (var dir in dataDirs.Split(':', StringSplitOptions.RemoveEmptyEntries))
        {
            _baseDirs.Add(Path.Combine(dir, "icons"));
        }

        _baseDirs.Add("/usr/share/pixmaps");
        AddTheme(theme ?? ConfiguredTheme() ?? "breeze", 0);
        AddTheme("hicolor", 0);
    }

    /// <summary>The themes searched, in order.</summary>
    public IReadOnlyList<string> Themes => _themes;

    /// <inheritdoc/>
    public IImage? GetIcon(string name, Color? tint, Size size, double scale)
    {
        var file = FindFile(name, (int)Math.Round(Math.Max(size.Width, size.Height)));
        if (file is null)
        {
            return null;
        }

        if (file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            if (!_geometries.TryGetValue(file, out var svg))
            {
                svg = ReadSvg(file);
                _geometries[file] = svg;
            }

            if (svg is not { } icon)
            {
                return null;
            }

            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing { Geometry = new RectangleGeometry(icon.Box), Brush = Brushes.Transparent });
            group.Children.Add(new GeometryDrawing { Geometry = icon.Geometry, Brush = new SolidColorBrush(tint ?? Colors.Black) });
            return new DrawingImage(group);
        }

        try
        {
            return new Bitmap(file);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void AddTheme(string theme, int depth)
    {
        if (depth > 8 || _themes.Contains(theme))
        {
            return;
        }

        _themes.Add(theme);
        foreach (var baseDir in _baseDirs)
        {
            var index = Path.Combine(baseDir, theme, "index.theme");
            if (!File.Exists(index))
            {
                continue;
            }

            foreach (var line in File.ReadLines(index))
            {
                if (line.StartsWith("Inherits=", StringComparison.Ordinal))
                {
                    foreach (var parent in line["Inherits=".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        AddTheme(parent, depth + 1);
                    }

                    return;
                }
            }

            return;
        }
    }

    private string? FindFile(string name, int size)
    {
        var key = name + "@" + size.ToString(CultureInfo.InvariantCulture);
        if (_files.TryGetValue(key, out var cached))
        {
            return cached;
        }

        string? best = null;
        var bestScore = int.MaxValue;
        foreach (var theme in _themes)
        {
            foreach (var baseDir in _baseDirs)
            {
                var root = Path.Combine(baseDir, theme);
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var file in Candidates(root, name))
                {
                    var score = Score(file, size);
                    if (score < bestScore)
                    {
                        best = file;
                        bestScore = score;
                    }
                }
            }

            if (best is not null)
            {
                break;
            }
        }

        _files[key] = best;
        return best;
    }

    private static IEnumerable<string> Candidates(string root, string name)
    {
        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(root);
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (var first in dirs)
        {
            foreach (var ext in new[] { ".svg", ".png" })
            {
                var direct = Path.Combine(first, name + ext);
                if (File.Exists(direct))
                {
                    yield return direct;
                }
            }

            IEnumerable<string> second;
            try
            {
                second = Directory.EnumerateDirectories(first);
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var dir in second)
            {
                foreach (var ext in new[] { ".svg", ".png" })
                {
                    var file = Path.Combine(dir, name + ext);
                    if (File.Exists(file))
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    private static int Score(string file, int size)
    {
        var parts = file.Split(Path.DirectorySeparatorChar);
        foreach (var part in parts)
        {
            if (part == "scalable" || part == "symbolic")
            {
                return 0;
            }

            var digits = part.Split('x')[0].Split('@')[0];
            if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dirSize) && dirSize > 0 && dirSize < 1024)
            {
                return dirSize >= size ? 1 + dirSize - size : 1000 + size - dirSize;
            }
        }

        return file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? 0 : 500;
    }

    private static (Geometry Geometry, Rect Box)? ReadSvg(string file)
    {
        try
        {
            var group = new GeometryGroup { FillRule = FillRule.NonZero };
            Rect? viewBox = null;
            double width = 0, height = 0;
            using var reader = XmlReader.Create(file, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                switch (reader.LocalName)
                {
                    case "svg":
                        width = D(reader.GetAttribute("width"));
                        height = D(reader.GetAttribute("height"));
                        if (reader.GetAttribute("viewBox") is { } vb)
                        {
                            var p = vb.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                            if (p.Length == 4)
                            {
                                viewBox = new Rect(D(p[0]), D(p[1]), D(p[2]), D(p[3]));
                            }
                        }

                        break;
                    case "path" when reader.GetAttribute("d") is { } d && Visible(reader):
                        group.Children.Add(StreamGeometry.Parse(d));
                        break;
                    case "rect" when Visible(reader):
                        group.Children.Add(new RectangleGeometry(new Rect(D(reader.GetAttribute("x")), D(reader.GetAttribute("y")), D(reader.GetAttribute("width")), D(reader.GetAttribute("height")))));
                        break;
                    case "circle" when Visible(reader):
                    {
                        var r = D(reader.GetAttribute("r"));
                        group.Children.Add(new EllipseGeometry(new Rect(D(reader.GetAttribute("cx")) - r, D(reader.GetAttribute("cy")) - r, 2 * r, 2 * r)));
                        break;
                    }
                }
            }

            if (group.Children.Count == 0)
            {
                return null;
            }

            var box = viewBox ?? new Rect(0, 0, width > 0 ? width : 16, height > 0 ? height : 16);
            group.Transform = new MatrixTransform(Matrix.CreateTranslation(-box.X, -box.Y));
            return (group, new Rect(0, 0, box.Width, box.Height));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool Visible(XmlReader reader)
    {
        var style = reader.GetAttribute("style") ?? string.Empty;
        var fill = reader.GetAttribute("fill");
        return fill != "none" && !style.Contains("fill:none", StringComparison.Ordinal) && !style.Contains("opacity:0", StringComparison.Ordinal);
    }

    private static double D(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0.0;

    private static string? ConfiguredTheme()
    {
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } c
            ? c
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        var kdeglobals = Path.Combine(config, "kdeglobals");
        if (!File.Exists(kdeglobals))
        {
            return null;
        }

        var inIcons = false;
        foreach (var line in File.ReadLines(kdeglobals))
        {
            var t = line.Trim();
            if (t.StartsWith('['))
            {
                inIcons = t == "[Icons]";
            }
            else if (inIcons && t.StartsWith("Theme=", StringComparison.Ordinal))
            {
                return t["Theme=".Length..];
            }
        }

        return null;
    }
}
