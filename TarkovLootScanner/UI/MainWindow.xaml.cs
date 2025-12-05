using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TarkovLootScanner.UI;

namespace TarkovLootScanner
{
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
                DragMove();
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
            StatusText.Text = message;
        }

        private void OpenOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            // If overlay is not created or not visible, create and show it
            if (_overlayWindow == null || !_overlayWindow.IsVisible)
            {
                _overlayWindow = new OverlayWindow();
                _overlayWindow.Closed += (_, _) =>
                {
                    _overlayWindow = null;
                    if (button != null)
                    {
                        button.Content = "Open Overlay";
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
}