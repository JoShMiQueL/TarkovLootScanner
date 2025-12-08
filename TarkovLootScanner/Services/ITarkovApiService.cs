using TarkovLootScanner.Models;

namespace TarkovLootScanner.Services;

/// <summary>
/// Interface for Tarkov API service that handles item data retrieval from cache
/// </summary>
public interface ITarkovApiService
{
    /// <summary>
    /// Gets item data by ID from cache
    /// </summary>
    Task<TarkovItem?> GetItemByIdAsync(string itemId);

    /// <summary>
    /// Gets item data by name from cache (searches for items containing the name)
    /// </summary>
    Task<List<TarkovItem>> GetItemsByNameAsync(string itemName);

    /// <summary>
    /// Loads trader avatars for display
    /// </summary>
    Task LoadTraderAvatarsAsync();

    /// <summary>
    /// Gets the trader avatar path from cache
    /// </summary>
    Task<string?> GetTraderAvatarUrlAsync(string traderName);

    /// <summary>
    /// Clears all cache entries
    /// </summary>
    void ClearAllCache();

    /// <summary>
    /// Loads all data for caching with single GraphQL query
    /// </summary>
    Task LoadAllDataForCacheAsync();

    /// <summary>
    /// Gets all cached Tarkov data
    /// </summary>
    Task<TarkovCacheData> GetAllCachedDataAsync();
}
