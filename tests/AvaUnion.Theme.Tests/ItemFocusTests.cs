using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using AvaUnion.Elements;
using AvaUnion.Engine;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ItemFocusTests
{
    [AvaloniaFact]
    public void List_Items_Show_Only_Keyboard_Focus()
    {
        var item = new ListBoxItem();
        ((IPseudoClasses)item.Classes).Set(":focus", true);
        Assert.False(StateMapper.Map(item, "listviewdelegate", new List<string>()).HasFlag(UnionStates.ActiveFocus));

        ((IPseudoClasses)item.Classes).Set(":focus-visible", true);
        Assert.True(StateMapper.Map(item, "listviewdelegate", new List<string>()).HasFlag(UnionStates.ActiveFocus));

        var button = new Button();
        ((IPseudoClasses)button.Classes).Set(":focus", true);
        Assert.True(StateMapper.Map(button, "button", new List<string>()).HasFlag(UnionStates.ActiveFocus));
    }
}
