namespace TarkovLootScanner.Models;

/// <summary>
/// Represents a price entry for buying/selling an item
/// </summary>
public class PriceEntry
{
    public int Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int PriceRUB { get; set; }
    public string VendorName { get; set; } = string.Empty;

    // Helper properties for display
    public string FormattedPrice => TotalSlots == 1 ?
        $"{PriceRUB:N0}₽" :
        $"{PriceRUB:N0}₽ ({Math.Round((double)PriceRUB / TotalSlots, 0):N0}₽)";

    public int TotalSlots { get; set; } = 1; // Will be set by parent item
}
