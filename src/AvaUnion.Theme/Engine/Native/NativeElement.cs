using System;
using System.Collections.Generic;

namespace AvaUnion.Engine.Native;

internal sealed unsafe class NativeElement : ElementHandle
{
    public NativeElement()
    {
        Handle = NativeMethods.ElementCreate();
    }

    public IntPtr Handle { get; private set; }

    public override void SetType(string? type)
    {
        var s = type ?? string.Empty;
        fixed (char* p = s)
        {
            NativeMethods.ElementSetType(Handle, p, s.Length);
        }
    }

    public override void SetId(string? id)
    {
        var s = id ?? string.Empty;
        fixed (char* p = s)
        {
            NativeMethods.ElementSetId(Handle, p, s.Length);
        }
    }

    public override void SetStates(UnionStates states) => NativeMethods.ElementSetStates(Handle, (uint)states);

    public override void SetHints(IReadOnlyList<string> hints)
    {
        var s = string.Join('\n', hints);
        fixed (char* p = s)
        {
            NativeMethods.ElementSetHints(Handle, p, s.Length);
        }
    }

    public override void SetAttribute(string name, string value)
    {
        fixed (char* n = name)
        fixed (char* v = value)
        {
            NativeMethods.ElementSetAttributeString(Handle, n, name.Length, v, value.Length);
        }
    }

    public override void SetAttribute(string name, double value)
    {
        fixed (char* n = name)
        {
            NativeMethods.ElementSetAttributeNumber(Handle, n, name.Length, value);
        }
    }

    public override void ClearAttributes() => NativeMethods.ElementClearAttributes(Handle);

    protected override void Dispose(bool disposing)
    {
        if (disposing && Handle != IntPtr.Zero)
        {
            NativeMethods.ElementDestroy(Handle);
            Handle = IntPtr.Zero;
        }
    }
}
