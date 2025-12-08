﻿using System.Windows;
using System.Windows.Input;

namespace TarkovLootScanner.UI;

/// <summary>
/// Interaction logic for OverlayWindow.xaml
/// </summary>
public partial class OverlayWindow : Window
{
    // Debug flag: when true, allows dragging the overlay to reposition it.
    public bool IsDebugDraggable { get; set; } = true;

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OverlayWindow_Loaded;
    }

    private async void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var random = new Random();
            int delay = random.Next(250, 501);
            await Task.Delay(delay);

            // After the simulated scan completes, hide scanning and show results.
            ScanningText.Visibility = Visibility.Collapsed;
            ResultsBlock.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during OverlayWindow loading sequence: {ex}");
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
