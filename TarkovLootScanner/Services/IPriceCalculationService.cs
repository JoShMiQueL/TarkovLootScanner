using TarkovLootScanner.Models;

namespace TarkovLootScanner.Services;

/// <summary>
/// Interface for price calculation and profit analysis services
/// </summary>
public interface IPriceCalculationService
{
    /// <summary>
    /// Gets the best trader price for an item (highest sellTo trader price)
    /// </summary>
    (string traderName, PriceEntry? priceEntry) GetBestTraderPrice(TarkovItem item);

    /// <summary>
    /// Calculates the profit between flea and trader prices, accounting for flea market fees
    /// </summary>
    (int? profit, string description) CalculateProfit(TarkovItem item);

    /// <summary>
    /// Formats price with slot consideration
    /// </summary>
    string FormatPrice(int priceRub, int totalSlots);
}
