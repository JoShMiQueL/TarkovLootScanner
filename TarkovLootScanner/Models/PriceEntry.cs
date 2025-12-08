namespace TarkovLootScanner.Models;

/// <summary>
/// Represents a price entry for buying/selling an item
/// </summary>
public class PriceEntry
{
    public int Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int PriceRUB { get; set; }
    public Vendor Vendor { get; set; } = new Vendor();

    // Helper properties for display
    public string FormattedPrice => TotalSlots == 1 ?
        $"{PriceRUB:N0}₽" :
        $"{PriceRUB:N0}₽ ({Math.Round((double)PriceRUB / TotalSlots, 0):N0}₽)";

    public int TotalSlots { get; set; } = 1; // Will be set by parent item
}

/// <summary>
/// Represents a vendor (trader, flea market, etc.)
/// </summary>
public class Vendor
{
    public string Name { get; set; } = string.Empty;
    public string? ImageLink { get; set; } // Some vendors might have images
}
