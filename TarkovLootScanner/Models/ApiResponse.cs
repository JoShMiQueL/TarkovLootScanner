namespace TarkovLootScanner.Models;

/// <summary>
/// Wrapper for the GraphQL API response
/// </summary>
public class GraphQLResponse<T>
{
    public T Data { get; set; } = default!;
    public object? Errors { get; set; }
}

/// <summary>
/// Combined response for single GraphQL query with items and traders
/// </summary>
public class CombinedDataResponse
{
    public List<TarkovItem> Items { get; set; } = new List<TarkovItem>();
    public List<TraderInfo> Traders { get; set; } = new List<TraderInfo>();
}

/// <summary>
/// Represents a single trader with basic information for avatar loading
/// </summary>
public class TraderInfo
{
    // API doesn't provide ID, using Name as unique identifier
    public string Name { get; set; } = string.Empty;
    public string ImageLink { get; set; } = string.Empty;

    // Computed property for backward compatibility (Name sanitized for filename)
    public string Id => Name.Replace(" ", "_").Replace("'", "").ToLowerInvariant();
}

/// <summary>
/// Data structure for caching all Tarkov data locally
/// </summary>
public class TarkovCacheData
{
    public List<TarkovItem> Items { get; set; } = new List<TarkovItem>();
    public List<TraderInfo> Traders { get; set; } = new List<TraderInfo>();
    public DateTime LastUpdate { get; set; }
}
