using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Resources;

namespace AvaUnion;

/// <summary>
/// A theme that uses the Union style, color scheme, fonts, icons and
/// animation speed of the Plasma session. Add it to <c>Application.Styles</c>.
/// The app updates when the session changes them.
/// </summary>
public class UnionTheme : Styles
{
    private readonly UnionResourceProvider _provider = new();
    private VariantFollower? _variantFollower;
    private string? _styleId;
    private string? _colorScheme;
    private UnionEngineKind _engine;
    private bool _initialized;

    /// <summary>Creates the theme.</summary>
    public UnionTheme()
    {
        Resources.MergedDictionaries.Add(_provider);
        Resources.MergedDictionaries.Add(new Themes.ControlThemes());
        Union.EnsureRegistered();
        _provider.OwnerChanged += (_, _) => Initialize();
    }

    /// <summary>Sets a style package, for example <c>breeze</c>. Null follows the <c>unionStyle</c> of the session.</summary>
    public string? StyleId
    {
        get => _styleId;
        set
        {
            _styleId = value;
            UnionEnvironment.ThemeStyleId = value;
            if (UnionEnvironment.IsReady)
            {
                UnionEnvironment.InvalidateAll(styleHandles: true);
            }
        }
    }

    /// <summary>
    /// A color scheme for this application only.
    /// The value is a <c>.colors</c> path or a scheme name, for example
    /// <c>BreezeDark</c>. Null follows the session. The managed engine has
    /// only BreezeLight and BreezeDark.
    /// </summary>
    public string? ColorScheme
    {
        get => _colorScheme;
        set
        {
            _colorScheme = value;
            if (_initialized)
            {
                UnionEnvironment.Engine.SetColorScheme(value);
                _variantFollower?.Apply();
            }
        }
    }

    /// <summary>The engine to use. Changes after the first use have no effect.</summary>
    public UnionEngineKind Engine
    {
        get => _engine;
        set
        {
            _engine = value;
            UnionEnvironment.RequestedEngine = value;
        }
    }

    /// <summary>True when <c>Application.RequestedThemeVariant</c> follows the scheme. This applies to the native engine.</summary>
    public bool FollowThemeVariant { get; set; } = true;

    /// <summary>The engine in use.</summary>
    public UnionEngineKind EngineKind => UnionEnvironment.Engine.Kind;

    /// <summary>The engine in use.</summary>
    public IUnionEngine EngineInstance => UnionEnvironment.Engine;

    private void Initialize()
    {
        if (_initialized || _provider.Owner is null)
        {
            return;
        }

        _initialized = true;
        var engine = UnionEnvironment.Engine;
        if (_colorScheme is not null)
        {
            engine.SetColorScheme(_colorScheme);
        }

        AttachToApplication();
    }

    private void AttachToApplication()
    {
        if (_variantFollower is not null)
        {
            return;
        }

        if (Application.Current is not { } app)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(AttachToApplication);
            return;
        }

        _variantFollower = new VariantFollower(app, () => FollowThemeVariant);
        if (app.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IControlledApplicationLifetime lifetime)
        {
            var engine = UnionEnvironment.Engine;
            lifetime.Exit += (_, _) => (engine as IDisposable)?.Dispose();
        }
    }
}
