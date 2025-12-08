using System.Collections.ObjectModel;

namespace TarkovLootScanner.Models;

/// <summary>
/// Represents a Tarkov item with pricing and trader information
/// </summary>
public class TarkovItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public int? BasePrice { get; set; }
    public int? LastLowPrice { get; set; }
    public int? Avg24hPrice { get; set; }
    public decimal? ChangeLast48hPercent { get; set; }
    public string? IconLink { get; set; }
    public string? Updated { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public ObservableCollection<PriceEntry>? SellFor { get; set; }
    public ObservableCollection<PriceEntry>? BuyFor { get; set; }

    // Helper properties for display
    public int TotalSlots
    {
        get
        {
            int slots = (Width ?? 1) * (Height ?? 1);
            // Update all price entries with the correct slot count
            if (SellFor != null)
            {
                foreach (var priceEntry in SellFor)
                {
                    priceEntry.TotalSlots = slots;
                }
            }
            if (BuyFor != null)
            {
                foreach (var priceEntry in BuyFor)
                {
                    priceEntry.TotalSlots = slots;
                }
            }
            return slots;
        }
    }

    public string DisplayName => TotalSlots == 1 ? ShortName : $"{ShortName} ({TotalSlots} Slot{(TotalSlots != 1 ? "s" : "")})";

    public string FormattedLastLowPrice => LastLowPrice.HasValue ?
        (TotalSlots == 1 ? $"{LastLowPrice.Value:N0}₽" : $"{LastLowPrice.Value:N0}₽ ({Math.Round((double)LastLowPrice.Value / TotalSlots, 0):N0}₽)") : "N/A";
    public string FormattedAvg24hPrice => Avg24hPrice.HasValue ?
        (TotalSlots == 1 ? $"{Avg24hPrice.Value:N0}₽" : $"{Avg24hPrice.Value:N0}₽ ({Math.Round((double)Avg24hPrice.Value / TotalSlots, 0):N0}₽)") : "N/A";
    public string FormattedBasePrice => BasePrice.HasValue ?
        (TotalSlots == 1 ? $"{BasePrice.Value:N0}₽" : $"{BasePrice.Value:N0}₽ ({Math.Round((double)BasePrice.Value / TotalSlots, 0):N0}₽)") : "N/A";

    public string TimeAgo
    {
        get
        {
            if (string.IsNullOrEmpty(Updated) || !DateTime.TryParse(Updated, out var updateTime))
                return "Updated recently";

            var timeAgo = DateTime.Now - updateTime;
            if (timeAgo.TotalMinutes < 1)
                return "Updated just now";
            if (timeAgo.TotalMinutes < 60)
                return $"Updated {timeAgo.TotalMinutes:F0} minute{(timeAgo.TotalMinutes >= 2 ? "s" : "")} ago";
            if (timeAgo.TotalHours < 24)
                return $"Updated {timeAgo.TotalHours:F0} hour{(timeAgo.TotalHours >= 2 ? "s" : "")} ago";
            return $"Updated {timeAgo.TotalDays:F0} day{(timeAgo.TotalDays >= 2 ? "s" : "")} ago";
        }
    }
}
