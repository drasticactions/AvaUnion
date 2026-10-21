using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaUnion.Icons;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class SvgCorpusTests
{
    [AvaloniaFact]
    public void Corpus_Matches_The_Reference_Renderer()
    {
        if (Environment.GetEnvironmentVariable("AVAUNION_SVG_CORPUS") is not { Length: > 0 } corpus)
        {
            return;
        }

        var scale = int.Parse(Environment.GetEnvironmentVariable("AVAUNION_SVG_SCALE") ?? "4", CultureInfo.InvariantCulture);
        var limit = double.Parse(Environment.GetEnvironmentVariable("AVAUNION_SVG_LIMIT") ?? "0.02", CultureInfo.InvariantCulture);
        var diffs = Path.Combine(corpus, "diff");
        if (Directory.Exists(diffs))
        {
            Directory.Delete(diffs, true);
        }

        Directory.CreateDirectory(diffs);
        var results = new List<(double Score, string Name)>();
        foreach (var svg in Directory.EnumerateFiles(Path.Combine(corpus, "svg"), "*.svg", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(Path.Combine(corpus, "svg"), svg);
            var reference = Path.Combine(corpus, "ref", Path.ChangeExtension(relative, ".png"));
            if (!File.Exists(reference))
            {
                continue;
            }

            using var expected = new Bitmap(reference);
            var size = expected.PixelSize;
            var icon = SvgIcon.Read(svg);
            var expectedPixels = Pixels(size, c => c.DrawImage(expected, new Rect(0, 0, size.Width, size.Height)));
            var actualPixels = icon is null ? new byte[expectedPixels.Length] : Pixels(size, c => c.DrawImage(icon.ToImage(null), new Rect(0, 0, size.Width, size.Height)));
            var score = Score(expectedPixels, actualPixels, size.Width);
            results.Add((score, relative));
            if (score > limit)
            {
                using var target = new RenderTargetBitmap(new PixelSize(size.Width * 2 + 4, size.Height));
                using (var context = target.CreateDrawingContext())
                {
                    context.FillRectangle(Brushes.White, new Rect(0, 0, size.Width * 2 + 4, size.Height));
                    context.DrawImage(expected, new Rect(0, 0, size.Width, size.Height));
                    if (icon is not null)
                    {
                        using (context.PushTransform(Matrix.CreateTranslation(size.Width + 4, 0)))
                        {
                            context.DrawImage(icon.ToImage(null), new Rect(0, 0, size.Width, size.Height));
                        }
                    }
                }

                target.Save(Path.Combine(diffs, $"{score:0.000}_{relative.Replace('/', '_')}.png"), new PngBitmapEncoderOptions());
            }
        }

        File.WriteAllLines(Path.Combine(corpus, "report.tsv"), results.OrderByDescending(r => r.Score).Select(r => $"{r.Score:0.0000}\t{r.Name}"));
        var failing = results.Count(r => r.Score > limit);
        File.WriteAllText(Path.Combine(corpus, "summary.txt"), $"{results.Count} icons, {failing} over {limit}\n");
    }

    private static byte[] Pixels(PixelSize size, Action<DrawingContext> draw)
    {
        using var target = new RenderTargetBitmap(size);
        using (var context = target.CreateDrawingContext())
        {
            draw(context);
        }

        var data = new byte[size.Width * size.Height * 4];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            target.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), data.Length, size.Width * 4);
        }
        finally
        {
            handle.Free();
        }

        return data;
    }

    private static double Score(byte[] expected, byte[] actual, int width)
    {
        var height = expected.Length / 4 / width;
        int bad = 0, inked = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = ((y * width) + x) * 4;
                if (expected[i + 3] > 0 || actual[i + 3] > 0)
                {
                    inked++;
                }

                if (!Matches(expected, i, actual, x, y, width, height) || !Matches(actual, i, expected, x, y, width, height))
                {
                    bad++;
                }
            }
        }

        return inked == 0 ? 0 : (double)bad / inked;
    }

    private static bool Matches(byte[] source, int i, byte[] other, int x, int y, int width, int height)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    continue;
                }

                var j = ((ny * width) + nx) * 4;
                var d = 0;
                for (var c = 0; c < 4; c++)
                {
                    d = Math.Max(d, Math.Abs(source[i + c] - other[j + c]));
                }

                if (d <= 48)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
