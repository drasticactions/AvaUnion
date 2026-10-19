using System;
using Avalonia.Interactivity;

namespace AvaUnion.Controls;

/// <summary>The data of <see cref="MonthGrid.DateClicked"/>.</summary>
public class DateClickedEventArgs : RoutedEventArgs
{
    /// <summary>Creates the data for <paramref name="date"/>.</summary>
    public DateClickedEventArgs(RoutedEvent routedEvent, DateOnly date)
        : base(routedEvent) => Date = date;

    /// <summary>The date that the user clicked.</summary>
    public DateOnly Date { get; }
}
