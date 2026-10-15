using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct AvuInitOptions
{
    public int HostRunsGLib;
    public int ForcePlatform;
}
