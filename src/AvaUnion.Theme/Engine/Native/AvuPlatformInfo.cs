using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct AvuPlatformInfo
{
    public int SmoothScroll;
    public float AnimationSpeed;
    public int RightToLeft;
    public int IconThemeId;
    public int DefaultStyleId;
}
