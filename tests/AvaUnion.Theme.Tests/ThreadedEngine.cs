using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaUnion.Engine;

namespace AvaUnion.Theme.Tests;

internal sealed class ThreadedEngine : IUnionEngine
{
    public UnionEngineKind Kind => UnionEngineKind.Native;

    public int Queries { get; private set; }

    public string Description => NativeHost.Run(e => e.Description);

    public UnionStyleHandle DefaultStyle => NativeHost.Run(e => e.DefaultStyle);

    public UnionFonts Fonts => NativeHost.Run(e => e.Fonts);

    public UnionPlatform Platform => NativeHost.Run(e => e.Platform);

    public string? ColorScheme => NativeHost.Run(e => e.ColorScheme);

    public int Epoch => NativeHost.Run(e => e.Epoch);

    public event Action<UnionChange>? Changed
    {
        add { }
        remove { }
    }

    public UnionStyleHandle? GetStyle(string id) => NativeHost.Run(e => e.GetStyle(id));

    public IReadOnlyList<UnionStyleMetadata> Styles => NativeHost.Run(e => e.Styles);

    public ElementHandle CreateElement() => new Handle(NativeHost.Run(e => e.CreateElement()));

    public ResolvedStyle Query(UnionStyleHandle style, ReadOnlySpan<ElementHandle> chain)
    {
        var inner = new ElementHandle[chain.Length];
        for (var i = 0; i < chain.Length; i++)
        {
            inner[i] = ((Handle)chain[i]).Inner;
        }

        Queries++;
        return NativeHost.Run(e => e.Query(style, inner));
    }

    public Color SchemeColor(ColorGroup group, ColorSet set, ColorRoleKind kind, int role) => NativeHost.Run(e => e.SchemeColor(group, set, kind, role));

    public IconRaster? RenderIcon(string name, Color? tint, PixelSize size, double scale) => NativeHost.Run(e => e.RenderIcon(name, tint, size, scale));

    public IconRaster? RenderImage(string source, Color? maskColor, bool invertedMask, PixelSize size, double scale) => NativeHost.Run(e => e.RenderImage(source, maskColor, invertedMask, size, scale));

    public UnionImageGeometry? GetImageGeometry(string source) => NativeHost.Run(e => e.GetImageGeometry(source));

    public void SetColorScheme(string? pathOrName) => NativeHost.Run(e =>
    {
        e.SetColorScheme(pathOrName);
        return 0;
    });

    private sealed class Handle(ElementHandle inner) : ElementHandle
    {
        public ElementHandle Inner { get; } = inner;

        public override void SetType(string? type) => Do(h => h.SetType(type));

        public override void SetId(string? id) => Do(h => h.SetId(id));

        public override void SetStates(UnionStates states) => Do(h => h.SetStates(states));

        public override void SetHints(IReadOnlyList<string> hints)
        {
            var copy = new List<string>(hints);
            Do(h => h.SetHints(copy));
        }

        public override void SetAttribute(string name, string value) => Do(h => h.SetAttribute(name, value));

        public override void SetAttribute(string name, double value) => Do(h => h.SetAttribute(name, value));

        public override void ClearAttributes() => Do(h => h.ClearAttributes());

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Do(h => h.Dispose());
            }

            base.Dispose(disposing);
        }

        private void Do(Action<ElementHandle> action) => NativeHost.Run(_ =>
        {
            action(Inner);
            return 0;
        });
    }
}
