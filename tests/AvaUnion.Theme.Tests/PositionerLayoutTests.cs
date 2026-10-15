using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AvaUnion.Engine;
using AvaUnion.Presenters.Layout;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class PositionerLayoutTests
{
    public static IEnumerable<object[]> Fixtures() =>
        Directory.GetFiles(TestData.Path("positioner"), "*.json").Order().Select(f => new object[] { Path.GetFileNameWithoutExtension(f) });

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Layout_Matches_Union_Fixture(string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestData.Path($"positioner/{name}.json")));
        var input = doc.RootElement.GetProperty("layout");
        var expected = doc.RootElement.GetProperty("expected");

        var layout = new PositionerLayout
        {
            Size = new LayoutSize(Number(input, "width"), Number(input, "height")),
            Spacing = Number(input, "spacing"),
            Padding = Margins(input, "padding"),
            Inset = Margins(input, "inset"),
        };

        foreach (var (key, container) in Containers(layout))
        {
            if (!input.TryGetProperty(key, out var containerData))
            {
                continue;
            }

            foreach (var (bucketKey, bucket) in Buckets(container))
            {
                if (!containerData.TryGetProperty(bucketKey, out var bucketData))
                {
                    continue;
                }

                foreach (var itemData in bucketData.GetProperty("items").EnumerateArray())
                {
                    var item = new LayoutItem
                    {
                        ImplicitSize = new LayoutSize(Number(itemData, "implicitWidth"), Number(itemData, "implicitHeight")),
                        MinimumSize = new LayoutSize(Number(itemData, "minimumWidth"), Number(itemData, "minimumHeight")),
                        Margins = Margins(itemData, "margins"),
                        Order = itemData.TryGetProperty("order", out var o) ? o.GetInt32() : 0,
                        VerticalAlignment = itemData.TryGetProperty("verticalAlignment", out var v) ? ParseAlignment(v.GetString()!) : UnionAlignment.Center,
                    };
                    if (item.VerticalAlignment == UnionAlignment.StackCenter)
                    {
                        bucket.StackCenter = true;
                    }
                    else if (item.VerticalAlignment == UnionAlignment.StackFill)
                    {
                        bucket.StackFill = true;
                    }

                    bucket.Items.Add(item);
                }
            }
        }

        layout.Layout();

        foreach (var (key, container) in Containers(layout))
        {
            if (!expected.TryGetProperty(key, out var e) || e.ValueKind != JsonValueKind.Object || !e.EnumerateObject().Any())
            {
                Assert.True(container.IsEmpty, $"container {key} expected to be empty");
                continue;
            }

            Same(container.Position, Point(e), $"{key} position");
            Same(container.Size, Size(e), $"{key} size");
            foreach (var (bucketKey, bucket) in Buckets(container))
            {
                if (!e.TryGetProperty(bucketKey, out var b) || !b.EnumerateObject().Any())
                {
                    Assert.True(bucket.IsEmpty, $"bucket {bucketKey} of {key} expected to be empty");
                    continue;
                }

                Same(bucket.Position, Point(b), $"{key}.{bucketKey} position");
                Same(bucket.Size, Size(b), $"{key}.{bucketKey} size");
                var items = b.GetProperty("items").EnumerateArray().ToList();
                for (var i = 0; i < bucket.Items.Count; i++)
                {
                    Same(bucket.Items[i].Position, Point(items[i]), $"{key}.{bucketKey} item {i} position");
                    Same(bucket.Items[i].Size, Size(items[i]), $"{key}.{bucketKey} item {i} size");
                }
            }
        }
    }

    private static (string, LayoutContainer)[] Containers(PositionerLayout l) =>
        [("item", l.ItemContainer), ("background", l.BackgroundContainer), ("content", l.ContentContainer)];

    private static (string, LayoutBucket)[] Buckets(LayoutContainer c) => [("start", c.Start), ("center", c.Center), ("end", c.End), ("fill", c.Fill)];

    private static double Number(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0.0;

    private static LayoutSize Point(JsonElement e) => new(Number(e, "x"), Number(e, "y"));

    private static LayoutSize Size(JsonElement e) => new(Number(e, "width"), Number(e, "height"));

    private static LayoutMargins Margins(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v))
        {
            return default;
        }

        if (v.ValueKind == JsonValueKind.Number)
        {
            return LayoutMargins.Uniform(v.GetDouble());
        }

        return v.ValueKind == JsonValueKind.Object ? new LayoutMargins(Number(v, "left"), Number(v, "top"), Number(v, "right"), Number(v, "bottom")) : default;
    }

    private static UnionAlignment ParseAlignment(string value)
    {
        var name = value.Replace("-", string.Empty, StringComparison.Ordinal);
        foreach (var a in Enum.GetValues<UnionAlignment>())
        {
            if (a.ToString().StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                return a;
            }
        }

        return (UnionAlignment)(-1);
    }

    private static void Same(LayoutSize actual, LayoutSize expected, string what)
    {
        Assert.True(Math.Abs(actual.Width - expected.Width) < 1e-9 && Math.Abs(actual.Height - expected.Height) < 1e-9,
            $"{what} is ({actual.Width}, {actual.Height}), expected ({expected.Width}, {expected.Height})");
    }
}
