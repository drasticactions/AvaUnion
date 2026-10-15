using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace AvaUnion.Engine;

/// <summary>The metadata of a style package, from its <c>metadata.json</c>.</summary>
/// <param name="Id">The style id. This is the name of the package folder.</param>
/// <param name="Name">The display name for the current UI culture, or null.</param>
/// <param name="Description">The description for the current UI culture, or null.</param>
/// <param name="Version">The package version, or null.</param>
/// <param name="License">The license, or null.</param>
/// <param name="Url">The home page of the package, or null.</param>
/// <param name="Authors">The authors.</param>
/// <param name="IsHidden">True when the package is not shown in style listings.</param>
/// <param name="Origin">Where the style comes from.</param>
public sealed record UnionStyleMetadata(
    string Id,
    string? Name,
    string? Description,
    string? Version,
    string? License,
    Uri? Url,
    IReadOnlyList<string> Authors,
    bool IsHidden,
    UnionStyleOrigin Origin)
{
    /// <summary>
    /// Reads the text of a <c>metadata.json</c> file. The name and the
    /// description come from the <c>name[xx_YY]</c> entry for
    /// <see cref="CultureInfo.CurrentUICulture"/>. If there is no such entry,
    /// they come from the entry without a suffix. If the text is not valid
    /// JSON, the metadata is empty.
    /// </summary>
    public static UnionStyleMetadata Parse(string id, string? json, UnionStyleOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(id);
        var empty = new UnionStyleMetadata(id, null, null, null, null, null, [], false, origin);
        if (string.IsNullOrWhiteSpace(json))
        {
            return empty;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return empty;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return empty;
            }

            var locale = CultureInfo.CurrentUICulture.Name.Replace('-', '_');
            var authors = new List<string>();
            if (root.TryGetProperty("authors", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var author in list.EnumerateArray())
                {
                    if (author.ValueKind == JsonValueKind.String)
                    {
                        authors.Add(author.GetString()!);
                    }
                }
            }

            var url = Text(root, "url") is { Length: > 0 } link && Uri.TryCreate(link, UriKind.Absolute, out var parsed) ? parsed : null;
            var hidden = root.TryGetProperty("hidden", out var h) && h.ValueKind == JsonValueKind.True;
            return new UnionStyleMetadata(
                id,
                Localized(root, "name", locale),
                Localized(root, "description", locale),
                Text(root, "version"),
                Text(root, "license"),
                url,
                authors,
                hidden,
                origin);
        }
    }

    private static string? Localized(JsonElement root, string key, string locale) =>
        (locale.Length > 0 ? Text(root, $"{key}[{locale}]") : null) ?? Text(root, key);

    private static string? Text(JsonElement root, string key) =>
        root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
