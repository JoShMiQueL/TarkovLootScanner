using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TarkovLootScanner.UI
{
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
		}

		private void OverlayWindow_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!IsDebugDraggable)
			{
				return;
			}

			if (e.ChangedButton == MouseButton.Left)
			{
				DragMove();
			}
		}
	}
}
