using Avalonia.Interactivity;

namespace AvaUnion.Controls;

/// <summary>The data of <see cref="Dialog.ButtonClicked"/>.</summary>
public class DialogButtonClickedEventArgs : RoutedEventArgs
{
    /// <summary>Creates the data for <paramref name="button"/>.</summary>
    public DialogButtonClickedEventArgs(DialogButtons button)
        : base(Dialog.ButtonClickedEvent) => Button = button;

    /// <summary>The standard button that the user clicked.</summary>
    public DialogButtons Button { get; }
}
