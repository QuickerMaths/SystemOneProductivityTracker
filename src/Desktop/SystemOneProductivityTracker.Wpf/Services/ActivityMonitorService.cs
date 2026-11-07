using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SystemOneProductivityTracker.Wpf.Native;

namespace SystemOneProductivityTracker.Wpf.Services;

public record struct ActiveWindowSnapshot(int Id, string ProcessName, string? WindowTitle, string? DisplayName, bool IsIdle);

public sealed class ActivityMonitorService: IDisposable
{
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _idleThreshold = TimeSpan.FromSeconds(2);
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public event Action<ActiveWindowSnapshot>? SpanshotCaptured;

    public void Start()
    {
        if (_loopTask is not null) return;
        _cts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _loopTask = null;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_pollInterval);

        while(await timer.WaitForNextTickAsync(cancellationToken))
        {
            var snapshot = SampleCurrentWindow();
            if(snapshot is not null)
            {
                SpanshotCaptured?.Invoke(snapshot.Value);
            }
        }
    }

    private  ActiveWindowSnapshot? SampleCurrentWindow()
    {
        bool isIdle = GetIdleTime() >= _idleThreshold;

        nint hWnd = Win32Api.GetForegroundWindow();
        if (hWnd == nint.Zero) return null;

        Win32Api.GetWindowThreadProcessId(hWnd, out uint processId);
        if (processId == 0) return null;

        try
        {
            using Process process = Process.GetProcessById((int) processId);
            
            string processName = process.ProcessName;
            string? windowTitle = string.IsNullOrWhiteSpace(process.MainWindowTitle)
                ? null
                : process.MainWindowTitle;
            string? displayName = TryGetFriendlyDisplayName(process);

            return new ActiveWindowSnapshot((int)processId, processName, windowTitle, displayName, isIdle);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static TimeSpan GetIdleTime()
    {
        var info = new Win32Api.LASTINPUTINFO
        {
            cbSize = (uint)Marshal.SizeOf<Win32Api.LASTINPUTINFO>()
        };

        if (!Win32Api.GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        uint currentTicks = unchecked((uint)Environment.TickCount);
        uint idleMilliseconds = unchecked(currentTicks - info.dwTime);

        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }

    private static string? TryGetFriendlyDisplayName(Process process)
    {
        try
        {
            string? description = process.MainModule?.FileVersionInfo.FileDescription;
            return string.IsNullOrWhiteSpace(description) ? process.ProcessName : description;
        }
        catch (Win32Exception)
        {
            return process.ProcessName;
        }
        catch (InvalidOperationException)
        {
            return process.ProcessName;
        }
    }

    public void Dispose() => Stop();
}