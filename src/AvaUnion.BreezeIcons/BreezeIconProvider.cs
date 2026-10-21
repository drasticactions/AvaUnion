using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaUnion.Icons;

namespace AvaUnion.BreezeIcons;

/// <summary>
/// Finds icons in the Breeze icons that this package holds. It uses the
/// smallest size that is at least the size asked for, or else the largest
/// size. The parts in the text color take the tint.
/// </summary>
public sealed class BreezeIconProvider : IIconProvider
{
    private readonly Lazy<BreezeArchive> _archive = new(BreezeArchive.Load);
    private readonly Dictionary<(int Blob, bool Pad), SvgIcon?> _icons = [];

    /// <summary>Creates the provider.</summary>
    public BreezeIconProvider()
    {
    }

    /// <summary>The icon names that the provider has.</summary>
    public IEnumerable<string> Names => _archive.Value.Icons.Keys;

    /// <inheritdoc/>
    public IImage? GetIcon(string name, Color? tint, Size size, double scale)
    {
        if (!_archive.Value.Icons.TryGetValue(name, out var sizes))
        {
            return null;
        }

        var wanted = Math.Max(size.Width, size.Height);
        (int Blob, bool Pad)? entry = null;
        foreach (var (available, candidate) in sizes)
        {
            entry = candidate;
            if (available >= wanted)
            {
                break;
            }
        }

        if (entry is not { } found)
        {
            return null;
        }

        if (!_icons.TryGetValue(found, out var icon))
        {
            using var stream = _archive.Value.Read(found.Blob);
            icon = SvgIcon.Read(stream);
            if (found.Pad)
            {
                icon = icon?.Padded(1);
            }

            _icons[found] = icon;
        }

        return icon?.ToImage(tint);
    }
}
