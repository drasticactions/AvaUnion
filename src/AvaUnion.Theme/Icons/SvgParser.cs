using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Icons;

internal static class SvgParser
{
    public static bool TryNumber(string text, ref int index, out double value)
    {
        SkipSeparators(text, ref index);
        var start = index;
        if (index < text.Length && text[index] is '+' or '-')
        {
            index++;
        }

        var digits = false;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
            digits = true;
        }

        if (index < text.Length && text[index] == '.')
        {
            index++;
            while (index < text.Length && char.IsAsciiDigit(text[index]))
            {
                index++;
                digits = true;
            }
        }

        if (!digits)
        {
            index = start;
            value = 0;
            return false;
        }

        if (index < text.Length && text[index] is 'e' or 'E')
        {
            var mark = index++;
            if (index < text.Length && text[index] is '+' or '-')
            {
                index++;
            }

            if (index < text.Length && char.IsAsciiDigit(text[index]))
            {
                while (index < text.Length && char.IsAsciiDigit(text[index]))
                {
                    index++;
                }
            }
            else
            {
                index = mark;
            }
        }

        return double.TryParse(text.AsSpan(start, index - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public static List<double> Numbers(string? text)
    {
        var list = new List<double>();
        if (text is null)
        {
            return list;
        }

        var index = 0;
        while (TryNumber(text, ref index, out var value))
        {
            list.Add(value);
        }

        return list;
    }

    public static double Length(string? text, double percentOf = 0, double fallback = 0, double fontSize = 16)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        text = text.Trim();
        var index = 0;
        if (!TryNumber(text, ref index, out var value))
        {
            return fallback;
        }

        return text[index..].Trim() switch
        {
            "" or "px" => value,
            "%" => value * percentOf / 100,
            "pt" => value * 4 / 3,
            "pc" => value * 16,
            "mm" => value * 96 / 25.4,
            "cm" => value * 96 / 2.54,
            "in" => value * 96,
            "em" => value * fontSize,
            "ex" => value * fontSize / 2,
            _ => value,
        };
    }

    public static double Fraction(string? text, double fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        text = text.Trim();
        var index = 0;
        if (!TryNumber(text, ref index, out var value))
        {
            return fallback;
        }

        return text[index..].Trim() == "%" ? value / 100 : value;
    }

    public static Matrix Transform(string? text)
    {
        var result = Matrix.Identity;
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && (char.IsWhiteSpace(text[index]) || text[index] == ','))
            {
                index++;
            }

            var nameStart = index;
            while (index < text.Length && char.IsAsciiLetter(text[index]))
            {
                index++;
            }

            var name = text[nameStart..index];
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (name.Length == 0 || index >= text.Length || text[index] != '(')
            {
                return result;
            }

            var close = text.IndexOf(')', index);
            if (close < 0)
            {
                return result;
            }

            var v = Numbers(text[(index + 1)..close]);
            index = close + 1;
            var m = name switch
            {
                "matrix" when v.Count == 6 => new Matrix(v[0], v[1], v[2], v[3], v[4], v[5]),
                "translate" when v.Count >= 1 => Matrix.CreateTranslation(v[0], v.Count > 1 ? v[1] : 0),
                "scale" when v.Count >= 1 => Matrix.CreateScale(v[0], v.Count > 1 ? v[1] : v[0]),
                "rotate" when v.Count == 1 => Matrix.CreateRotation(v[0] * Math.PI / 180),
                "rotate" when v.Count == 3 => Matrix.CreateTranslation(-v[1], -v[2]) * Matrix.CreateRotation(v[0] * Math.PI / 180) * Matrix.CreateTranslation(v[1], v[2]),
                "skewX" when v.Count == 1 => new Matrix(1, 0, Math.Tan(v[0] * Math.PI / 180), 1, 0, 0),
                "skewY" when v.Count == 1 => new Matrix(1, Math.Tan(v[0] * Math.PI / 180), 0, 1, 0, 0),
                _ => Matrix.Identity,
            };
            result = m * result;
        }

        return result;
    }

    public static bool TryColor(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();
        if (text[0] == '#')
        {
            var hex = text[1..];
            if (hex.Length is 3 or 4)
            {
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2], hex.Length == 4 ? string.Concat(hex[3], hex[3]) : "ff");
            }
            else if (hex.Length == 6)
            {
                hex += "ff";
            }

            if (hex.Length != 8 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgba))
            {
                return false;
            }

            color = Color.FromArgb((byte)rgba, (byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8));
            return true;
        }

        var open = text.IndexOf('(');
        if (open > 0 && text.EndsWith(')'))
        {
            var function = text[..open].Trim().ToLowerInvariant();
            if (function is not ("rgb" or "rgba"))
            {
                return false;
            }

            var parts = text[(open + 1)..^1].Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is < 3 or > 4)
            {
                return false;
            }

            var c = new byte[4];
            c[3] = 255;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                var percent = part.EndsWith('%');
                if (!double.TryParse(percent ? part[..^1] : part, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
                {
                    return false;
                }

                var scale = i == 3 ? (percent ? 2.55 : 255) : (percent ? 2.55 : 1);
                c[i] = (byte)Math.Clamp(Math.Round(n * scale), 0, 255);
            }

            color = Color.FromArgb(c[3], c[0], c[1], c[2]);
            return true;
        }

        if (text.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            color = Colors.Transparent;
            return true;
        }

        return text.Equals("none", StringComparison.OrdinalIgnoreCase) || text.Equals("currentColor", StringComparison.OrdinalIgnoreCase)
            ? false
            : Color.TryParse(text, out color);
    }

    public static Geometry? Path(string? data, FillRule rule)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        var geometry = new StreamGeometry();
        var any = false;
        using (var context = geometry.Open())
        {
            context.SetFillRule(rule);
            var index = 0;
            var command = '\0';
            Point current = default, start = default, control = default;
            var open = false;
            var previous = '\0';

            void Ensure(StreamGeometryContext ctx)
            {
                if (!open)
                {
                    ctx.BeginFigure(current, true);
                    open = true;
                }
            }

            while (true)
            {
                SkipSeparators(data, ref index);
                if (index >= data.Length)
                {
                    break;
                }

                var c = data[index];
                if (char.IsAsciiLetter(c))
                {
                    command = c;
                    index++;
                }
                else if (command == '\0' || command is 'z' or 'Z')
                {
                    break;
                }

                var relative = char.IsLower(command);
                var origin = relative ? current : default;
                var ok = true;
                switch (char.ToUpperInvariant(command))
                {
                    case 'M':
                        if (!Pair(data, ref index, out var m))
                        {
                            ok = false;
                            break;
                        }

                        if (open)
                        {
                            context.EndFigure(false);
                        }

                        current = start = m + origin;
                        context.BeginFigure(current, true);
                        open = true;
                        any = true;
                        command = relative ? 'l' : 'L';
                        previous = 'M';
                        continue;
                    case 'Z':
                        if (open)
                        {
                            context.EndFigure(true);
                            open = false;
                        }

                        current = start;
                        previous = 'Z';
                        continue;
                    case 'L':
                        if (!Pair(data, ref index, out var l))
                        {
                            ok = false;
                            break;
                        }

                        Ensure(context);
                        current = l + origin;
                        context.LineTo(current);
                        break;
                    case 'H':
                        if (!TryNumber(data, ref index, out var h))
                        {
                            ok = false;
                            break;
                        }

                        Ensure(context);
                        current = new Point(relative ? current.X + h : h, current.Y);
                        context.LineTo(current);
                        break;
                    case 'V':
                        if (!TryNumber(data, ref index, out var v))
                        {
                            ok = false;
                            break;
                        }

                        Ensure(context);
                        current = new Point(current.X, relative ? current.Y + v : v);
                        context.LineTo(current);
                        break;
                    case 'C':
                        if (!Pair(data, ref index, out var c1) || !Pair(data, ref index, out var c2) || !Pair(data, ref index, out var ce))
                        {
                            ok = false;
                            break;
                        }

                        Ensure(context);
                        control = c2 + origin;
                        current = ce + origin;
                        context.CubicBezierTo(c1 + origin, control, current);
                        previous = 'C';
                        continue;
                    case 'S':
                        if (!Pair(data, ref index, out var s2) || !Pair(data, ref index, out var se))
                        {
                            ok = false;
                            break;
                        }

                        Ensure(context);
                        var s1 = previous == 'C' ? current * 2 - control : current;
                        control = s2 + origin;
                        current = se + origin;
                        context.CubicBezierTo(s1, control, current);
                        previous = 'C';
                        continue;
                    case 'Q':
                        if (!Pair(data, ref index, out var q1) || !Pair(data, ref index, out var qe))
                        {
                            ok = false;
                            break;
                        }

                        Ensure(context);
                        control = q1 + origin;
                        current = qe + origin;
                        context.QuadraticBezierTo(control, current);
                        previous = 'Q';
                        continue;
                    case 'T':
                        if (!Pair(data, ref index, out var te))
                        {
                            ok = false;
                            break;
                        }

                        Ensure(context);
                        control = previous == 'Q' ? current * 2 - control : current;
                        current = te + origin;
                        context.QuadraticBezierTo(control, current);
                        previous = 'Q';
                        continue;
                    case 'A':
                        if (!TryNumber(data, ref index, out var rx) || !TryNumber(data, ref index, out var ry) || !TryNumber(data, ref index, out var angle)
                            || !Flag(data, ref index, out var large) || !Flag(data, ref index, out var sweep) || !Pair(data, ref index, out var ae))
                        {
                            ok = false;
                            break;
                        }

                        Ensure(context);
                        var end = ae + origin;
                        if (rx == 0 || ry == 0)
                        {
                            context.LineTo(end);
                        }
                        else if (end != current)
                        {
                            context.ArcTo(end, new Size(Math.Abs(rx), Math.Abs(ry)), angle, large, sweep ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
                        }

                        current = end;
                        break;
                    default:
                        ok = false;
                        break;
                }

                if (!ok)
                {
                    break;
                }

                previous = char.ToUpperInvariant(command);
            }

            if (open)
            {
                context.EndFigure(false);
            }
        }

        return any ? geometry : null;
    }

    public static Geometry? Poly(string? points, bool closed, FillRule rule)
    {
        var v = Numbers(points);
        if (v.Count < 4)
        {
            return null;
        }

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.SetFillRule(rule);
            context.BeginFigure(new Point(v[0], v[1]), true);
            for (var i = 2; i + 1 < v.Count; i += 2)
            {
                context.LineTo(new Point(v[i], v[i + 1]));
            }

            context.EndFigure(closed);
        }

        return geometry;
    }

    public static Geometry Ellipse(Point center, double rx, double ry)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            var size = new Size(rx, ry);
            context.BeginFigure(new Point(center.X + rx, center.Y), true);
            context.ArcTo(new Point(center.X, center.Y + ry), size, 0, false, SweepDirection.Clockwise);
            context.ArcTo(new Point(center.X - rx, center.Y), size, 0, false, SweepDirection.Clockwise);
            context.ArcTo(new Point(center.X, center.Y - ry), size, 0, false, SweepDirection.Clockwise);
            context.ArcTo(new Point(center.X + rx, center.Y), size, 0, false, SweepDirection.Clockwise);
            context.EndFigure(true);
        }

        return geometry;
    }

    public static Geometry Rectangle(Rect rect, double rx, double ry)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            if (rx <= 0 || ry <= 0)
            {
                context.BeginFigure(rect.TopLeft, true);
                context.LineTo(rect.TopRight);
                context.LineTo(rect.BottomRight);
                context.LineTo(rect.BottomLeft);
            }
            else
            {
                var size = new Size(rx, ry);
                context.BeginFigure(new Point(rect.X + rx, rect.Y), true);
                context.LineTo(new Point(rect.Right - rx, rect.Y));
                context.ArcTo(new Point(rect.Right, rect.Y + ry), size, 0, false, SweepDirection.Clockwise);
                context.LineTo(new Point(rect.Right, rect.Bottom - ry));
                context.ArcTo(new Point(rect.Right - rx, rect.Bottom), size, 0, false, SweepDirection.Clockwise);
                context.LineTo(new Point(rect.X + rx, rect.Bottom));
                context.ArcTo(new Point(rect.X, rect.Bottom - ry), size, 0, false, SweepDirection.Clockwise);
                context.LineTo(new Point(rect.X, rect.Y + ry));
                context.ArcTo(new Point(rect.X + rx, rect.Y), size, 0, false, SweepDirection.Clockwise);
            }

            context.EndFigure(true);
        }

        return geometry;
    }

    private static bool Pair(string text, ref int index, out Point point)
    {
        if (TryNumber(text, ref index, out var x) && TryNumber(text, ref index, out var y))
        {
            point = new Point(x, y);
            return true;
        }

        point = default;
        return false;
    }

    private static bool Flag(string text, ref int index, out bool flag)
    {
        SkipSeparators(text, ref index);
        flag = false;
        if (index < text.Length && text[index] is '0' or '1')
        {
            flag = text[index++] == '1';
            return true;
        }

        return false;
    }

    private static void SkipSeparators(string text, ref int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        if (index < text.Length && text[index] == ',')
        {
            index++;
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }
    }
}
