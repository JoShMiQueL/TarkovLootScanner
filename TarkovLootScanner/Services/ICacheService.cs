using TarkovLootScanner.Models;

namespace TarkovLootScanner.Services;

/// <summary>
/// Interface for caching services that handle file-based persistence of data and images
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets data from cache or loads it using the provided loader function
    /// </summary>
    Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T?>> loader, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Stores data in cache
    /// </summary>
    Task StoreAsync<T>(string key, T data) where T : class;

    /// <summary>
    /// Saves data as JSON to a file
    /// </summary>
    Task SaveDataToFileAsync<T>(string fileName, T data);

    /// <summary>
    /// Loads data from JSON file, returns null if file doesn't exist
    /// </summary>
    Task<T?> LoadDataFromFileAsync<T>(string fileName);

    /// <summary>
    /// Downloads and caches an image from URL to local file, returns local path
    /// </summary>
    Task<string?> DownloadAndCacheImageAsync(string url, string cacheSubfolder, string fileName);

    /// <summary>
    /// Gets the local path for a cached image, or null if not cached
    /// </summary>
    string? GetCachedImagePath(string cacheSubfolder, string fileName);

    /// <summary>
    /// Clears all cached data and files
    /// </summary>
    Task ClearAllCacheAsync();

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    Task<(int fileCount, long totalSizeMb)> GetCacheStatisticsAsync();
}
