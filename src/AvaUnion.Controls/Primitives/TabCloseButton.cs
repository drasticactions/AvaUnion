using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AvaUnion.Controls.Primitives;

/// <summary>The close button of a tab when <see cref="ClosableTabs"/> is enabled.</summary>
[PseudoClasses(":pressed")]
public class TabCloseButton : TemplatedControl
{
    /// <summary>Creates the button.</summary>
    public TabCloseButton() => AutomationProperties.SetName(this, "Close");

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            e.Pointer.Capture(this);
            PseudoClasses.Set(":pressed", true);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!PseudoClasses.Contains(":pressed"))
        {
            return;
        }

        PseudoClasses.Set(":pressed", false);
        e.Pointer.Capture(null);
        e.Handled = true;
        if (new Avalonia.Rect(Bounds.Size).Contains(e.GetPosition(this)))
        {
            Close();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PseudoClasses.Set(":pressed", false);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.TabCloseButtonAutomationPeer(this);

    internal void Close()
    {
        if (this.FindAncestorOfType<TabItem>() is { } tab)
        {
            tab.RaiseEvent(new TabCloseRequestedEventArgs(tab));
        }
    }
}
