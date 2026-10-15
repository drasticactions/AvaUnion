using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct AvuProbeResult
{
    public int AbiVersion;
    public int ResolvedSize;
    public fixed byte ShimUnionVersion[32];
    public fixed byte LoadedUnionVersion[32];
}
