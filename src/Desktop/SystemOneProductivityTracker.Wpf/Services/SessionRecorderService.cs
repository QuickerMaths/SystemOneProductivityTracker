using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using SystemOneProductivityTracker.Application.Interfaces;
using SystemOneProductivityTracker.Domain.Entities;
using SystemOneProductivityTracker.Infrastructure.Persistence;

namespace SystemOneProductivityTracker.Wpf.Services
{
    public sealed class SessionRecorderService
    {
        private readonly ActivityMonitorService _monitor;
        private readonly IAuthService _authService;
        private readonly IJevClassficationService _classifier;
        private readonly IDbContextFactory<SystemOneProductivityTrackerDbContext> _dbFactory;
        private SemaphoreSlim _syncLock = new(1, 1);
        private readonly object _stateLock = new();


        private readonly Dictionary<string, TrackedApp> _trackedAppsCache = new(StringComparer.OrdinalIgnoreCase);

        private ActivitySession? _currentSession;
        private string? _currentProcessName;

        public event Action<string, TimeSpan>? CurrentActivityUpdated;

        public SessionRecorderService(
            ActivityMonitorService monitor,
            IAuthService authService,
            IJevClassficationService classifier,
            IDbContextFactory<SystemOneProductivityTrackerDbContext> dbFactory)
        {
            _monitor = monitor;
            _authService = authService;
            _classifier = classifier;
            _dbFactory = dbFactory;

            _authService.AuthenticationStateChanged += OnAuthStateChanged;
            _monitor.SpanshotCaptured += OnSnapShotCaptured;
        }

        private async void OnAuthStateChanged()
        {
            if(_authService.IsAuthenticated && _authService.CurrentUserId is not null)
            {
                await LoadUserAppsCacheAsync(_authService.CurrentUserId);
                _monitor.Start();
            }
            else
            {
                _monitor.Stop();
                await FlushCurrentSessionToDbAsync();
                _trackedAppsCache.Clear();
            }
        }

        private async Task LoadUserAppsCacheAsync(string userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var userApps = await db.TrackedApps
                .Where(x => x.UserId == userId)
                .AsNoTracking()
                .ToListAsync( );

            _trackedAppsCache.Clear();
            foreach(var app in userApps)
            {
                _trackedAppsCache[app.ProcessName] = app;
            }

            Debug.WriteLine($"Loaded {_trackedAppsCache.Count} known apps");
        }

        private async void OnSnapShotCaptured(ActiveWindowSnapshot snapshot)
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUserId is null)
                return;

            if (!await _syncLock.WaitAsync(0))
                return;

            try
            {
                if(snapshot.IsIdle)
                {
                    if(_currentSession is not null)
                    {
                        await FlushCurrentSessionToDbAsync();
                    }
                    CurrentActivityUpdated?.Invoke("Idle", TimeSpan.Zero);
                    return;
                }

                if(_currentSession is not null
                    && string.Equals(_currentProcessName, snapshot.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    _currentSession.EndedAt = DateTime.UtcNow;
                    _currentSession.WindowTitle = snapshot.WindowTitle ?? _currentSession.WindowTitle;

                    var elapsed = _currentSession.EndedAt - _currentSession.StartedAt;
                    CurrentActivityUpdated?.Invoke(snapshot.DisplayName ?? snapshot.ProcessName, elapsed);
                    return;
                }

                if(_currentSession is not null)
                {
                    await FlushCurrentSessionToDbAsync();
                }

                var trackedApp = await GetOrCreateTrackedAppAsync(_authService.CurrentUserId, snapshot);

                var now = DateTime.UtcNow;
                _currentProcessName = snapshot.ProcessName;
                _currentSession = new ActivitySession
                {
                    UserId = _authService.CurrentUserId,
                    TrackedAppId = trackedApp.TrackedAppId,
                    WindowTitle = snapshot.WindowTitle,
                    StartedAt = now,
                    EndedAt = now
                };

                CurrentActivityUpdated?.Invoke(trackedApp.DisplayName ?? trackedApp.ProcessName, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"er: {ex.Message}");
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private async Task<TrackedApp> GetOrCreateTrackedAppAsync(string userId, ActiveWindowSnapshot snapshot)
        {
            if (_trackedAppsCache.TryGetValue(snapshot.ProcessName, out var existingApp))
            {
                return existingApp;
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var newApp = new TrackedApp
            {
                UserId = userId,
                ProcessName = snapshot.ProcessName,
                DisplayName = snapshot.DisplayName,
                IsAllwaysAllowed = false,
                AppCategoryId = null
            };

            db.TrackedApps.Add(newApp);
            await db.SaveChangesAsync();

            Debug.WriteLine($"New tracked app: {snapshot.DisplayName ?? snapshot.ProcessName}");

            _trackedAppsCache[newApp.ProcessName] = newApp;

            return newApp;
        }

        public async Task FlushCurrentSessionToDbAsync()
        {
            var snapshot = DetachSessionToFlush();
            if (snapshot is null) return;

            var (sessionToFlush, processName) = snapshot.Value;

            await using var db = await _dbFactory.CreateDbContextAsync();

            await _classifier.ClassifyAppAsync(JevRequest(processName, sessionToFlush.WindowTitle));

            db.ActivitySessions.Add(sessionToFlush);
            await db.SaveChangesAsync();

            Debug.WriteLine("Session flushed");

            (ActivitySession Session, string ProcessName)? DetachSessionToFlush()
            {
                lock (_stateLock)
                {
                    if (_currentSession is null || _currentProcessName is null) return null;
                    if ((_currentSession.EndedAt - _currentSession.StartedAt).TotalSeconds < 2) return null;

                    var result = (_currentSession, _currentProcessName);

                    _currentSession = null;
                    _currentProcessName = null;

                    return result;
                }
            }
        }

        private JevClassificationRequest JevRequest(string processName, string? windowSampleName)
        {
            return new JevClassificationRequest(processName, windowSampleName);
        }
    }
}
