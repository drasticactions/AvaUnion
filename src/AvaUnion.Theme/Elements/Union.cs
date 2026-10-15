using Avalonia;
using AvaUnion.Elements;
using AvaUnion.Engine;

namespace AvaUnion;

/// <summary>
/// Attached properties that make an element a Union element. The classes of
/// an element are hints.
/// </summary>
public static class Union
{
    /// <summary>
    /// The lowercase Union type, for example <c>button</c>. An empty string
    /// makes an untyped element.
    /// </summary>
    public static readonly AttachedProperty<string?> TypeProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Type", typeof(Union));

    /// <summary>The element id.</summary>
    public static readonly AttachedProperty<string?> IdProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Id", typeof(Union));

    /// <summary>More hints, separated by spaces.</summary>
    public static readonly AttachedProperty<string?> HintsProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Hints", typeof(Union));

    /// <summary>The element attributes, for example <c>display=icon-only;direction=top</c>.</summary>
    public static readonly AttachedProperty<UnionAttributes?> AttributesProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, UnionAttributes?>("Attributes", typeof(Union));

    /// <summary>The source of the element states.</summary>
    public static readonly AttachedProperty<UnionStateSource> StateSourceProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, UnionStateSource>("StateSource", typeof(Union));

    /// <summary>The <c>colorset</c> attribute.</summary>
    public static readonly AttachedProperty<UnionColorSet> ColorSetProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, UnionColorSet>("ColorSet", typeof(Union));

    /// <summary>
    /// The style package for a subtree, for example <c>breeze-mobile</c>. The
    /// value inherits. Null uses the style of the session.
    /// </summary>
    public static readonly AttachedProperty<string?> StyleIdProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("StyleId", typeof(Union), inherits: true);

    /// <summary>The live node of an element that has a <see cref="TypeProperty"/>.</summary>
    public static readonly AttachedProperty<ElementNode?> NodeProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, ElementNode?>("Node", typeof(Union));

    /// <summary>
    /// The style slot of the nearest node at or above the element. The value
    /// inherits. <c>{union:Style}</c> binds to it. When only colors change, the
    /// slot stays the same and raises
    /// <see cref="UnionStyleSlot.ColorsChanged"/>.
    /// </summary>
    public static readonly AttachedProperty<UnionStyleSlot?> StyleProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, UnionStyleSlot?>("Style", typeof(Union), inherits: true);

    /// <summary>Gets <see cref="TypeProperty"/>.</summary>
    public static string? GetType(StyledElement element) => element.GetValue(TypeProperty);

    /// <summary>Sets <see cref="TypeProperty"/>.</summary>
    public static void SetType(StyledElement element, string? value) => element.SetValue(TypeProperty, value);

    /// <summary>Gets <see cref="IdProperty"/>.</summary>
    public static string? GetId(StyledElement element) => element.GetValue(IdProperty);

    /// <summary>Sets <see cref="IdProperty"/>.</summary>
    public static void SetId(StyledElement element, string? value) => element.SetValue(IdProperty, value);

    /// <summary>Gets <see cref="HintsProperty"/>.</summary>
    public static string? GetHints(StyledElement element) => element.GetValue(HintsProperty);

    /// <summary>Sets <see cref="HintsProperty"/>.</summary>
    public static void SetHints(StyledElement element, string? value) => element.SetValue(HintsProperty, value);

    /// <summary>Gets <see cref="AttributesProperty"/>.</summary>
    public static UnionAttributes? GetAttributes(StyledElement element) => element.GetValue(AttributesProperty);

    /// <summary>Sets <see cref="AttributesProperty"/>.</summary>
    public static void SetAttributes(StyledElement element, UnionAttributes? value) => element.SetValue(AttributesProperty, value);

    /// <summary>Gets <see cref="StateSourceProperty"/>.</summary>
    public static UnionStateSource GetStateSource(StyledElement element) => element.GetValue(StateSourceProperty);

    /// <summary>Sets <see cref="StateSourceProperty"/>.</summary>
    public static void SetStateSource(StyledElement element, UnionStateSource value) => element.SetValue(StateSourceProperty, value);

    /// <summary>Gets <see cref="ColorSetProperty"/>.</summary>
    public static UnionColorSet GetColorSet(StyledElement element) => element.GetValue(ColorSetProperty);

    /// <summary>Sets <see cref="ColorSetProperty"/>.</summary>
    public static void SetColorSet(StyledElement element, UnionColorSet value) => element.SetValue(ColorSetProperty, value);

    /// <summary>Gets <see cref="StyleIdProperty"/>.</summary>
    public static string? GetStyleId(StyledElement element) => element.GetValue(StyleIdProperty);

    /// <summary>Sets <see cref="StyleIdProperty"/>.</summary>
    public static void SetStyleId(StyledElement element, string? value) => element.SetValue(StyleIdProperty, value);

    /// <summary>Gets <see cref="NodeProperty"/>.</summary>
    public static ElementNode? GetNode(StyledElement element) => element.GetValue(NodeProperty);

    /// <summary>The current resolved style of the element.</summary>
    public static ResolvedStyle? GetStyle(StyledElement element) => element.GetValue(StyleProperty)?.Current;

    /// <summary>Gets <see cref="StyleProperty"/>.</summary>
    public static UnionStyleSlot? GetStyleSlot(StyledElement element) => element.GetValue(StyleProperty);

    static Union()
    {
        TypeProperty.Changed.AddClassHandler<StyledElement>((e, _) => NodeTracker.Update(e));
        IdProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetNode(e)?.Refresh());
        HintsProperty.Changed.AddClassHandler<StyledElement>((e, _) => RefreshSelfAndPopups(e));
        AttributesProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetNode(e)?.Refresh());
        ColorSetProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetNode(e)?.Refresh());
        StateSourceProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetNode(e)?.Rebind());
        StyleIdProperty.Changed.AddClassHandler<StyledElement>((e, _) => NodeTracker.StyleChanged(e));
        PopupShadowSpace.Register();
        MenuBarHover.Register();
        ButtonMenuPlacement.Register();
        TabWheel.Register();
        TabHeaderText.Register();
        Avalonia.Controls.TreeViewItem.IsExpandedProperty.Changed.AddClassHandler<Avalonia.Controls.TreeViewItem>((e, _) => AutoHints.RefreshTreeRows(e));
        Avalonia.Controls.ItemsControl.ItemCountProperty.Changed.AddClassHandler<Avalonia.Controls.TreeViewItem>((e, _) => AutoHints.RefreshTreeRows(e));
        Avalonia.Controls.ItemsControl.ItemCountProperty.Changed.AddClassHandler<Avalonia.Controls.TabControl>((e, _) => AutoHints.RefreshTabs(e));
        Avalonia.Controls.Button.IsPressedProperty.Changed.AddClassHandler<Avalonia.Controls.Button>((b, _) => ForwardSplitPress(b));
        Avalonia.Controls.Button.IsDefaultProperty.Changed.AddClassHandler<Avalonia.Controls.Button>((b, _) => GetNode(b)?.Refresh());
    }

    private static void ForwardSplitPress(Avalonia.Controls.Button button)
    {
        if (button.Name == "PART_PrimaryButton" && button.TemplatedParent is Avalonia.Controls.SplitButton split)
        {
            Avalonia.Controls.PseudoClassesExtensions.Set((Avalonia.Controls.IPseudoClasses)split.Classes, ":primary-pressed", button.IsPressed);
        }
    }

    private static void RefreshSelfAndPopups(StyledElement e)
    {
        GetNode(e)?.Refresh();
        if (e is Avalonia.Controls.Primitives.Popup popup)
        {
            UnionEnvironment.RefreshPopupRootsOf(popup);
        }
    }

    internal static void EnsureRegistered()
    {
    }
}
