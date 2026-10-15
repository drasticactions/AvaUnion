using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct AvuInitResult
{
    public int PumpMode;
    public int PlatformThemeLoaded;
    public int CreatedApplication;
    public int PlatformPluginId;
    public int DefaultStyleId;
    public int QtVersionId;
    public int UnionVersionId;
}
