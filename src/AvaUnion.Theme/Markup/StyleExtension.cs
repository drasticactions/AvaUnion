using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

namespace AvaUnion.Markup;

/// <summary>
/// <c>{union:Style Text.Color}</c> binds to a style value of the nearest
/// Union element at or above the target. You can use it in templates and in
/// ControlTheme setters.
/// </summary>
public sealed class StyleExtension : MarkupExtension
{
    /// <summary>Creates the extension.</summary>
    public StyleExtension()
    {
    }

    /// <summary>Creates the extension for a path.</summary>
    public StyleExtension(string path)
    {
        Path = path;
    }

    /// <summary>A dotted <see cref="Engine.ResolvedStyle"/> path, for example <c>Layout.Padding</c>.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The value to use when the leaf is not set.</summary>
    public object? Fallback { get; set; }

    /// <summary>The sides that a length applies to when it binds to a thickness, for example <c>Left</c>. <c>-Left</c> negates the length.</summary>
    public string? Sides { get; set; }

    /// <summary>
    /// A color scheme color to use when the leaf is not set. The format is
    /// <c>[Group.]Set.Kind.Role</c>, for example <c>View.Foreground.Text</c> or
    /// <c>Disabled.View.Foreground.Text</c>.
    /// </summary>
    public string? FallbackColor { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider) => Create(Path, Fallback, Sides, FallbackColor);

    /// <summary>Builds the binding for <c>{union:Style}</c>.</summary>
    public static CompiledBinding Create(string path, object? fallback = null, string? sides = null, string? fallbackColor = null)
    {
        var bindingPath = new CompiledBindingPathBuilder()
            .Self()
            .Property(Union.StyleProperty, StyleAccessor.For(StylePaths.Get(path)))
            .Build();
        return new CompiledBinding(bindingPath)
        {
            Mode = BindingMode.OneWay,
            Converter = new StyleValueConverter(path, sides, fallback as string, fallbackColor),
            FallbackValue = fallback is null or string ? AvaloniaProperty.UnsetValue : fallback,
        };
    }
}
