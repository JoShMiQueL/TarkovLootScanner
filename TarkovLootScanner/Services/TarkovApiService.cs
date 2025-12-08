using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TarkovLootScanner.Models;

namespace TarkovLootScanner.Services;

/// <summary>
/// Service for Tarkov data retrieval from local cache with API fallback
/// </summary>
public class TarkovApiService : ITarkovApiService
{
    private readonly ICacheService _cacheService;
    private readonly ILoggerService _logger;
    private readonly HttpClient? _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeSpan _cacheValidityPeriod = TimeSpan.FromMinutes(15);

    public TarkovApiService(ICacheService cacheService, ILoggerService logger)
    {
        _cacheService = cacheService;
        _logger = logger;

        _logger.LogInformation("Initializing TarkovApiService");

        // Initialize HTTP client for API fallback (keeping minimal for emergency API calls)
        try
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.tarkov.dev/")
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TarkovLootScanner/1.0");
            _logger.LogInformation("HTTP client initialized for API fallback");
        }
        catch (Exception ex)
        {
            _httpClient = null;
            _logger.LogError("Failed to initialize HTTP client", ex);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _logger.LogInformation("TarkovApiService initialization completed");
    }



    /// <summary>
    /// Gets item data by ID from cache
    /// </summary>
    public async Task<TarkovItem?> GetItemByIdAsync(string itemId)
    {
        var cachedData = await GetAllCachedDataAsync();
        return cachedData?.Items?.FirstOrDefault(i => i.Id == itemId);
    }

    /// <summary>
    /// Gets item data by name from cache (searches for items containing the name)
    /// </summary>
    public async Task<List<TarkovItem>> GetItemsByNameAsync(string itemName)
    {
        var cachedData = await GetAllCachedDataAsync();
        if (cachedData?.Items == null)
            return new List<TarkovItem>();

        return cachedData.Items
            .Where(i => !string.IsNullOrEmpty(i.Name) &&
                       i.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Loads all data for caching with single GraphQL query
    /// </summary>
    public async Task LoadAllDataForCacheAsync()
    {
        var startTime = DateTime.Now;
        _logger.LogInformation("API - Starting initialization with single GraphQL query for all data");

        var cachedData = await _cacheService.LoadDataFromFileAsync<TarkovCacheData>("cache_data.json");
        _logger.LogInformation($"CACHE - Cache data loaded in {(DateTime.Now - startTime).TotalMilliseconds}ms");

        // Check if we need to load data from API (null, empty cache, or stale cache)
        bool needsApiLoad = cachedData == null ||
                          (cachedData.Items?.Count == 0 && cachedData.Traders?.Count == 0) ||
                          (cachedData.LastUpdate != default(DateTime) && DateTime.UtcNow - cachedData.LastUpdate > _cacheValidityPeriod);

        if (needsApiLoad)
        {
            string reason = cachedData == null ? "no cache data exists" :
                           (cachedData.Items?.Count == 0 && cachedData.Traders?.Count == 0) ? "cache data is empty" :
                           $"cache data is stale (>{_cacheValidityPeriod.TotalMinutes} minutes old)";
            _logger.LogInformation($"API - Fetching fresh data from API because: {reason}");

            if (_httpClient != null)
            {
                // Single GraphQL query for all data (fixed for tarkov.dev schema)
                var query = @"{
  items {
    id
    name
    shortName
    basePrice
    lastLowPrice
    avg24hPrice
    changeLast48hPercent
    fleaMarketFee
    iconLink
    updated
    width
    height
    sellFor {
      price
      currency
      priceRUB
      vendor {
        name
      }
    }
    buyFor {
      price
      currency
      priceRUB
      vendor {
        name
      }
    }
  }
  traders {
    name
    imageLink
  }
}";

                var request = new { query };

                var apiCallStart = DateTime.Now;
                try
                {
                    _logger.LogInformation("API - Making GraphQL call to load all data");

                    var response = await _httpClient.PostAsJsonAsync("graphql", request);
                    var apiCallTime = DateTime.Now - apiCallStart;

                    if (response.IsSuccessStatusCode)
                    {
                        // Read raw response
                        var rawJson = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"API - GraphQL response length: {rawJson.Length} chars");

                        var items = new List<TarkovItem>();
                        var traders = new List<TraderInfo>();
                        TimeSpan deserializationTime = TimeSpan.Zero;

                        if (!string.IsNullOrEmpty(rawJson))
                        {
                            var deserializationStart = DateTime.Now;
                            var result = JsonSerializer.Deserialize<GraphQLResponse<CombinedDataResponse>>(rawJson, _jsonOptions);
                            deserializationTime = DateTime.Now - deserializationStart;

                            items = result?.Data?.Items ?? new List<TarkovItem>();
                            traders = result?.Data?.Traders ?? new List<TraderInfo>();

                            // Ensure items and traders are never null for logging
                            items ??= new List<TarkovItem>();
                            traders ??= new List<TraderInfo>();

                            // Update price entry slots for all items
                            foreach (var item in items)
                            {
                                item.UpdatePriceEntrySlots();
                            }

                            _logger.LogInformation($"API - Deserialized data - Items: {items.Count}, Traders: {traders.Count}");

                        cachedData = new TarkovCacheData { Items = items, Traders = traders, LastUpdate = DateTime.UtcNow };
                        }
                        else
                        {
                            _logger.LogError("API - Empty response from GraphQL");
                            cachedData = new TarkovCacheData { Items = new List<TarkovItem>(), Traders = new List<TraderInfo>() };
                        }

                        // Cache to file
                        var cacheSaveStart = DateTime.Now;
                        await _cacheService.SaveDataToFileAsync("cache_data.json", cachedData);
                        var cacheSaveTime = DateTime.Now - cacheSaveStart;

                        _logger.LogInformation($"API - Received {items.Count} items and {traders.Count} traders - DATA FETCHED FROM API");
                        _logger.LogInformation($"API - Total API call time: {(apiCallTime.TotalMilliseconds):F2}ms, Deserialization: {(deserializationTime.TotalMilliseconds):F2}ms, File save: {(cacheSaveTime.TotalMilliseconds):F2}ms");

                        await _logger.LogAPICallAsync("GraphQL_AllData", apiCallTime, true);
                        await _logger.LogPerformanceAsync("JSON_Deserialization", deserializationTime, $"Items: {items.Count}, Traders: {traders.Count}");
                        await _logger.LogPerformanceAsync("File_Cache_Save", cacheSaveTime, "cache_data.json");
                        await _logger.LogPerformanceAsync("Total_LoadAllData", DateTime.Now - startTime, "Initialization completed");
                    }
                    else
                    {
                        await _logger.LogAPICallAsync("GraphQL_AllData", apiCallTime, false, $"Status: {response.StatusCode}");
                        _logger.LogError($"API call failed with status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogAPICallAsync("GraphQL_AllData", DateTime.Now - apiCallStart, false, ex.Message);
                    _logger.LogError("Error loading all data from API", ex);
                }
            }
            else
            {
                _logger.LogError("Cannot load data - HTTP client not available");
            }
        }
        else
        {
            string lastUpdateInfo = cachedData!.LastUpdate != default(DateTime)
                ? $" (Last updated: {cachedData.LastUpdate:yyyy-MM-dd HH:mm:ss UTC}, age: {(DateTime.UtcNow - cachedData.LastUpdate).TotalMinutes:F1} minutes)"
                : " (no timestamp available)";

            _logger.LogInformation($"CACHE - Using existing cache data with {cachedData.Items?.Count ?? 0} items and {cachedData.Traders?.Count ?? 0} traders{lastUpdateInfo}");
            await _logger.LogPerformanceAsync("Cache_Load", DateTime.Now - startTime, $"Existing data loaded{lastUpdateInfo}");
        }
    }

    /// <summary>
    /// Gets all cached Tarkov data
    /// </summary>
    public async Task<TarkovCacheData> GetAllCachedDataAsync()
    {
        var cachedData = await _cacheService.LoadDataFromFileAsync<TarkovCacheData>("cache_data.json");

        // If cache doesn't exist or is empty, load from API first
        if (cachedData == null || (cachedData.Items?.Count == 0 && cachedData.Traders?.Count == 0))
        {
            await LoadAllDataForCacheAsync();
            cachedData = await _cacheService.LoadDataFromFileAsync<TarkovCacheData>("cache_data.json");

            // Return loaded data or empty if still failed
            return cachedData ?? new TarkovCacheData { Items = new List<TarkovItem>(), Traders = new List<TraderInfo>() };
        }

        // Ensure price entry slots are updated for existing cached data
        if (cachedData?.Items != null)
        {
            foreach (var item in cachedData.Items)
            {
                item.UpdatePriceEntrySlots();
            }
        }

        return cachedData!;
    }

    private async Task CacheTraderAvatarsAsync(IEnumerable<TraderInfo> traders)
    {
        foreach (var trader in traders)
        {
            if (!string.IsNullOrEmpty(trader.ImageLink) && !string.IsNullOrEmpty(trader.Id))
            {
                var cachedPath = _cacheService.GetCachedImagePath("traders", $"{trader.Id}.png");
                if (cachedPath == null)
                {
                    await _cacheService.DownloadAndCacheImageAsync(trader.ImageLink, "traders", $"{trader.Id}.png");
                }
            }
        }
    }

    /// <summary>
    /// Fetches trader data for avatar URLs from cached data
    /// </summary>
    public async Task LoadTraderAvatarsAsync()
    {
        // Ensure cache is loaded
        var cachedData = await _cacheService.LoadDataFromFileAsync<TarkovCacheData>("cache_data.json");

        if (cachedData?.Traders != null)
        {
            // Cache trader avatar images
            await CacheTraderAvatarsAsync(cachedData.Traders);
        }
        else
        {
            // Fallback: load if no cached data
            await LoadAllDataForCacheAsync();

            // Retry with loaded data
            cachedData = await _cacheService.LoadDataFromFileAsync<TarkovCacheData>("cache_data.json");
            if (cachedData?.Traders != null)
            {
                await CacheTraderAvatarsAsync(cachedData.Traders);
            }
        }
    }

    /// <summary>
    /// Gets the trader avatar path from cache, loading traders if necessary
    /// </summary>
    public async Task<string?> GetTraderAvatarUrlAsync(string traderName)
    {
        // Ensure traders are loaded
        await LoadTraderAvatarsAsync();

        // Find trader by name to get ID
        var cachedData = await GetAllCachedDataAsync();
        var trader = cachedData?.Traders?.FirstOrDefault(t => t.Name == traderName);

        if (trader?.Id != null)
        {
            // Get cached local image path using trader ID
            return _cacheService.GetCachedImagePath("traders", $"{trader.Id}.png");
        }

        return null;
    }

    /// <summary>
    /// Clears all cache entries
    /// </summary>
    public void ClearAllCache()
    {
        _ = _cacheService.ClearAllCacheAsync();
    }

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    public (int totalItemsCached, int totalListsCached) GetCacheStatistics()
    {
        // Note: This is a simplified version. Real implementation would track counts
        return (0, 0);
    }
}
