using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using AvaUnion.Controls;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery.Pages;

public sealed record Dragon(string Type, string Wings, string FrontLegs, string HindLegs);

public sealed record FileEntry(FileSystemInfo Info, string Name, string Size, string Type, string Modified)
{
    public static FileEntry From(FileSystemInfo info, string? name = null, string? type = null) => new(
        info,
        name ?? info.Name,
        info is FileInfo file ? FormatSize(file.Length) : string.Empty,
        type ?? (info is DirectoryInfo ? "Folder" : info.Extension.TrimStart('.') + " File"),
        info.LastWriteTime.ToString("g", CultureInfo.CurrentCulture));

    public IEnumerable? Children()
    {
        if (Info is not DirectoryInfo directory)
        {
            return null;
        }

        IEnumerable<FileEntry> Read()
        {
            FileSystemInfo[] entries;
            try
            {
                entries = directory.GetFileSystemInfos();
            }
            catch (Exception)
            {
                yield break;
            }

            foreach (var entry in entries.OrderBy(e => e is FileInfo).ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                yield return From(entry);
            }
        }

        return Read();
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} bytes",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KiB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MiB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):0.#} GiB",
    };
}

public partial class WidgetsListPage : UserControl
{
    private static readonly Dragon[] s_dragons =
    [
        new("Western Dragon", "Yes", "Yes", "Yes"),
        new("Eastern Dragon", "No", "Yes", "Yes"),
        new("Wyvern", "Yes", "No", "Yes"),
        new("Wyrm", "No", "No", "No"),
        new("Lindworm", "No", "Yes", "No"),
        new("Amphiptere", "Yes", "No", "No"),
    ];

    public WidgetsListPage()
    {
        AvaloniaXamlLoader.Load(this);
        var files = this.FindControl<ColumnTreeView>("Files")!;
        files.ChildrenSelector = item => ((FileEntry)item).Children();
        try
        {
            var root = new DirectoryInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "/");
            files.ItemsSource = new[] { FileEntry.From(root, root.FullName, "Drive") };
        }
        catch (Exception)
        {
        }

        foreach (var name in new[] { "Table", "TableNoGrid" })
        {
            var grid = this.FindControl<Avalonia.Controls.DataGrid>(name)!;
            grid.ItemsSource = s_dragons;
            grid.LoadingRow += (_, e) => e.Row.Header = e.Row.Index + 1;
        }
    }
}
