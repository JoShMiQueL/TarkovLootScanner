using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TarkovLootScanner.UI;
using TarkovLootScanner.Services;

namespace TarkovLootScanner;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private OverlayWindow? _overlayWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Close overlay window when main window is closing
        if (_overlayWindow != null && _overlayWindow.IsVisible)
        {
            var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();
            logger.LogInformation("MainWindow - Closing overlay on application shutdown");
            _overlayWindow.Close();
        }

        base.OnClosing(e);
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException ex)
            {
                var logger = App.ServiceProvider?.GetService<ILoggerService>();
                logger?.LogWarning($"MainWindow - Error during DragMove: {ex.Message}");
            }
            catch (Exception ex)
            {
                var logger = App.ServiceProvider?.GetService<ILoggerService>();
                logger?.LogError($"MainWindow - Unexpected DragMove error", ex);
            }
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public void SetStatus(string message)
    {
        // Ensure StatusText is always updated on the UI thread
        if (Dispatcher.CheckAccess())
        {
            StatusText.Text = message;
        }
        else
        {
            Dispatcher.BeginInvoke(new Action(() => StatusText.Text = message));
        }
    }

    private void ToggleOverlay()
    {
        var logger = App.ServiceProvider.GetRequiredService<ILoggerService>();

        // If overlay is currently open, close it
        if (_overlayWindow != null && _overlayWindow.IsVisible)
        {
            logger.LogInformation("MainWindow - Closing existing overlay");
            _overlayWindow.Close();
            OpenOverlayButton.Content = "Open Overlay";
            SetStatus("Overlay closed");
            return;
        }

        // Otherwise, open a new overlay
        var itemIdentifier = ItemNameTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(itemIdentifier))
        {
            logger.LogWarning("MainWindow - Attempted to open overlay with empty item name");
            return;
        }

        logger.LogInformation($"MainWindow - Opening overlay for item: {itemIdentifier}");

        // Create and show new overlay
        _overlayWindow = new OverlayWindow(
            itemIdentifier,
            App.ServiceProvider.GetRequiredService<ITarkovApiService>(),
            App.ServiceProvider.GetRequiredService<IPriceCalculationService>(),
            App.ServiceProvider.GetRequiredService<ICacheService>()
        );

        _overlayWindow.Closed += (_, _) =>
        {
            try
            {
                _overlayWindow = null;
                OpenOverlayButton.Content = "Open Overlay";
                logger.LogInformation("MainWindow - Overlay closed");
                SetStatus("Overlay closed");
            }
            catch (Exception ex)
            {
                logger.LogError("MainWindow - Error in overlay Closed handler", ex);
            }
        };

        _overlayWindow.Show();
        OpenOverlayButton.Content = "Close Overlay";
        SetStatus($"Overlay opened for: {itemIdentifier}");
    }

    private void OpenOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleOverlay();
    }

    private void ItemNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ToggleOverlay();
            e.Handled = true; // Prevent the Enter key from being processed further
        }
    }
}
