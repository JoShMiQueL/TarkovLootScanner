using TarkovLootScanner.Models;

namespace TarkovLootScanner.Services;

/// <summary>
/// Implementation of price calculation and profit analysis service
/// </summary>
public class TarkovPriceService : IPriceCalculationService
{
    /// <summary>
    /// Gets the best trader price for an item (highest sellTo trader price)
    /// </summary>
    public (string traderName, PriceEntry? priceEntry) GetBestTraderPrice(TarkovItem item)
    {
        if (item.SellFor == null || !item.SellFor.Any())
            return ("N/A", null);

        // Get the highest trader price (excluding flea market)
        var bestTraderEntry = item.SellFor
            .Where(p => p.VendorName != "Flea Market")
            .OrderByDescending(p => p.PriceRUB)
            .FirstOrDefault();

        return bestTraderEntry != null ? (bestTraderEntry.VendorName, bestTraderEntry) : ("N/A", null);
    }

    /// <summary>
    /// Calculates the profit between flea and trader prices, accounting for flea market fees
    /// </summary>
    public (int? profit, string description) CalculateProfit(TarkovItem item)
    {
        if (item.LastLowPrice == null)
            return (null, "No flea market price");

        if (item.SellFor == null || !item.SellFor.Any())
            return (null, "No trader data");

        // Find the best trader sell price (highest price traders pay for the item)
        var bestTraderEntry = item.SellFor
            .Where(p => p.VendorName != "Flea Market")
            .OrderByDescending(p => p.PriceRUB)
            .FirstOrDefault();

        if (bestTraderEntry == null)
            return (null, "No trader data");

        // Calculate profit: (flea market received amount after fees) - (trader sell price)
        // In Tarkov, flea market charges fees, so players receive lastLowPrice - fleaMarketFee
        var fleaMarketReceived = item.LastLowPrice.Value;
        if (item.FleaMarketFee.HasValue)
        {
            fleaMarketReceived = item.LastLowPrice.Value - item.FleaMarketFee.Value;
        }

        var profit = fleaMarketReceived - bestTraderEntry.PriceRUB;

        // Format profit with sign (+ or -)
        var totalSlots = Math.Max(1, (item.Width ?? 1) * (item.Height ?? 1));
        if (totalSlots == 1)
        {
            var sign = profit >= 0 ? "+" : "";
            return (profit, $"{sign}{profit:N0}₽");
        }
        else
        {
            var profitPerSlot = Math.Round((double)profit / totalSlots, 0);
            var sign = profit >= 0 ? "+" : "";
            return (profit, $"{sign}{profit:N0}₽ ({profitPerSlot:N0}₽)");
        }
    }

    /// <summary>
    /// Formats price with slot consideration
    /// </summary>
    public string FormatPrice(int priceRub, int totalSlots)
    {
        if (totalSlots == 1)
        {
            return $"{priceRub:N0}₽";
        }
        else
        {
            var pricePerSlot = Math.Round((double)priceRub / totalSlots, 0);
            return $"{priceRub:N0}₽ ({pricePerSlot:N0}₽)";
        }
    }
}
