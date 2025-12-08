using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TarkovLootScanner.UI;

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
                // Swallow/log the exception to avoid crashing the UI thread during drag
                System.Diagnostics.Debug.WriteLine($"Error during DragMove: {ex}");
            }
            catch (Exception ex)
            {
                // Fallback catch to ensure no unexpected exceptions escape this fire-and-forget handler
                System.Diagnostics.Debug.WriteLine($"Unexpected error during DragMove: {ex}");
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

    private void OpenOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;

        // If overlay is not created or not visible, create and show it
        if (_overlayWindow == null || !_overlayWindow.IsVisible)
        {
            // Default item identifier for demo - can be made configurable later
            string itemIdentifier = "Salewa"; // Salewa shortName for testing

            _overlayWindow = new OverlayWindow(itemIdentifier);
            _overlayWindow.Closed += (_, _) =>
            {
                try
                {
                    _overlayWindow = null;
                    if (button != null)
                    {
                        button.Content = "Open Overlay";
                    }
                }
                catch (Exception ex)
                {
                    // Swallow/log the exception to avoid crashing the UI thread
                    System.Diagnostics.Debug.WriteLine($"Error in overlay Closed handler: {ex}");
                }
            };

            _overlayWindow.Show();

            if (button != null)
            {
                button.Content = "Close Overlay";
            }
        }
        else
        {
            // Toggle off: close the overlay
            _overlayWindow.Close();

            if (button != null)
            {
                button.Content = "Open Overlay";
            }
        }
    }
}
