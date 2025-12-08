﻿using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TarkovLootScanner.Models;
using TarkovLootScanner.Services;

namespace TarkovLootScanner.UI;

/// <summary>
/// Interaction logic for OverlayWindow.xaml
/// </summary>
public partial class OverlayWindow : Window, INotifyPropertyChanged
{
    private readonly TarkovApiService _apiService;
    private TarkovItem? _currentItem;
    public string ItemIdentifier { get; set; }
    // Debug flag: when true, allows dragging the overlay to reposition it.
    public bool IsDebugDraggable { get; set; } = true;

    public OverlayWindow(string itemIdentifier)
    {
        ItemIdentifier = itemIdentifier;
        _apiService = new TarkovApiService();
        InitializeComponent();
        Loaded += OverlayWindow_Loaded;

        // Set the DataContext for binding (helpful for future MVVM integration)
        DataContext = this;
    }

    public TarkovItem? CurrentItem
    {
        get => _currentItem;
        set
        {
            _currentItem = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentItem)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemDisplayName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemImageUrl)));
        }
    }

    public string ItemDisplayName => CurrentItem?.DisplayName ?? "Loading...";
    public string ItemImageUrl => CurrentItem?.IconLink ?? "/Assets/Placeholder.png";

    public event PropertyChangedEventHandler? PropertyChanged;

    private async void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show scanning state initially
            ScanningText.Visibility = Visibility.Visible;
            ResultsBlock.Visibility = Visibility.Collapsed;

            // Load item data from API
            await LoadItemDataAsync();

            // After loading completes, hide scanning and show results.
            ScanningText.Visibility = Visibility.Collapsed;
            ResultsBlock.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during OverlayWindow loading sequence: {ex}");

            // If loading fails, hide scanning and show results with error state
            ScanningText.Text = "Error loading item data";
            await Task.Delay(2000);
            Close(); // Close overlay on error
        }
    }

    private async Task LoadItemDataAsync()
    {
        try
        {
            TarkovItem? item = null;

            // Try different strategies to find the item
            // First, try as ID
            if (!string.IsNullOrEmpty(ItemIdentifier) && ItemIdentifier.Length == 24 && !ItemIdentifier.Contains(" "))
            {
                item = await _apiService.GetItemByIdAsync(ItemIdentifier);
            }

            // If not found by ID or not an ID format, try as name
            if (item == null && !string.IsNullOrEmpty(ItemIdentifier))
            {
                var items = await _apiService.GetItemsByNameAsync(ItemIdentifier);
                item = items.FirstOrDefault(i => string.Equals(i.ShortName, ItemIdentifier, StringComparison.OrdinalIgnoreCase)) ??
                       items.FirstOrDefault(i => i.Name.Contains(ItemIdentifier, StringComparison.OrdinalIgnoreCase));
            }

            if (item != null)
            {
                CurrentItem = item;
                UpdateUI();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Item not found: {ItemIdentifier}");
                // Show placeholder or error state
                TitleText.Text = $"Item not found: {ItemIdentifier}";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading item data: {ex.Message}");

            // Show error in UI
            TitleText.Text = "Error loading item";

            // Wait a bit then close
            await Task.Delay(2000);
            Close();
        }
    }

        private void UpdateUI()
    {
        if (CurrentItem == null) return;

        // Update title
        TitleText.Text = CurrentItem.DisplayName;

        // Update last updated using the TimeAgo property
        UpdatedText.Text = CurrentItem.TimeAgo;

        // Update item image
        if (!string.IsNullOrEmpty(CurrentItem.IconLink))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(CurrentItem.IconLink);
                bitmap.EndInit();
                ItemImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading item image: {ex.Message}");
                // Keep placeholder image
            }
        }

        // Update price section
        if (CurrentItem.LastLowPrice.HasValue)
        {
            PriceResultText.Text = CurrentItem.FormattedLastLowPrice;
        }

        if (CurrentItem.Avg24hPrice.HasValue)
        {
            LowAvg24HResultText.Text = CurrentItem.FormattedAvg24hPrice;
        }

        // Update trader section
        var (traderName, priceEntry) = TarkovApiService.GetBestTraderPrice(CurrentItem);
        if (priceEntry != null)
        {
            TraderPriceText.Text = traderName;
            TraderPriceResultText.Text = priceEntry.FormattedPrice;

            // Load trader icon asynchronously using API-obtained URLs
            _ = LoadTraderAvatarAsync(traderName);
            System.Diagnostics.Debug.WriteLine($"Best trader: {traderName} at {priceEntry.FormattedPrice}");
        }

        // Update profit section (remove "Profit" text)
        var (profit, profitDesc) = TarkovApiService.CalculateProfit(CurrentItem);
        if (profit.HasValue)
        {
            // CalculateProfit now handles per-slot formatting internally
            ProfitResultText.Text = profitDesc;
        }
        else
        {
            ProfitResultText.Text = "No profit data";
        }
    }

    private string? GetTraderAvatarUrl(string traderName)
    {
        // Map trader names to their imageLink from Tarkov.dev API
        // These URLs were obtained from querying the traders endpoint
        var traderAvatarUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Prapor"] = "https://assets.tarkov.dev/54cb50c76803fa8b248b4571.webp",
            ["Therapist"] = "https://assets.tarkov.dev/54cb57776803fa99248b456e.webp",
            ["Fence"] = "https://assets.tarkov.dev/579dc571d53a0658a154fbec.webp",
            ["Skier"] = "https://assets.tarkov.dev/58330581ace78e27b8b10cee.webp",
            ["Peacekeeper"] = "https://assets.tarkov.dev/5935c25fb3acc3127c3d8cd9.webp",
            ["Mechanic"] = "https://assets.tarkov.dev/5a7c2eca46aef81a7ca2145d.webp",
            ["Ragman"] = "https://assets.tarkov.dev/5ac3b934156ae10c4430e83c.webp",
            ["Jaeger"] = "https://assets.tarkov.dev/5c0647fdd443bc2504c2d371.webp"
        };

        if (traderAvatarUrls.TryGetValue(traderName, out var avatarUrl))
        {
            return avatarUrl;
        }

        // Try partial matches
        foreach (var kvp in traderAvatarUrls)
        {
            if (traderName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    private async Task LoadTraderAvatarAsync(string traderName)
    {
        var traderAvatarUrl = GetTraderAvatarUrl(traderName);
        System.Diagnostics.Debug.WriteLine($"Loading trader avatar for: {traderName} from URL: {traderAvatarUrl}");

        if (string.IsNullOrEmpty(traderAvatarUrl)) return;

        try
        {
            using var client = new HttpClient();
            var imageBytes = await client.GetByteArrayAsync(traderAvatarUrl);

            await Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var bitmap = new BitmapImage();
                    using var stream = new MemoryStream(imageBytes);
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    TraderImage.Source = bitmap;
                    System.Diagnostics.Debug.WriteLine("Trader avatar loaded successfully from API");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating bitmap for trader {traderName}: {ex.Message}");
                    // Keep placeholder on failure
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading trader avatar for {traderName}: {ex.Message}");
            // Keep placeholder on failure
        }
    }

    private void OverlayWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsDebugDraggable)
        {
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during Overlay DragMove: {ex}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error during Overlay DragMove: {ex}");
            }
        }
    }
}
