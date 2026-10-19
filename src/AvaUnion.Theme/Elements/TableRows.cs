using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace AvaUnion.Elements;

internal static class TableRows
{
    private static readonly ConditionalWeakTable<TableView, object> s_watched = [];

    public static void Register()
    {
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TableView>((table, _) =>
        {
            if (s_watched.TryGetValue(table, out var seen))
            {
                return;
            }

            s_watched.Add(table, table);
            table.ContainerPrepared += (_, e) => RefreshLater(e.Container);
            table.ContainerIndexChanged += (_, e) => RefreshLater(e.Container);
            table.Classes.CollectionChanged += (_, _) =>
            {
                foreach (var row in table.GetRealizedContainers())
                {
                    Union.GetNode(row)?.Refresh();
                }
            };
        });
    }

    private static void RefreshLater(Control row) =>
        Dispatcher.UIThread.Post(() => Union.GetNode(row)?.Refresh(), DispatcherPriority.Loaded);
}
