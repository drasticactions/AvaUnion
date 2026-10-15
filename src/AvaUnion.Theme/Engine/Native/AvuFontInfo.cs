using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct AvuFontInfo
{
    public int FamilyId;
    public float PointSize;
    public float PixelSize;
    public int Weight;
    public int Italic;
}
