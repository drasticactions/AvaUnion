using System;
using System.Collections.Generic;
using System.Globalization;
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
    private const string Prefix = "breeze/";

    private readonly Dictionary<string, SortedDictionary<int, string>> _resources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgIcon?> _icons = new(StringComparer.Ordinal);

    /// <summary>Creates the provider.</summary>
    public BreezeIconProvider()
    {
        foreach (var resource in typeof(BreezeIconProvider).Assembly.GetManifestResourceNames())
        {
            var path = resource.Replace('\\', '/');
            if (!path.StartsWith(Prefix, StringComparison.Ordinal) || !path.EndsWith(".svg", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = path[Prefix.Length..^".svg".Length].Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var size))
            {
                continue;
            }

            if (!_resources.TryGetValue(parts[1], out var sizes))
            {
                _resources[parts[1]] = sizes = [];
            }

            sizes[size] = resource;
        }
    }

    /// <summary>The icon names that the provider has.</summary>
    public IEnumerable<string> Names => _resources.Keys;

    /// <inheritdoc/>
    public IImage? GetIcon(string name, Color? tint, Size size, double scale)
    {
        if (!_resources.TryGetValue(name, out var sizes))
        {
            return null;
        }

        var wanted = Math.Max(size.Width, size.Height);
        string? resource = null;
        foreach (var (available, candidate) in sizes)
        {
            resource = candidate;
            if (available >= wanted)
            {
                break;
            }
        }

        if (resource is null)
        {
            return null;
        }

        if (!_icons.TryGetValue(resource, out var icon))
        {
            using var stream = typeof(BreezeIconProvider).Assembly.GetManifestResourceStream(resource);
            icon = stream is null ? null : SvgIcon.Read(stream);
            _icons[resource] = icon;
        }

        return icon?.ToImage(tint);
    }
}
