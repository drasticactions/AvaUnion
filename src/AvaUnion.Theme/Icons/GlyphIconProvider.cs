using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Icons;

/// <summary>Vector glyphs for the icon names that the Breeze CSS uses.</summary>
public sealed class GlyphIconProvider : IIconProvider
{
    private const string Down = "M3.5,5.5 L8,10 L12.5,5.5 L13.5,6.5 L8,12 L2.5,6.5 Z";
    private const string Up = "M3.5,10.5 L8,6 L12.5,10.5 L13.5,9.5 L8,4 L2.5,9.5 Z";
    private const string Left = "M10.5,3.5 L6,8 L10.5,12.5 L9.5,13.5 L4,8 L9.5,2.5 Z";
    private const string Right = "M5.5,3.5 L10,8 L5.5,12.5 L6.5,13.5 L12,8 L6.5,2.5 Z";
    private const string Cross = "M4,4.7 L4.7,4 L8,7.3 L11.3,4 L12,4.7 L8.7,8 L12,11.3 L11.3,12 L8,8.7 L4.7,12 L4,11.3 L7.3,8 Z";
    private const string Check = "M3,8.5 L6.5,12 L13,4.5 L12,3.5 L6.5,10 L4,7.5 Z";
    private const string Calendar = "M2,3 L14,3 L14,14 L2,14 Z M3,6 L3,13 L13,13 L13,6 Z M5,1.5 L6,1.5 L6,4 L5,4 Z M10,1.5 L11,1.5 L11,4 L10,4 Z M5,8 L7,8 L7,10 L5,10 Z";
    private const string Plus = "M7.5,3 L8.5,3 L8.5,7.5 L13,7.5 L13,8.5 L8.5,8.5 L8.5,13 L7.5,13 L7.5,8.5 L3,8.5 L3,7.5 L7.5,7.5 Z";
    private const string Minus = "M3,7.5 L13,7.5 L13,8.5 L3,8.5 Z";
    private const string Diamond = "M8,2.6 L13.4,8 L8,13.4 L2.6,8 Z M8,4 L4,8 L8,12 L12,8 Z";
    private const string Menu = "M2,3 L14,3 L14,4 L2,4 Z M2,7.5 L14,7.5 L14,8.5 L2,8.5 Z M2,12 L14,12 L14,13 L2,13 Z";
    private const string Search = "M6.5,2 A4.5,4.5 0 1 1 6.5,11 A4.5,4.5 0 1 1 6.5,2 Z M6.5,3 A3.5,3.5 0 1 0 6.5,10 A3.5,3.5 0 1 0 6.5,3 Z M9.6,10.3 L10.3,9.6 L14,13.3 L13.3,14 Z";
    private const string Help = "M8,1.5 A6.5,6.5 0 1 1 8,14.5 A6.5,6.5 0 1 1 8,1.5 Z M8,2.5 A5.5,5.5 0 1 0 8,13.5 A5.5,5.5 0 1 0 8,2.5 Z M7.5,10 L8.5,10 L8.5,11 L7.5,11 Z M5.5,6.2 C5.5,4.8 6.6,4 8,4 C9.4,4 10.5,4.8 10.5,6 C10.5,7.6 8.5,7.7 8.5,9 L7.5,9 C7.5,7.2 9.5,7 9.5,6 C9.5,5.4 8.9,5 8,5 C7.1,5 6.5,5.5 6.5,6.2 Z";
    private const string Working = "M8,2 A6,6 0 0 1 14,8 L13,8 A5,5 0 0 0 8,3 Z M8,14 A6,6 0 0 1 2,8 L3,8 A5,5 0 0 0 8,13 Z";
    private const string Shade = "M3.5,9.5 L8,5 L12.5,9.5 L13.5,8.5 L8,3 L2.5,8.5 Z M3,12 L13,12 L13,13 L3,13 Z";

    private const string Folder = "M1.5,3 L6,3 L7.5,4.5 L14.5,4.5 L14.5,13 L1.5,13 Z M2.5,6 L2.5,12 L13.5,12 L13.5,6 Z";
    private const string File = "M3,1.5 L9.5,1.5 L13,5 L13,14.5 L3,14.5 Z M4,2.5 L4,13.5 L12,13.5 L12,6 L8.5,6 L8.5,2.5 Z";
    private const string Drive = "M2,9 L14,9 L14,13 L2,13 Z M3,10 L3,12 L13,12 L13,10 Z M3.5,4 L12.5,4 L14,9 L13,9 L11.8,5 L4.2,5 L3,9 L2,9 Z M10.5,10.5 L12,10.5 L12,11.5 L10.5,11.5 Z";
    private const string Refresh = "M8,2.5 A5.5,5.5 0 1 1 2.5,8 L3.5,8 A4.5,4.5 0 1 0 8,3.5 L8,5.5 L5,3 L8,0.5 Z";

    private static readonly Dictionary<string, string> s_glyphs = new()
    {
        ["folder"] = Folder,
        ["emblem-success"] = Check,
        ["emblem-error"] = Cross,
        ["emblem-information"] = Help,
        ["emblem-warning"] = Help,
        ["text-x-generic"] = File,
        ["drive-harddisk"] = Drive,
        ["view-refresh"] = Refresh,
        ["application-menu-symbolic"] = Menu,
        ["arrow-down"] = Down,
        ["arrow-down-symbolic"] = Down,
        ["arrow-up"] = Up,
        ["arrow-up-symbolic"] = Up,
        ["arrow-left"] = Left,
        ["arrow-left-symbolic"] = Left,
        ["go-previous"] = Left,
        ["go-previous-symbolic"] = Left,
        ["go-next"] = Right,
        ["go-next-symbolic"] = Right,
        ["go-up"] = Up,
        ["view-fullscreen"] = Up,
        ["view-restore"] = Down,
        ["overflow-menu"] = Menu,
        ["go-down"] = Down,
        ["arrow-right"] = Right,
        ["arrow-right-symbolic"] = Right,
        ["edit-clear"] = Cross,
        ["view-calendar"] = Calendar,
        ["view-calendar-symbolic"] = Calendar,
        ["dialog-cancel"] = Cross,
        ["dialog-ok-apply"] = Check,
        ["dialog-ok"] = Check,
        ["edit-delete-remove"] = Cross,
        ["empty"] = string.Empty,
        ["help-contextual-symbolic"] = Help,
        ["process-working-symbolic"] = Working,
        ["search"] = Search,
        ["spinbox-increase"] = Plus,
        ["spinbox-decrease"] = Minus,
        ["window-close-symbolic"] = Cross,
        ["window-maximize-symbolic"] = Up,
        ["window-minimize-symbolic"] = Down,
        ["window-restore-symbolic"] = Diamond,
        ["window-restore"] = Diamond,
        ["window-shade-symbolic"] = Shade,
    };

    /// <summary>The names this provider knows.</summary>
    public static IReadOnlyCollection<string> Names => s_glyphs.Keys;

    /// <inheritdoc/>
    public IImage? GetIcon(string name, Color? tint, Size size, double scale)
    {
        if (!s_glyphs.TryGetValue(name, out var data))
        {
            return null;
        }

        Geometry geometry = data.Length == 0 ? new RectangleGeometry(new Rect(0, 0, 16, 16)) : StreamGeometry.Parse(data);
        IBrush brush = data.Length == 0 ? Brushes.Transparent : new SolidColorBrush(tint ?? Colors.Black);
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing { Geometry = new RectangleGeometry(new Rect(0, 0, 16, 16)), Brush = Brushes.Transparent });
        group.Children.Add(new GeometryDrawing { Geometry = geometry, Brush = brush });
        return new DrawingImage(group);
    }
}
