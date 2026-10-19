using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaUnion.Elements;
using AvaUnion.Engine;

namespace AvaUnion.Icons;

/// <summary>
/// The chain of icon providers. App providers are first. Then come the native
/// engine icons or the freedesktop icon theme, the fallback providers, and the
/// built-in glyphs.
/// </summary>
public static class IconProviders
{
    private static readonly List<IIconProvider> s_custom = [];
    private static readonly List<IIconProvider> s_fallbacks = [];
    private static readonly Dictionary<string, IImage?> s_cache = [];
    private static IIconProvider[]? s_builtIn;

    static IconProviders()
    {
        UnionEnvironment.Changed += change =>
        {
            if ((change & (UnionChange.Icons | UnionChange.Colors)) != 0)
            {
                s_cache.Clear();
                Presenters.UnionImageCache.Clear();
                Invalidated?.Invoke();
            }
        };
    }

    internal static event Action? Invalidated;

    /// <summary>Puts a provider in front of the built-in ones.</summary>
    public static void Register(IIconProvider provider)
    {
        s_custom.Insert(0, provider);
        s_cache.Clear();
        Invalidated?.Invoke();
    }

    /// <summary>
    /// Adds a provider after the icon theme of the system and before the
    /// built-in glyphs, for example a bundled icon set. Fallbacks added first
    /// are asked first.
    /// </summary>
    public static void RegisterFallback(IIconProvider provider)
    {
        if (s_fallbacks.Contains(provider))
        {
            return;
        }

        s_fallbacks.Add(provider);
        s_builtIn = null;
        s_cache.Clear();
        Invalidated?.Invoke();
    }

    /// <summary>Finds an icon through the chain.</summary>
    public static IImage? Get(string name, Color? tint, Size size, double scale)
    {
        var key = $"{name}|{size.Width}x{size.Height}|{scale}|{tint?.ToUInt32()}";
        if (s_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        IImage? result = null;
        foreach (var provider in s_custom)
        {
            result = provider.GetIcon(name, tint, size, scale);
            if (result is not null)
            {
                break;
            }
        }

        if (result is null)
        {
            foreach (var provider in BuiltIn())
            {
                result = provider.GetIcon(name, tint, size, scale);
                if (result is not null)
                {
                    break;
                }
            }
        }

        s_cache[key] = result;
        return result;
    }

    private static IIconProvider[] BuiltIn()
    {
        if (s_builtIn is not null)
        {
            return s_builtIn;
        }

        var list = new List<IIconProvider>();
        if (UnionEnvironment.Engine.Kind == UnionEngineKind.Native)
        {
            list.Add(new NativeIconProvider());
        }
        else if (System.OperatingSystem.IsLinux())
        {
            list.Add(new FreedesktopIconProvider());
        }

        list.AddRange(s_fallbacks);
        list.Add(new GlyphIconProvider());
        return s_builtIn = [.. list];
    }
}
