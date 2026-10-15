using Avalonia.Data.Converters;

namespace AvaUnion.Markup;

internal static class FileChooserConverters
{
    public static readonly IValueConverter IconName = new FuncValueConverter<string?, string>(key => key switch
    {
        "Icon_Folder" => "folder",
        "Icon_Volume" => "drive-harddisk",
        _ => "text-x-generic",
    });
}
