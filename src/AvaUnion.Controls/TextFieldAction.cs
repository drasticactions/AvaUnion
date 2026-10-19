using System;
using System.Windows.Input;
using Avalonia;

namespace AvaUnion.Controls;

/// <summary>An action that an <see cref="ActionTextField"/> shows as a button inside the field.</summary>
public class TextFieldAction : AvaloniaObject
{
    /// <summary>Defines the <see cref="IconName"/> property.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<TextFieldAction, string?>(nameof(IconName));

    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextFieldAction, string?>(nameof(Text));

    /// <summary>Defines the <see cref="Command"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<TextFieldAction, ICommand?>(nameof(Command));

    /// <summary>Defines the <see cref="CommandParameter"/> property.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<TextFieldAction, object?>(nameof(CommandParameter));

    /// <summary>Defines the <see cref="IsVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsVisibleProperty =
        AvaloniaProperty.Register<TextFieldAction, bool>(nameof(IsVisible), true);

    /// <summary>Defines the <see cref="IsEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<TextFieldAction, bool>(nameof(IsEnabled), true);

    /// <summary>The name of the icon of the button.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>The text of the action. The button shows it as a tool tip and as its accessible name.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>The command that the button runs.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The parameter of <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>True when the button shows. The default is true.</summary>
    public bool IsVisible
    {
        get => GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    /// <summary>True when the button can be clicked. The default is true.</summary>
    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    /// <summary>Occurs when the user clicks the button.</summary>
    public event EventHandler? Triggered;

    /// <summary>Raises <see cref="Triggered"/> and runs <see cref="Command"/>.</summary>
    public void Trigger()
    {
        Triggered?.Invoke(this, EventArgs.Empty);
        if (Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }
}
