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
/// Wrapper for items query response
/// </summary>
public class ItemsResponse
{
    public List<TarkovItem> Items { get; set; } = new List<TarkovItem>();
}

/// <summary>
/// Wrapper for single item query response
/// </summary>
public class ItemResponse
{
    public TarkovItem Item { get; set; } = new TarkovItem();
}
