using System.Timers;

namespace TarkovLootScanner.Services;

/// <summary>
/// Service responsible for initializing and periodically updating the cache
/// </summary>
public class CacheInitializationService : IDisposable
{
    private readonly ITarkovApiService _apiService;
    private readonly ILoggerService _logger;
    private readonly System.Timers.Timer? _updateTimer;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(15);
    private bool _isDisposed;

    public CacheInitializationService(ITarkovApiService apiService, ILoggerService logger)
    {
        _apiService = apiService;
        _logger = logger;

        // Configure periodic updates
        _updateTimer = new System.Timers.Timer(_updateInterval.TotalMilliseconds);
        _updateTimer.Elapsed += OnUpdateTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Start();

        _logger.LogInformation($"CacheInitializationService configured with {_updateInterval.TotalMinutes} minute update intervals");
    }

    /// <summary>
    /// Performs initial cache loading (synchronous for startup)
    /// </summary>
    public async Task InitializeCacheAsync()
    {
        var initStart = DateTime.Now;
        _logger.LogInformation("Starting initial cache loading");

        try
        {
            await _apiService.LoadAllDataForCacheAsync();
            await _logger.LogPerformanceAsync("Initial_Cache_Load", DateTime.Now - initStart, "Application startup cache loaded");

            // Load trader avatars as well
            await _apiService.LoadTraderAvatarsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize cache during startup", ex);
            // Don't throw - allow app to start with empty cache
        }
    }

    private async void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var updateStart = DateTime.Now;
        _logger.LogInformation("Starting periodic cache update");

        try
        {
            await _apiService.LoadAllDataForCacheAsync();
            await _logger.LogPerformanceAsync("Periodic_Cache_Update", DateTime.Now - updateStart, "Background cache refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update cache periodically", ex);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _updateTimer?.Dispose();
        _logger.LogInformation("CacheInitializationService disposed");
    }
}
