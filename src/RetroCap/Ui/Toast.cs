using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RetroCap.Ui
{
    /// <summary>Non-activating SAP message strip in the bottom-right corner (.sap-message).</summary>
    public sealed class Toast : Window
    {
        private static Toast? _current;

        private Toast(string text, bool error)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            var res = Application.Current;
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new Image
            {
                Source = (ImageSource)res.FindResource(error ? "Icon.error" : "Icon.success"),
                Style = (Style)res.FindResource("Sap.Icon"), Width = 14, Height = 14,
            });
            row.Children.Add(new TextBlock
            {
                Text = text, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center,
                FontFamily = (FontFamily)res.FindResource("Sap.Font"), FontSize = 11,
                Foreground = error ? Brushes.White : Brushes.Black,
            });

            // Status-bordered strip inside a raised frame.
            var strip = new Border
            {
                Background = (Brush)res.FindResource(error ? "Sap.StatusError" : "Sap.StatusSuccessBg"),
                BorderBrush = (Brush)res.FindResource(error ? "Sap.StatusError" : "Sap.StatusSuccess"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 10, 4),
                Child = row,
            };
            Content = new Bevel { Kind = BevelKind.OutStrong, Background = (Brush)res.FindResource("Sap.Surface"), Padding = new Thickness(3), Child = strip };
        }

        public static void Show(string text)
        {
            _current?.Close();
            var t = new Toast(text, text.StartsWith("Error", StringComparison.Ordinal));
            _current = t;
            t.Loaded += (_, _) =>
            {
                var area = SystemParameters.WorkArea;
                t.Left = area.Right - t.ActualWidth - 12;
                t.Top = area.Bottom - t.ActualHeight - 12;
            };
            t.Left = -10000;
            t.Show();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
            timer.Tick += (_, _) => { timer.Stop(); t.Close(); if (_current == t) _current = null; };
            timer.Start();
        }
    }
}
