namespace TarkovLootScanner.Models;

/// <summary>
/// Represents a Tarkov trader
/// </summary>
public class Trader
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageLink { get; set; }
    public string? ResetTime { get; set; }
}
