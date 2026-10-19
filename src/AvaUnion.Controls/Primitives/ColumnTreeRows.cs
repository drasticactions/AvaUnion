using Avalonia.Controls;

namespace AvaUnion.Controls.Primitives;

internal sealed class ColumnTreeRows : ItemsControl
{
    protected override System.Type StyleKeyOverride => typeof(ItemsControl);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new ColumnTreeViewRow();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<ColumnTreeViewRow>(item, out recycleKey);

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is ColumnTreeViewRow row && item is ColumnTreeNode node)
        {
            row.Attach(node);
        }
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        if (container is ColumnTreeViewRow row)
        {
            row.Detach();
        }

        base.ClearContainerForItemOverride(container);
    }
}
