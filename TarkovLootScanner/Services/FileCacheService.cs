using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using TarkovLootScanner.Models;

namespace TarkovLootScanner.Services;

/// <summary>
/// File-based cache service implementation that persists data and images to local files
/// </summary>
public class FileCacheService : ICacheService
{
    private readonly ILoggerService _logger;
    private readonly string _cacheDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, CacheEntry> _memoryCache;

    private class CacheEntry
    {
        public object Data { get; set; }
        public DateTime Timestamp { get; set; }

        public CacheEntry(object data)
        {
            Data = data;
            Timestamp = DateTime.UtcNow;
        }

        public bool IsExpired(TimeSpan expiration) => DateTime.UtcNow - Timestamp > expiration;
    }

    public FileCacheService(ILoggerService logger)
    {
        _logger = logger;
        _logger.LogInformation("Initializing FileCacheService");

        // Get the directory where the executable is located
        var exePath = AppDomain.CurrentDomain.BaseDirectory;
        _cacheDirectory = Path.Combine(exePath, "cache");

        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheDirectory);
        _logger.LogInformation($"Cache directory created/verified at: {_cacheDirectory}");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        _httpClient = new HttpClient();
        _memoryCache = new ConcurrentDictionary<string, CacheEntry>();

        _logger.LogInformation("FileCacheService initialization completed");
    }

    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T?>> loader, TimeSpan? expiration = null) where T : class
    {
        var startTime = DateTime.Now;
        expiration ??= TimeSpan.FromMinutes(15);

        _logger.LogInformation($"CACHE - Starting GetOrLoad for key: {key}, type: {typeof(T).Name}");

        // Check memory cache first
        if (_memoryCache.TryGetValue(key, out var cacheEntry) && !cacheEntry.IsExpired(expiration.Value))
        {
            await _logger.LogCacheOperationAsync("MEMORY_HIT", key, true, DateTime.Now - startTime);
            return cacheEntry.Data as T;
        }

        // Check file cache
        var filePath = GetCacheFilePath(key);
        if (File.Exists(filePath))
        {
            try
            {
                var fileReadStart = DateTime.Now;
                var json = await File.ReadAllTextAsync(filePath);
                var data = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                var fileReadTime = DateTime.Now - fileReadStart;

                if (data != null)
                {
                    // Check if file is expired
                    var fileInfo = new FileInfo(filePath);
                    if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc <= expiration.Value)
                    {
                        _memoryCache[key] = new CacheEntry(data);
                        await _logger.LogCacheOperationAsync("FILE_HIT", key, true, DateTime.Now - startTime);
                        await _logger.LogPerformanceAsync("FileRead", fileReadTime, $"Size: {json.Length} chars");
                        return data;
                    }
                    else
                    {
                        _logger.LogInformation($"CACHE - File expired for key: {key}, age: {DateTime.UtcNow - fileInfo.LastWriteTimeUtc}");
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.LogCacheOperationAsync("FILE_ERROR", key, false);
                _logger.LogError($"Error reading cached data for {key}", ex);
                // Continue to loader
            }
        }
        else
        {
            _logger.LogInformation($"CACHE - No file cache found for key: {key}");
        }

        // Load from source
        _logger.LogInformation($"CACHE - Loading from source for key: {key}");
        var result = await loader();
        if (result != null)
        {
            await StoreAsync(key, result);
            await _logger.LogCacheOperationAsync("SOURCE_LOAD", key, true, DateTime.Now - startTime);
        }
        else
        {
            await _logger.LogCacheOperationAsync("SOURCE_LOAD", key, false, DateTime.Now - startTime);
        }

        return result;
    }

    public async Task StoreAsync<T>(string key, T data) where T : class
    {
        _memoryCache[key] = new CacheEntry(data);

        // Also save to file
        var filePath = GetCacheFilePath(key);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task SaveDataToFileAsync<T>(string fileName, T data)
    {
        var filePath = Path.Combine(_cacheDirectory, fileName);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<T?> LoadDataFromFileAsync<T>(string fileName)
    {
        var filePath = Path.Combine(_cacheDirectory, fileName);
        if (!File.Exists(filePath))
        {
            return default;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data from file {fileName}: {ex.Message}");
            return default;
        }
    }

    public async Task<string?> DownloadAndCacheImageAsync(string url, string cacheSubfolder, string fileName)
    {
        var subfolderPath = Path.Combine(_cacheDirectory, cacheSubfolder);
        Directory.CreateDirectory(subfolderPath);

        var localPath = Path.Combine(subfolderPath, fileName);

        // Check if already cached
        if (File.Exists(localPath))
        {
            return localPath;
        }

        try
        {
            var imageBytes = await _httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(localPath, imageBytes);
            return localPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading image from {url}: {ex.Message}");
            return null;
        }
    }

    public string? GetCachedImagePath(string cacheSubfolder, string fileName)
    {
        var localPath = Path.Combine(_cacheDirectory, cacheSubfolder, fileName);
        return File.Exists(localPath) ? localPath : null;
    }

    public async Task ClearAllCacheAsync()
    {
        _memoryCache.Clear();

        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, true);
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<(int fileCount, long totalSizeMb)> GetCacheStatisticsAsync()
    {
        if (!Directory.Exists(_cacheDirectory))
        {
            return (0, 0);
        }

        var files = Directory.GetFiles(_cacheDirectory, "*", SearchOption.AllDirectories);
        var totalSize = files.Sum(f => new FileInfo(f).Length);
        return (files.Length, totalSize / (1024 * 1024));
    }

    private string GetCacheFilePath(string key)
    {
        // Sanitize key for filename
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_cacheDirectory, $"{safeKey}.json");
    }
}
