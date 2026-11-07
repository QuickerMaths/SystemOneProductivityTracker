using System.ComponentModel;
using System.Diagnostics;

namespace SystemOneProductivityTracker.Wpf.Services;

public record struct ActiveWindowSnapshot(int Id, string ProcessName, string? WindowTitle, string? DisplayName, bool IsIdle);

public sealed class ActivityMonitorService
{
    private readonly IReadOnlyList<ActiveWindowSnapshot> _activitySnapshots = Array.Empty<ActiveWindowSnapshot>();

    public int ScanCount { get; set; } = 0;

    public void StartMonitoring()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = Task.Run(async () =>
        {
            while (await timer.WaitForNextTickAsync())
            {
                foreach(var process in Process.GetProcesses())
                {
                    GetProcess(process);
                    ScanCount += 1;
                }
            }
        });
    }
    
    public void GetProcess(Process process)
    {
        var trackedProcess = new ActiveWindowSnapshot()
        {
            Id = process.Id,
            ProcessName = process.ProcessName,
            WindowTitle = string.IsNullOrWhiteSpace(process.MainWindowTitle) ? null : process.MainWindowTitle,
            DisplayName = TryGetFriendlyDisplayName(process), 
            IsIdle = false 
        };

        _ = _activitySnapshots.Append(trackedProcess);
    }

    public int DisplayActivitySnapshots()
    {
        return _activitySnapshots.Count;
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
}