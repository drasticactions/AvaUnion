using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
using AvaUnion.Engine;

namespace AvaUnion.Elements;

/// <summary>
/// Maps Avalonia pseudo-classes to <c>Union::Element</c> states.
/// It also adds hints for the states that Breeze uses but that have no Union state bit.
/// </summary>
public static class StateMapper
{
    /// <summary>Gets the element that supplies the states of <paramref name="element"/>. It is null when the element has no states.</summary>
    public static StyledElement? StateSourceOf(StyledElement element)
    {
        return element.GetValue(Union.StateSourceProperty) switch
        {
            UnionStateSource.Self => element,
            UnionStateSource.TemplatedParent => element.TemplatedParent as StyledElement,
            UnionStateSource.None => null,
            _ => element is TemplatedControl || element.TemplatedParent is not StyledElement ? element : element.TemplatedParent as StyledElement,
        };
    }

    /// <summary>Gets the states of <paramref name="source"/>. It adds the hints for states to <paramref name="hints"/>.</summary>
    public static UnionStates Map(StyledElement? source, string type, List<string> hints)
    {
        if (source is null)
        {
            return UnionStates.None;
        }

        var states = UnionStates.None;
        var classes = source.Classes;
        foreach (var c in classes)
        {
            if (c.Length == 0 || c[0] != ':')
            {
                continue;
            }

            if (UnionHintProviders.HintFor(source, c) is { } mapped)
            {
                Add(hints, mapped);
                continue;
            }

            switch (c)
            {
                case ":pointerover":
                    states |= UnionStates.Hovered;
                    break;
                case ":focus" when source is ListBoxItem or TreeViewItem or ComboBoxItem:
                    break;
                case ":focus":
                    states |= UnionStates.ActiveFocus;
                    break;
                case ":focus-visible":
                    states |= UnionStates.VisualFocus | UnionStates.ActiveFocus;
                    break;
                case ":focus-within" when type is "spinbox" or "combobox":
                    states |= UnionStates.ActiveFocus;
                    break;
                case ":pressed":
                    states |= UnionStates.Pressed;
                    break;
                case ":checked" when source is ToggleButton { TemplatedParent: Expander }:
                    Add(hints, "expanded");
                    break;
                case ":checked":
                    states |= UnionStates.Checked;
                    break;
                case ":disabled":
                    states |= UnionStates.Disabled;
                    break;
                case ":selected" when source is TreeViewItem or TableViewRow:
                    Add(hints, "selected");
                    break;
                case ":selected" when source is TabItem or TabStripItem:
                    states |= UnionStates.Checked;
                    break;
                case ":selected":
                    states |= UnionStates.Highlighted;
                    break;
                case ":indeterminate":
                    Add(hints, "indeterminate");
                    break;
                case ":dropdownopen" when source is ComboBox:
                    states |= UnionStates.Pressed;
                    break;
                case ":flyout-open" or ":pressed" or ":primary-pressed" when source is SplitButton:
                    break;
                case ":flyout-open":
                    break;
                case ":open" when source is MenuItem:
                    Add(hints, "menu-open");
                    if (type == "menubaritem")
                    {
                        states |= UnionStates.Pressed;
                    }

                    break;
                case ":today":
                    Add(hints, "today");
                    break;
                case ":expanded":
                    Add(hints, "expanded");
                    break;
                case ":maximized":
                    Add(hints, "maximized");
                    break;
                case ":visited":
                    Add(hints, "visited");
                    break;
            }
        }

        if (source is Button { IsDefault: true })
        {
            states |= UnionStates.Highlighted;
        }

        if (source is SplitButton split)
        {
            states |= SplitStates(split, hints);
        }

        if (source is InputElement { IsEffectivelyEnabled: false })
        {
            states |= UnionStates.Disabled;
        }

        return states;
    }

    private static UnionStates SplitStates(SplitButton split, List<string> hints)
    {
        var classes = split.Classes;
        var down = classes.Contains(":pressed") || classes.Contains(":primary-pressed");
        var menuOpen = classes.Contains(":flyout-open");
        var isChecked = classes.Contains(":checked");
        var sunken = down || menuOpen;
        var raised = !isChecked && !down;
        if (sunken && raised)
        {
            Add(hints, "menu-open");
        }

        return sunken && !raised ? UnionStates.Pressed : UnionStates.None;
    }

    private static void Add(List<string> hints, string hint)
    {
        if (!hints.Contains(hint))
        {
            hints.Add(hint);
        }
    }
}
