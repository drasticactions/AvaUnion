using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace AvaUnion.Engine.Managed;

/// <summary>
/// A symbolic color. It is a tree of RGBA values, provider lookups and
/// operations.
/// </summary>
public abstract class UnionColor
{
    private UnionColor()
    {
    }

    /// <summary>An explicit empty color, for example from <c>background: none</c>. It resolves to transparent.</summary>
    public static readonly UnionColor Empty = new EmptyColor();

    /// <summary>A literal color.</summary>
    public static UnionColor Rgba(byte r, byte g, byte b, byte a = 255) => new RgbaColor(new UnionRgba(r, g, b, a));

    /// <summary>A color from a provider, for example <c>kcolorscheme</c>.</summary>
    public static UnionColor Custom(string source, IReadOnlyList<string> arguments) => new CustomColor(source, arguments);

    /// <summary>Adds each channel. The result is clamped.</summary>
    public static UnionColor Add(UnionColor a, UnionColor b) => new BinaryColor(BinaryOp.Add, a, b);

    /// <summary>Subtracts each channel. The result is clamped.</summary>
    public static UnionColor Subtract(UnionColor a, UnionColor b) => new BinaryColor(BinaryOp.Subtract, a, b);

    /// <summary>Multiplies each channel in the range 0..1. The result is truncated.</summary>
    public static UnionColor Multiply(UnionColor a, UnionColor b) => new BinaryColor(BinaryOp.Multiply, a, b);

    /// <summary>Replaces the given channels.</summary>
    public static UnionColor Set(UnionColor color, byte? r, byte? g, byte? b, byte? a) => new SetColor(color, r, g, b, a);

    /// <summary>Calculates <c>a·(1−t) + b·t</c> for each channel. The result is truncated.</summary>
    public static UnionColor Mix(UnionColor a, UnionColor b, double amount) => new MixColor(a, b, amount);

    /// <summary>Resolves to RGBA. Returns null when the color cannot resolve.</summary>
    /// <param name="provider">Resolves a provider color from (source, arguments) to RGBA or null.</param>
    public abstract UnionRgba? ToRgba(Func<string, IReadOnlyList<string>, UnionRgba?> provider);

    /// <summary>Resolves to an Avalonia color. A color that cannot resolve becomes transparent.</summary>
    public Color Resolve(Func<string, IReadOnlyList<string>, UnionRgba?> provider) =>
        ToRgba(provider)?.ToColor() ?? UnionRgba.Transparent;

    private static UnionRgba OrZero(UnionRgba? value) => value ?? default;

    private static byte Clamp(int value) => (byte)Math.Clamp(value, 0, 255);

    private sealed class EmptyColor : UnionColor
    {
        public override UnionRgba? ToRgba(Func<string, IReadOnlyList<string>, UnionRgba?> provider) => null;

        public override string ToString() => "Color(Empty)";
    }

    private sealed class RgbaColor(UnionRgba value) : UnionColor
    {
        public override UnionRgba? ToRgba(Func<string, IReadOnlyList<string>, UnionRgba?> provider) => value;

        public override string ToString() => $"RGBA({value.R}, {value.G}, {value.B}, {value.A})";
    }

    private sealed class CustomColor(string source, IReadOnlyList<string> arguments) : UnionColor
    {
        public override UnionRgba? ToRgba(Func<string, IReadOnlyList<string>, UnionRgba?> provider) => provider(source, arguments);

        public override string ToString() => $"Custom({source}, {string.Join(", ", arguments)})";
    }

    private enum BinaryOp
    {
        Add,
        Subtract,
        Multiply,
    }

    private sealed class BinaryColor(BinaryOp op, UnionColor a, UnionColor b) : UnionColor
    {
        public override UnionRgba? ToRgba(Func<string, IReadOnlyList<string>, UnionRgba?> provider)
        {
            var x = OrZero(a.ToRgba(provider));
            var y = OrZero(b.ToRgba(provider));
            return op switch
            {
                BinaryOp.Add => new UnionRgba(Clamp(x.R + y.R), Clamp(x.G + y.G), Clamp(x.B + y.B), Clamp(x.A + y.A)),
                BinaryOp.Subtract => new UnionRgba(Clamp(x.R - y.R), Clamp(x.G - y.G), Clamp(x.B - y.B), Clamp(x.A - y.A)),
                _ => new UnionRgba(Mul(x.R, y.R), Mul(x.G, y.G), Mul(x.B, y.B), Mul(x.A, y.A)),
            };
        }

        private static byte Mul(byte p, byte q) => Clamp((int)(p / 255.0 * (q / 255.0) * 255.0));

        public override string ToString() => $"{op}({a}, {b})";
    }

    private sealed class SetColor(UnionColor color, byte? r, byte? g, byte? b, byte? a) : UnionColor
    {
        public override UnionRgba? ToRgba(Func<string, IReadOnlyList<string>, UnionRgba?> provider)
        {
            var c = OrZero(color.ToRgba(provider));
            return new UnionRgba(r ?? c.R, g ?? c.G, b ?? c.B, a ?? c.A);
        }

        public override string ToString() => $"Set({color}, r: {r}, g: {g}, b: {b}, a: {a})";
    }

    private sealed class MixColor(UnionColor a, UnionColor b, double amount) : UnionColor
    {
        public override UnionRgba? ToRgba(Func<string, IReadOnlyList<string>, UnionRgba?> provider)
        {
            var x = OrZero(a.ToRgba(provider));
            var y = OrZero(b.ToRgba(provider));
            var inv = 1.0 - amount;
            return new UnionRgba(Mix(x.R, y.R, inv, amount), Mix(x.G, y.G, inv, amount), Mix(x.B, y.B, inv, amount), Mix(x.A, y.A, inv, amount));
        }

        private static byte Mix(byte p, byte q, double inv, double t) => (byte)(p * inv + q * t);

        public override string ToString() => $"Mix({a}, {b}, {amount})";
    }
}
