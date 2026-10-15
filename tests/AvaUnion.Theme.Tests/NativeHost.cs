using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AvaUnion.Engine.Native;

namespace AvaUnion.Theme.Tests;

internal static unsafe partial class NativeHost
{
    private static readonly BlockingCollection<Action> s_queue = new();
    private static NativeEngine? s_engine;
    private static string? s_unavailable;
    private static readonly Lazy<bool> s_started = new(Start);

    [LibraryImport("libc", EntryPoint = "setenv")]
    private static partial int SetEnv(byte* name, byte* value, int overwrite);

    public static string ShimPath => Path.Combine(TestData.RepoRoot, "native", "build", $"libavaunion-native-{TestData.UnionVersion}.so");

    public static string? Unavailable
    {
        get
        {
            _ = s_started.Value;
            return s_unavailable;
        }
    }

    public static string? DataMismatch =>
        Unavailable ?? (Run(e => e.UnionVersion) is var installed && installed != TestData.UnionVersion
            ? $"the data comes from Union {TestData.UnionVersion}, and Union {installed} is installed"
            : null);

    public static T Run<T>(Func<NativeEngine, T> action)
    {
        if (Unavailable is { } reason)
        {
            throw new InvalidOperationException(reason);
        }

        T result = default!;
        Exception? error = null;
        using var done = new ManualResetEventSlim();
        s_queue.Add(() =>
        {
            try
            {
                result = action(s_engine!);
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                done.Set();
            }
        });
        done.Wait();
        if (error is not null)
        {
            throw new InvalidOperationException("native call failed", error);
        }

        return result;
    }

    private static bool Start()
    {
        if (!OperatingSystem.IsLinux())
        {
            s_unavailable = "not Linux";
            return false;
        }

        if (Environment.GetEnvironmentVariable(NativeLibraryResolver.PathVariable) is null)
        {
            if (!File.Exists(ShimPath))
            {
                s_unavailable = "shim not built (native/build)";
                return false;
            }

            Environment.SetEnvironmentVariable(NativeLibraryResolver.PathVariable, ShimPath);
        }

        var scratch = Directory.CreateTempSubdirectory("avaunion-tests-");
        Native("XDG_CONFIG_HOME", Directory.CreateDirectory(Path.Combine(scratch.FullName, "config")).FullName);
        Native("XDG_CACHE_HOME", Directory.CreateDirectory(Path.Combine(scratch.FullName, "cache")).FullName);

        using var ready = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try
            {
                s_engine = NativeEngine.Create();
            }
            catch (Exception e)
            {
                s_unavailable = e.Message;
            }

            ready.Set();
            foreach (var action in s_queue.GetConsumingEnumerable())
            {
                action();
            }
        })
        {
            IsBackground = true,
            Name = "Qt",
        };
        thread.Start();
        ready.Wait();
        return s_unavailable is null;
    }

    private static void Native(string name, string value)
    {
        var n = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        var v = System.Text.Encoding.UTF8.GetBytes(value + "\0");
        fixed (byte* pn = n)
        fixed (byte* pv = v)
        {
            SetEnv(pn, pv, 1);
        }
    }
}
