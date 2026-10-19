using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaUnion.Controls;

/// <summary>The data of <see cref="ClosableTabs.CloseRequestedEvent"/>.</summary>
public class TabCloseRequestedEventArgs : RoutedEventArgs
{
    /// <summary>Creates the data for <paramref name="tab"/>.</summary>
    public TabCloseRequestedEventArgs(TabItem tab)
        : base(ClosableTabs.CloseRequestedEvent) => Tab = tab;

    /// <summary>The tab that the user wants to close.</summary>
    public TabItem Tab { get; }
}
