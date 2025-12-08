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
        _logger.LogInformation("PERIODIC CACHE UPDATE - Starting background data refresh");

        try
        {
            // Check cache age before update
            var cachedData = await new FileCacheService(_logger).LoadDataFromFileAsync<TarkovLootScanner.Models.TarkovCacheData>("cache_data.json");
            if (cachedData?.LastUpdate != default(DateTime))
            {
                var age = DateTime.UtcNow - cachedData!.LastUpdate;
                _logger.LogInformation($"PERIODIC CACHE UPDATE - Current cache age: {(int)age.TotalMinutes} minutes ({cachedData.LastUpdate:yyyy-MM-dd HH:mm:ss UTC})");
            }

            await _apiService.LoadAllDataForCacheAsync();

            // Check if data was actually updated
            var updatedData = await new FileCacheService(_logger).LoadDataFromFileAsync<TarkovLootScanner.Models.TarkovCacheData>("cache_data.json");
            var updateDuration = DateTime.Now - updateStart;

            if ((updatedData?.LastUpdate ?? DateTime.MinValue) != (cachedData?.LastUpdate ?? DateTime.MinValue))
            {
                _logger.LogInformation($"PERIODIC CACHE UPDATE - SUCCESS: Data refreshed in {updateDuration.TotalSeconds:F1}s");
                _logger.LogInformation($"PERIODIC CACHE UPDATE - New cache timestamp: {updatedData?.LastUpdate:yyyy-MM-dd HH:mm:ss UTC}");
            }
            else
            {
                _logger.LogInformation($"PERIODIC CACHE UPDATE - COMPLETED in {updateDuration.TotalSeconds:F1}s (no data changes detected)");
            }

            await _logger.LogPerformanceAsync("Periodic_Cache_Update", updateDuration, "Background cache check completed");
        }
        catch (Exception ex)
        {
            var errorDuration = DateTime.Now - updateStart;
            _logger.LogError($"PERIODIC CACHE UPDATE - FAILED after {errorDuration.TotalSeconds:F1}s", ex);
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
