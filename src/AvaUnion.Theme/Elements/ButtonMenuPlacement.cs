using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaUnion.Elements;

internal static class ButtonMenuPlacement
{
    public static void Register()
    {
        Button.FlyoutProperty.Changed.AddClassHandler<Button>((_, e) => Apply(e.NewValue));
        SplitButton.FlyoutProperty.Changed.AddClassHandler<SplitButton>((_, e) => Apply(e.NewValue));
    }

    private static void Apply(object? flyout)
    {
        if (flyout is PopupFlyoutBase popup && !popup.IsSet(PopupFlyoutBase.PlacementProperty))
        {
            popup.SetCurrentValue(PopupFlyoutBase.PlacementProperty, PlacementMode.BottomEdgeAlignedLeft);
        }
    }
}
