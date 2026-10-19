using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaUnion.Controls;

/// <summary>The header of a column of a <see cref="ColumnTreeView"/>.</summary>
public class ColumnTreeViewHeader : ContentControl
{
    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ColumnTreeViewHeaderAutomationPeer(this);
}
