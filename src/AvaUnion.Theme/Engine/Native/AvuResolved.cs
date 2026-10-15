using System;

namespace AvaUnion.Engine.Native;

internal partial struct AvuResolved
{
    private const int FontFamily = 1;
    private const int FontPointSize = 2;
    private const int FontPixelSize = 4;
    private const int FontWeight = 8;
    private const int FontItalic = 16;

    public readonly bool Has(int leaf) => leaf < 64 ? (Present0 & (1UL << leaf)) != 0 : (Present1 & (1UL << (leaf - 64))) != 0;

    public readonly UnionFont ReadFont(Func<int, string?> str)
    {
        var mask = TextFontMask;
        return new UnionFont(
            (mask & FontFamily) != 0 ? str(TextFontFamilyId) : null,
            (mask & FontPointSize) != 0 ? TextFontPointSize : null,
            (mask & FontPixelSize) != 0 ? TextFontPixelSize : null,
            (mask & FontWeight) != 0 ? TextFontWeight : null,
            (mask & FontItalic) != 0 ? TextFontItalic != 0 : null);
    }
}
