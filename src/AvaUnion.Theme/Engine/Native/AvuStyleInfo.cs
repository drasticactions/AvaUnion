using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct AvuStyleInfo
{
    public int IdString;
    public int PathString;
    public int HasErrors;
    public int RuleCount;
    public int MetadataString;
}
