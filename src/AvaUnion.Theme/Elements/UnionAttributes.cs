using System;
using System.Collections.Generic;
using System.Globalization;

namespace AvaUnion.Elements;

/// <summary>
/// Element attributes, written as <c>display=icon-only;direction=top</c>.
/// A value that parses as a number is a number. Other values are strings.
/// </summary>
public sealed class UnionAttributes
{
    /// <summary>No attributes.</summary>
    public static readonly UnionAttributes Empty = new([]);

    /// <summary>Creates attributes from pairs.</summary>
    public UnionAttributes(IReadOnlyList<KeyValuePair<string, object>> values)
    {
        Values = values;
    }

    /// <summary>The attributes, in the order of the text.</summary>
    public IReadOnlyList<KeyValuePair<string, object>> Values { get; }

    /// <summary>Parses <c>name=value;name=value</c>.</summary>
    public static UnionAttributes Parse(string text)
    {
        var values = new List<KeyValuePair<string, object>>();
        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
            {
                throw new FormatException($"'{part}' is not name=value");
            }

            var name = part[..eq].Trim();
            var raw = part[(eq + 1)..].Trim();
            object value = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : raw;
            values.Add(new KeyValuePair<string, object>(name, value));
        }

        return new UnionAttributes(values);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new List<string>(Values.Count);
        foreach (var (key, value) in Values)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{key}={value}"));
        }

        return string.Join(";", parts);
    }
}
