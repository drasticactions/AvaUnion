using System;
using Avalonia.Threading;

namespace AvaUnion.Engine.Native;

internal sealed class NativePump : IDisposable
{
    private static readonly TimeSpan s_fast = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan s_slow = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan s_idleBeforeBackoff = TimeSpan.FromSeconds(10);

    private readonly DispatcherTimer _timer;
    private DateTime _lastActivity = DateTime.UtcNow;

    public NativePump()
    {
        _timer = new DispatcherTimer(s_fast, DispatcherPriority.Background, OnTick);
        _timer.Start();
    }

    public void Wake()
    {
        _lastActivity = DateTime.UtcNow;
        _timer.Interval = s_fast;
    }

    public int PumpNow() => NativeMethods.Pump(2);

    private void OnTick(object? sender, EventArgs e)
    {
        var processed = PumpNow();
        var now = DateTime.UtcNow;
        if (processed > 0)
        {
            _lastActivity = now;
            _timer.Interval = s_fast;
        }
        else if (now - _lastActivity > s_idleBeforeBackoff)
        {
            _timer.Interval = s_slow;
        }
    }

    public void Dispose() => _timer.Stop();
}
