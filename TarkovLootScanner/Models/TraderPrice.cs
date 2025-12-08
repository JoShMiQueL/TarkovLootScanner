using System.ComponentModel;

namespace TarkovLootScanner.Models;

/// <summary>
/// Represents the price of an item from a specific trader
/// </summary>
public class TraderPrice : INotifyPropertyChanged
{
    private int _totalSlots = 1; // Will be set by parent item

    public int Price { get; set; }
    public Trader Trader { get; set; } = new Trader();

    public int TotalSlots
    {
        get => _totalSlots;
        set
        {
            _totalSlots = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedPrice)));
        }
    }

    // Helper properties for display - price per slot in parentheses
    public string FormattedPrice => TotalSlots == 1 ?
        $"{Price:N0}₽" :
        $"{Price:N0}₽ ({Math.Round((double)Price / TotalSlots, 0):N0}₽)";

    public event PropertyChangedEventHandler? PropertyChanged;
}
