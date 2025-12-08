﻿using System.Collections.Concurrent;
using System.ComponentModel;
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
    private readonly ConcurrentDictionary<string, BitmapImage> _traderImageCache;
    private TarkovItem? _currentItem;
    public string ItemIdentifier { get; set; }
    // Debug flag: when true, allows dragging the overlay to reposition it.
    public bool IsDebugDraggable { get; set; } = true;

    public OverlayWindow(string itemIdentifier)
    {
        ItemIdentifier = itemIdentifier;
        _apiService = new TarkovApiService();
        _traderImageCache = new ConcurrentDictionary<string, BitmapImage>();
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

    private async Task LoadTraderAvatarAsync(string traderName)
    {
        System.Diagnostics.Debug.WriteLine($"Loading trader avatar for: {traderName}");

        // Check local image cache first
        if (_traderImageCache.TryGetValue(traderName, out var cachedImage))
        {
            TraderImage.Source = cachedImage;
            System.Diagnostics.Debug.WriteLine($"Trader avatar loaded from local cache for {traderName}");
            return;
        }

        try
        {
            // Get trader avatar URL dynamically from API
            var traderAvatarUrl = await _apiService.GetTraderAvatarUrlAsync(traderName);
            System.Diagnostics.Debug.WriteLine($"Trader avatar URL for {traderName}: {traderAvatarUrl}");

            if (string.IsNullOrEmpty(traderAvatarUrl))
            {
                System.Diagnostics.Debug.WriteLine($"No avatar URL found for trader {traderName}");
                return;
            }

            // Download image
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

                    // Cache the bitmap image locally
                    _traderImageCache[traderName] = bitmap;

                    TraderImage.Source = bitmap;
                    System.Diagnostics.Debug.WriteLine($"Trader avatar loaded and cached successfully for {traderName}");
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
