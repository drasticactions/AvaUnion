using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct AvuPackageInfo
{
    public int IdString;
    public int PathString;
    public int MetadataString;
}
