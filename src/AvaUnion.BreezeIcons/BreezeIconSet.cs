using Avalonia;
using AvaUnion.Icons;

namespace AvaUnion.BreezeIcons;

/// <summary>Adds the Breeze icons of this package to the icon chain of AvaUnion.</summary>
public static class BreezeIconSet
{
    private static BreezeIconProvider? s_provider;

    /// <summary>
    /// Adds the Breeze icons after the icon theme of the system and before the
    /// built-in glyphs. A second call does nothing.
    /// </summary>
    public static void Register()
    {
        if (s_provider is null)
        {
            s_provider = new BreezeIconProvider();
            IconProviders.RegisterFallback(s_provider);
        }
    }

    /// <summary>Adds the Breeze icons, as <see cref="Register"/> does.</summary>
    public static AppBuilder WithBreezeIcons(this AppBuilder builder)
    {
        Register();
        return builder;
    }
}
