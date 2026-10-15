using System.Collections.Generic;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery.Pages;

public sealed record Person(string Name, string City, int Born);

public partial class DataGridPage : UserControl
{
    private static readonly List<Person> s_people =
    [
        new("Ada Lovelace", "London", 1815),
        new("Grace Hopper", "New York", 1906),
        new("Linus Torvalds", "Helsinki", 1969),
        new("Margaret Hamilton", "Paoli", 1936),
        new("Ken Thompson", "New Orleans", 1943),
        new("Barbara Liskov", "Los Angeles", 1939),
        new("Matthias Ettrich", "Bietigheim-Bissingen", 1972),
        new("Frances Allen", "Peru, New York", 1932),
    ];

    public DataGridPage()
    {
        AvaloniaXamlLoader.Load(this);
        var people = this.FindControl<Avalonia.Controls.DataGrid>("People")!;
        people.ItemsSource = new DataGridCollectionView(s_people);
        this.FindControl<CheckBox>("Alternate")!.IsCheckedChanged += (s, _) =>
        {
            people.Classes.Set("no-alternating-colors", ((CheckBox)s!).IsChecked != true);
            people.ItemsSource = new DataGridCollectionView(s_people);
        };

        var grouped = this.FindControl<Avalonia.Controls.DataGrid>("Grouped")!;
        var view = new DataGridCollectionView(s_people);
        view.GroupDescriptions.Add(new CityGroups());
        grouped.ItemsSource = view;
    }

    private sealed class CityGroups : DataGridGroupDescription
    {
        public override string PropertyName => nameof(Person.City);

        public override object GroupKeyFromItem(object item, int level, CultureInfo culture) => ((Person)item).City;
    }
}
