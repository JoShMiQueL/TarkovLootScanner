using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TarkovLootScanner.Models;

namespace TarkovLootScanner.Services;

/// <summary>
/// Service for interacting with the Tarkov.dev GraphQL API
/// </summary>
public class TarkovApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public TarkovApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.tarkov.dev/")
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TarkovLootScanner/1.0");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Fetches item data by ID
    /// </summary>
    public async Task<TarkovItem?> GetItemByIdAsync(string itemId)
    {
        var query = @"{
  item(id: """ + itemId + @""") {
    id
    name
    shortName
    basePrice
    lastLowPrice
    avg24hPrice
    changeLast48hPercent
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
}";

        var request = new
        {
            query
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("graphql", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<ItemResponse>>(_jsonOptions);
            return result?.Data?.Item;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching item by ID: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fetches item data by name (searches for items containing the name)
    /// </summary>
    public async Task<List<TarkovItem>> GetItemsByNameAsync(string itemName)
    {
        var query = @"{
  items(name: """ + itemName + @""") {
    id
    name
    shortName
    basePrice
    lastLowPrice
    avg24hPrice
    changeLast48hPercent
    iconLink
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
}";

        var request = new
        {
            query
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("graphql", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<ItemsResponse>>(_jsonOptions);
            return result?.Data?.Items ?? new List<TarkovItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching items by name: {ex.Message}");
            return new List<TarkovItem>();
        }
    }

    /// <summary>
    /// Gets the best trader price for an item (highest sellTo trader price)
    /// </summary>
    public static (string traderName, PriceEntry? priceEntry) GetBestTraderPrice(TarkovItem item)
    {
        if (item.SellFor == null || !item.SellFor.Any())
            return ("N/A", null);

        // Get the highest trader price (excluding flea market)
        var bestTraderEntry = item.SellFor
            .Where(p => p.Vendor.Name != "Flea Market")
            .OrderByDescending(p => p.PriceRUB)
            .FirstOrDefault();

        return bestTraderEntry != null ? (bestTraderEntry.Vendor.Name, bestTraderEntry) : ("N/A", null);
    }

    /// <summary>
    /// Calculates the profit between flea and trader prices
    /// </summary>
    public static (int? profit, string description) CalculateProfit(TarkovItem item)
    {
        if (item.LastLowPrice == null || item.SellFor == null || !item.SellFor.Any())
            return (null, "Not enough data");

        // Find the best trader sell price (highest price traders pay for the item)
        var bestTraderEntry = item.SellFor
            .Where(p => p.Vendor.Name != "Flea Market")
            .OrderByDescending(p => p.PriceRUB)
            .FirstOrDefault();

        if (bestTraderEntry == null)
            return (null, "No trader data");

        // Calculate profit: player's flea sell price - what traders pay
        // In Tarkov, players sell items through flea market and receive payment after a small fee
        var profit = item.LastLowPrice.Value - bestTraderEntry.PriceRUB;

        // Format profit - no per-slot breakdown for single-slot items
        var totalSlots = item.TotalSlots;
        if (totalSlots == 1)
        {
            return (profit, $"{profit:N0}₽");
        }
        else
        {
            var profitPerSlot = Math.Round((double)profit / totalSlots, 0);
            return (profit, $"{profit:N0}₽ ({profitPerSlot:N0}₽)");
        }
    }
}
