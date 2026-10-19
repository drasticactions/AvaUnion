using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaUnion.Controls.Primitives;

/// <summary>The header of a table row when <see cref="TableRowHeaders"/> is visible. It shows the row number.</summary>
public class TableRowHeader : ContentControl
{
    private TableView? _table;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _table = this.FindAncestorOfType<TableView>();
        if (_table is not null)
        {
            _table.ContainerPrepared += OnContainerChanged;
            _table.ContainerIndexChanged += OnContainerChanged;
        }

        Update();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_table is not null)
        {
            _table.ContainerPrepared -= OnContainerChanged;
            _table.ContainerIndexChanged -= OnContainerChanged;
            _table = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new TableRowHeaderAutomationPeer(this);

    private void OnContainerChanged(object? sender, System.EventArgs e) => Update();

    private void Update()
    {
        if (_table is not null && this.FindAncestorOfType<TableViewRow>() is { } row && _table.IndexFromContainer(row) is var index and >= 0)
        {
            Content = (index + 1).ToString(CultureInfo.CurrentCulture);
        }
    }

    private sealed class TableRowHeaderAutomationPeer(TableRowHeader owner) : ContentControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.HeaderItem;
    }
}
