using System;
using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

internal static unsafe partial class NativeMethods
{
    public const string Library = "avaunion-native";

    public const int AbiVersion = 2;

    public const int Ok = 0;
    public const int ErrorArgument = -1;
    public const int ErrorException = -2;
    public const int ErrorForeignApplication = -3;
    public const int ErrorNotInitialized = -4;
    public const int ErrorVersion = -5;
    public const int ErrorNoUnion = -6;
    public const int ErrorNoStyle = -7;
    public const int ErrorBuffer = -8;
    public const int ErrorIo = -9;
    public const int ErrorInvalidPackage = -10;

    public const uint ChangeColors = 0x01;
    public const uint ChangeStyle = 0x02;
    public const uint ChangeFonts = 0x04;
    public const uint ChangeIcons = 0x08;
    public const uint ChangeAnimation = 0x10;
    public const uint ChangeLog = 0x20;

    public const int PumpTimer = 0;
    public const int PumpShared = 1;

    [LibraryImport(Library, EntryPoint = "avu_probe")]
    public static partial int Probe(AvuProbeResult* result);

    [LibraryImport(Library, EntryPoint = "avu_init")]
    public static partial int Init(AvuInitOptions* options, AvuInitResult* result);

    [LibraryImport(Library, EntryPoint = "avu_shutdown")]
    public static partial void Shutdown();

    [LibraryImport(Library, EntryPoint = "avu_pump")]
    public static partial int Pump(int maxMilliseconds);

    [LibraryImport(Library, EntryPoint = "avu_set_callback")]
    public static partial void SetCallback(delegate* unmanaged<uint, int, void*, void> callback, void* user);

    [LibraryImport(Library, EntryPoint = "avu_style_default")]
    public static partial IntPtr StyleDefault();

    [LibraryImport(Library, EntryPoint = "avu_style_by_id")]
    public static partial IntPtr StyleById(char* id, int length);

    [LibraryImport(Library, EntryPoint = "avu_style_info_get")]
    public static partial int StyleInfo(IntPtr style, AvuStyleInfo* info);

    [LibraryImport(Library, EntryPoint = "avu_style_from_package")]
    public static partial int StyleFromPackage(char* path, int length, IntPtr* style);

    [LibraryImport(Library, EntryPoint = "avu_installed_styles")]
    public static partial int InstalledStyles(AvuPackageInfo* packages, int capacity);

    [LibraryImport(Library, EntryPoint = "avu_element_create")]
    public static partial IntPtr ElementCreate();

    [LibraryImport(Library, EntryPoint = "avu_element_destroy")]
    public static partial void ElementDestroy(IntPtr element);

    [LibraryImport(Library, EntryPoint = "avu_element_set_type")]
    public static partial void ElementSetType(IntPtr element, char* value, int length);

    [LibraryImport(Library, EntryPoint = "avu_element_set_id")]
    public static partial void ElementSetId(IntPtr element, char* value, int length);

    [LibraryImport(Library, EntryPoint = "avu_element_set_states")]
    public static partial void ElementSetStates(IntPtr element, uint states);

    [LibraryImport(Library, EntryPoint = "avu_element_set_hints")]
    public static partial void ElementSetHints(IntPtr element, char* value, int length);

    [LibraryImport(Library, EntryPoint = "avu_element_set_attribute_string")]
    public static partial void ElementSetAttributeString(IntPtr element, char* name, int nameLength, char* value, int valueLength);

    [LibraryImport(Library, EntryPoint = "avu_element_set_attribute_number")]
    public static partial void ElementSetAttributeNumber(IntPtr element, char* name, int nameLength, double value);

    [LibraryImport(Library, EntryPoint = "avu_element_clear_attributes")]
    public static partial void ElementClearAttributes(IntPtr element);

    [LibraryImport(Library, EntryPoint = "avu_query")]
    public static partial int Query(IntPtr style, IntPtr* chain, int count, AvuResolved* result);

    [LibraryImport(Library, EntryPoint = "avu_scheme_color")]
    public static partial int SchemeColor(int group, int set, int roleKind, int role, uint* argb);

    [LibraryImport(Library, EntryPoint = "avu_set_color_scheme_path")]
    public static partial int SetColorSchemePath(char* path, int length);

    [LibraryImport(Library, EntryPoint = "avu_string")]
    public static partial int String(int id, char* buffer, int capacity);

    [LibraryImport(Library, EntryPoint = "avu_icon_render")]
    public static partial int IconRender(char* name, int length, uint argbOrZero, int width, int height, float scale, byte* buffer, int capacity, int* outWidth, int* outHeight);

    [LibraryImport(Library, EntryPoint = "avu_image_render")]
    public static partial int ImageRender(int pathStringId, uint maskArgb, int maskMode, int width, int height, float scale, byte* buffer, int capacity, int* outWidth, int* outHeight);

    [LibraryImport(Library, EntryPoint = "avu_platform")]
    public static partial int Platform(AvuPlatformInfo* info);

    [LibraryImport(Library, EntryPoint = "avu_font")]
    public static partial int Font(int which, AvuFontInfo* info);

    [LibraryImport(Library, EntryPoint = "avu_export_style")]
    public static partial int ExportStyle(IntPtr style, char* outPath, int length);
}
