using Avalonia;

namespace AvaUnion;

/// <summary>App builder extensions.</summary>
public static class AppBuilderExtensions
{
    /// <summary>Sets the options for the process.</summary>
    public static AppBuilder WithAvaUnion(this AppBuilder builder, AvaUnionOptions? options = null)
    {
        Engine.EngineSelector.Options = options ?? new AvaUnionOptions();
        return builder;
    }
}
