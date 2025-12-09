﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using TarkovLootScanner.Models;
using TarkovLootScanner.Services;

namespace TarkovLootScanner.UI;

/// <summary>
/// Interaction logic for OverlayWindow.xaml
/// </summary>
public partial class OverlayWindow : Window, INotifyPropertyChanged
{
    private readonly ITarkovApiService _apiService;
    private readonly IPriceCalculationService _priceService;
    private readonly ICacheService _cacheService;
    private TarkovItem? _currentItem;
    public string ItemIdentifier { get; set; }
    // Debug flag: when true, allows dragging the overlay to reposition it.
    public bool IsDebugDraggable { get; set; } = true;

    public OverlayWindow(string itemIdentifier, ITarkovApiService apiService, IPriceCalculationService priceService, ICacheService cacheService)
    {
        ItemIdentifier = itemIdentifier;
        _apiService = apiService;
        _priceService = priceService;
        _cacheService = cacheService;

        var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
        logger.LogInformation($"OverlayWindow - Created for item: {itemIdentifier}");

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

    private void InitializePriceChart()
    {
        if (CurrentItem?.HistoricalPrices == null || CurrentItem.HistoricalPrices.Count == 0)
        {
            PriceChart.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            // Parse historical prices data - timestamps are Unix milliseconds
            var historicalData = CurrentItem.HistoricalPrices
                .Where(h => !string.IsNullOrEmpty(h.Timestamp) && long.TryParse(h.Timestamp, out _))
                .Select(h =>
                {
                    if (long.TryParse(h.Timestamp, out long unixTimestampMs))
                    {
                        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        return new { HistoricalPrice = h, DateTime = epoch.AddMilliseconds(unixTimestampMs) };
                    }
                    return null;
                })
                .Where(x => x != null)
                .OrderBy(x => x!.DateTime)
                .Select(x => x!.HistoricalPrice)
                .ToList();

            if (historicalData.Count < 2)
            {
                PriceChart.Visibility = Visibility.Collapsed;
                return;
            }

            // Extract dates and prices - timestamps are Unix timestamps in milliseconds
            var dates = historicalData.Select(h =>
            {
                if (long.TryParse(h.Timestamp, out long unixTimestampMs))
                {
                    // Convert Unix timestamp in milliseconds to DateTime
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    return epoch.AddMilliseconds(unixTimestampMs);
                }
                return DateTime.MinValue;
            }).Where(d => d != DateTime.MinValue).ToArray();

            var prices = historicalData.Take(dates.Length).Select(h => (double)h.Price).ToArray();

            // Clear any existing plots
            PriceChart.Plot.Clear();

            // Create a line plot with white color - use OLE Automation dates for X axis
            var timestamps = dates.Select(d => d.ToOADate()).ToArray();
            var scatter = PriceChart.Plot.Add.Scatter(timestamps, prices);
            scatter.Color = ScottPlot.Colors.White;
            scatter.LineWidth = 2;

            // Make background transparent
            PriceChart.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
            PriceChart.Plot.DataBackground.Color = ScottPlot.Colors.Transparent;

            // Hide grid lines for cleaner look
            PriceChart.Plot.Grid.IsVisible = false;

            // Set bottom and left axes lines to white, hide top and right
            PriceChart.Plot.Axes.Bottom.FrameLineStyle.Color = ScottPlot.Colors.White;
            PriceChart.Plot.Axes.Left.FrameLineStyle.Color = ScottPlot.Colors.White;
            PriceChart.Plot.Axes.Top.FrameLineStyle.IsVisible = false;
            PriceChart.Plot.Axes.Right.FrameLineStyle.IsVisible = false;

            // Set label colors to white
            PriceChart.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Colors.White;
            PriceChart.Plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Colors.White;

            // Remove Y axis labels and title for cleaner look
            PriceChart.Plot.YLabel("");
            PriceChart.Plot.Title("");

            // Add custom X-axis labels showing time ago (hours or days)
            var now = DateTime.UtcNow;
            var tickPositions = new List<double>();
            var tickLabels = new List<string>();

            // Determine if we should show hours or days based on the time span
            var oldestDate = dates.Min();
            var newestDate = dates.Max();
            var totalSpan = newestDate - oldestDate;
            var useHours = totalSpan.TotalHours <= 48; // Show hours if data spans 48 hours or less

            // Create ticks for each data point, showing time ago from right to left
            for (int i = dates.Length - 1; i >= 0; i--)
            {
                var timeSpan = now - dates[i];
                string label;

                if (useHours)
                {
                    var hoursAgo = (int)timeSpan.TotalHours;
                    label = hoursAgo == 0 ? "0h" : $"{hoursAgo}h";
                }
                else
                {
                    var daysAgo = (int)timeSpan.TotalDays;
                    label = $"{daysAgo}d";
                }

                tickPositions.Add(timestamps[i]);
                tickLabels.Add(label);
            }

            // Set custom tick positions and labels
            PriceChart.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickPositions.ToArray(), tickLabels.ToArray());
            PriceChart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 8;

            // Disable chart interactions at WPF level
            PriceChart.IsHitTestVisible = false;

            // Auto-scale and refresh
            PriceChart.Plot.Axes.AutoScale();
            PriceChart.Refresh();

            PriceChart.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
            logger.LogError("Error initializing price chart with historical data", ex);
            PriceChart.Visibility = Visibility.Collapsed;
        }
    }

    private async void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
        var windowLoadStart = DateTime.Now;

        try
        {
            logger.LogInformation($"OverlayWindow - Starting load process for item: {ItemIdentifier}");

            // Show scanning state initially
            ScanningText.Visibility = Visibility.Visible;
            ResultsBlock.Visibility = Visibility.Collapsed;

            // Load item data from API
            await LoadItemDataAsync();

            // After loading completes, hide scanning and show results.
            ScanningText.Visibility = Visibility.Collapsed;
            ResultsBlock.Visibility = Visibility.Visible;

            await logger.LogPerformanceAsync("OverlayWindow_Load", DateTime.Now - windowLoadStart, $"Item: {ItemIdentifier}");
        }
        catch (Exception ex)
        {
            logger.LogError($"OverlayWindow - Critical error during loading sequence for item: {ItemIdentifier}", ex);

            // If loading fails, hide scanning and show results with error state
            ScanningText.Text = "Error loading item data";
            await Task.Delay(2000);
            Close(); // Close overlay on error
        }
    }

    private async Task LoadItemDataAsync()
    {
        var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
        var itemSearchStart = DateTime.Now;

        try
        {
            logger.LogInformation($"OverlayWindow - Starting item search for: {ItemIdentifier}");

            TarkovItem? item = null;

            // Try different strategies to find the item
            // First, try as ID
            if (!string.IsNullOrEmpty(ItemIdentifier) && ItemIdentifier.Length == 24 && !ItemIdentifier.Contains(" "))
            {
                logger.LogInformation($"OverlayWindow - Searching by ID: {ItemIdentifier}");
                var idSearchStart = DateTime.Now;
                item = await _apiService.GetItemByIdAsync(ItemIdentifier);
                var idSearchTime = DateTime.Now - idSearchStart;

                if (item != null)
                {
                    logger.LogInformation($"OverlayWindow - Item found by ID: {item.Name} ({item.ShortName})");
                    await logger.LogPerformanceAsync("ItemSearch_ID", idSearchTime, $"Found: {item.Name}");
                }
                else
                {
                    await logger.LogPerformanceAsync("ItemSearch_ID", idSearchTime, "Not found - trying name search");
                }
            }

            // If not found by ID or not an ID format, try as name
            if (item == null && !string.IsNullOrEmpty(ItemIdentifier))
            {
                logger.LogInformation($"OverlayWindow - Searching by name: {ItemIdentifier}");
                var nameSearchStart = DateTime.Now;
                var items = await _apiService.GetItemsByNameAsync(ItemIdentifier);
                var nameSearchTime = DateTime.Now - nameSearchStart;

                logger.LogInformation($"OverlayWindow - Name search returned {items.Count} potential matches");

                // First try exact shortName match
                item = items.FirstOrDefault(i => string.Equals(i.ShortName, ItemIdentifier, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    logger.LogInformation($"OverlayWindow - Item found by exact ShortName match: {item.Name}");
                    await logger.LogPerformanceAsync("ItemSearch_Name", nameSearchTime, $"Exact match by ShortName: {item.Name}");
                }
                else
                {
                    // Then try partial name match
                    item = items.FirstOrDefault(i => i.Name.Contains(ItemIdentifier, StringComparison.OrdinalIgnoreCase));
                    if (item != null)
                    {
                        logger.LogInformation($"OverlayWindow - Item found by partial name match: {item.Name}");
                        await logger.LogPerformanceAsync("ItemSearch_Name", nameSearchTime, $"Partial match: {item.Name}");
                    }
                    else
                    {
                        await logger.LogPerformanceAsync("ItemSearch_Name", nameSearchTime, $"No matches found for '{ItemIdentifier}'");
                    }
                }
            }

            if (item != null)
            {
                logger.LogInformation($"OverlayWindow - Successfully loaded item: {item.Name} (ID: {item.Id})");
                CurrentItem = item;
                UpdateUI();

                await logger.LogPerformanceAsync("Total_ItemLoad", DateTime.Now - itemSearchStart, $"Success: {item.Name}");
            }
            else
            {
                logger.LogWarning($"OverlayWindow - Item not found: {ItemIdentifier}");
                await logger.LogPerformanceAsync("Total_ItemLoad", DateTime.Now - itemSearchStart, $"Failed: Item not found");

                // Show placeholder or error state
                TitleText.Text = $"Item not found: {ItemIdentifier}";
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"OverlayWindow - Error loading item data for: {ItemIdentifier}", ex);

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

        // Check if item is available on flea market
        bool isAvailableOnFlea = CurrentItem.LastLowPrice.HasValue;

        // Update title and banned tag visibility
        TitleText.Text = CurrentItem.DisplayName;
        BannedTag.Visibility = isAvailableOnFlea ? Visibility.Collapsed : Visibility.Visible;

        // Update last updated using the TimeAgo property
        UpdatedText.Text = CurrentItem.TimeAgo;

        // Update item image
        if (!string.IsNullOrEmpty(CurrentItem.IconLink))
        {
            _ = LoadItemImageAsync(CurrentItem);
        }

        // Hide prices section if banned from flea
        ResultsPrice.Visibility = isAvailableOnFlea ? Visibility.Visible : Visibility.Collapsed;

        // Show prices only if available on flea
        if (isAvailableOnFlea)
        {
            PriceResultText.Text = CurrentItem.FormattedLastLowPrice;
            LowAvg24HResultText.Text = CurrentItem.Avg24hPrice.HasValue
                ? CurrentItem.FormattedAvg24hPrice
                : "N/A";
        }

        // Update trader section
        var (traderName, priceEntry) = _priceService.GetBestTraderPrice(CurrentItem);
        if (priceEntry != null)
        {
            TraderPriceText.Text = traderName;
            int totalSlots = (CurrentItem.Width ?? 1) * (CurrentItem.Height ?? 1);
            TraderPriceResultText.Text = _priceService.FormatPrice(priceEntry.PriceRUB, totalSlots);

            // Load trader icon asynchronously using API-obtained URLs
            _ = LoadTraderAvatarAsync(traderName);

            var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
            logger.LogInformation($"OverlayWindow - Best trader: {traderName} at {priceEntry.FormattedPrice}");
        }
        else
        {
            TraderPriceText.Text = "No trader";
            TraderPriceResultText.Text = "N/A";
        }

        // Hide profit section if banned from flea - no profit calculation possible for banned items
        ResultsProfit.Visibility = isAvailableOnFlea ? Visibility.Visible : Visibility.Collapsed;

        // Control separator visibility based on sections being shown
        if (isAvailableOnFlea)
        {
            // All sections visible: show all separators
            SeparatorTitleToProfit.Visibility = Visibility.Visible;
            SeparatorProfitToPrice.Visibility = Visibility.Visible;
            SeparatorPriceToTrader.Visibility = Visibility.Visible;
        }
        else
        {
            // Only trader section visible: hide intermediate separators, show only trader separator
            SeparatorTitleToProfit.Visibility = Visibility.Collapsed;
            SeparatorProfitToPrice.Visibility = Visibility.Collapsed;
            SeparatorPriceToTrader.Visibility = Visibility.Visible;
        }

        // Show profit only if available on flea
        if (isAvailableOnFlea)
        {
            var (profit, profitDesc) = _priceService.CalculateProfit(CurrentItem);
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

        // Price chart disabled for now - future feature
        InitializePriceChart();
        PriceChart.Visibility = Visibility.Collapsed;
    }

    private async Task LoadTraderAvatarAsync(string traderName)
    {
        var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
        var avatarLoadStart = DateTime.Now;

        try
        {
            logger.LogInformation($"OverlayWindow - Loading trader avatar for: {traderName}");

            // Get cached local path for trader avatar
            var cachedPath = _cacheService.GetCachedImagePath("traders", $"{traderName.ToLower().Replace(" ", "_").Replace("'", "")}.png");

            if (!string.IsNullOrEmpty(cachedPath))
            {
                // Image is already cached locally, load it
                await Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(cachedPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        TraderImage.Source = bitmap;
                        logger.LogInformation($"OverlayWindow - Trader avatar loaded from cache for {traderName}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"OverlayWindow - Error loading cached trader avatar for {traderName}: {ex.Message}");
                    }
                });
                return;
            }

            // Image not cached, get URL and download
            var traderAvatarUrl = await _apiService.GetTraderAvatarUrlAsync(traderName);
            logger.LogInformation($"OverlayWindow - Trader avatar URL for {traderName}: {traderAvatarUrl}");

            if (string.IsNullOrEmpty(traderAvatarUrl))
            {
                logger.LogWarning($"OverlayWindow - No avatar URL found for trader {traderName}");
                return;
            }

            // Download and cache the image using cache service
            cachedPath = await _cacheService.DownloadAndCacheImageAsync(traderAvatarUrl, "traders", $"{traderName.ToLower().Replace(" ", "_").Replace("'", "")}.png");

            if (!string.IsNullOrEmpty(cachedPath))
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(cachedPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        TraderImage.Source = bitmap;
                        logger.LogInformation($"OverlayWindow - Trader avatar downloaded and cached for {traderName}");

                        await logger.LogPerformanceAsync($"TraderAvatar_Download_{traderName}", DateTime.Now - avatarLoadStart, "Downloaded and cached");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"OverlayWindow - Error loading downloaded trader avatar for {traderName}: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"OverlayWindow - Error loading trader avatar for {traderName}", ex);
            // Keep placeholder on failure
        }
    }

    private async Task LoadItemImageAsync(TarkovItem item)
    {
        if (string.IsNullOrEmpty(item.IconLink))
            return;

        var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
        var imageLoadStart = DateTime.Now;

        logger.LogInformation($"OverlayWindow - Loading item image for: {item.ShortName}");

        try
        {
            // Get cached local path for item image
            var cachedPath = _cacheService.GetCachedImagePath("items", $"{item.Id}.png");

            if (!string.IsNullOrEmpty(cachedPath))
            {
                // Image is already cached locally, load it
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(cachedPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ItemImage.Source = bitmap;
                        logger.LogInformation($"OverlayWindow - Item image loaded from cache for {item.ShortName}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"OverlayWindow - Error loading cached item image for {item.ShortName}: {ex.Message}");
                    }
                });
                return;
            }

            // Image not cached, download it
            cachedPath = await _cacheService.DownloadAndCacheImageAsync(item.IconLink, "items", $"{item.Id}.png");

            if (!string.IsNullOrEmpty(cachedPath))
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(cachedPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ItemImage.Source = bitmap;
                        logger.LogInformation($"OverlayWindow - Item image downloaded and cached for {item.ShortName}");

                        await logger.LogPerformanceAsync($"ItemImage_Download_{item.ShortName}", DateTime.Now - imageLoadStart, "Downloaded and cached");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"OverlayWindow - Error loading downloaded item image for {item.ShortName}: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"OverlayWindow - Error loading item image for {item.ShortName}", ex);
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
                var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
                logger.LogWarning($"OverlayWindow - Error during DragMove: {ex.Message}");
            }
            catch (Exception ex)
            {
                var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
                logger.LogError($"OverlayWindow - Unexpected DragMove error", ex);
            }
        }
    }
}
