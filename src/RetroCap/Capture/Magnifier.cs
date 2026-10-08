using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RetroCap.Services;
using RetroCap.Ui;

namespace RetroCap.Capture
{
    /// <summary>
    /// Pixel loupe that follows the cursor while selecting: 4x nearest-neighbour zoom with a
    /// crosshair, plus position and colour readouts, framed as a small SAP panel.
    /// </summary>
    public sealed class Magnifier : Bevel
    {
        private const int Zoom = 4, ViewW = 128, ViewH = 96;
        private readonly ScreenSnapshot _shot;
        private readonly ImageBrush _brush;
        private readonly TextBlock _pos = Mono(), _rgb = Mono(), _hex = Mono();
        private readonly Rectangle _swatch = new() { Width = 10, Height = 10, Stroke = Brushes.Black, StrokeThickness = 1 };

        public Magnifier(ScreenSnapshot shot)
        {
            _shot = shot;
            Kind = BevelKind.OutStrong;
            Background = (Brush)Application.Current.FindResource("Sap.Surface");
            Padding = new Thickness(3);

            _brush = new ImageBrush(shot.Image) { ViewboxUnits = BrushMappingMode.Absolute, Stretch = Stretch.Fill };
            var view = new Rectangle { Width = ViewW, Height = ViewH, Fill = _brush };
            RenderOptions.SetBitmapScalingMode(view, BitmapScalingMode.NearestNeighbor);

            // Crosshair one zoomed pixel wide, in translucent SAP blue.
            var cross = new SolidColorBrush(Color.FromArgb(0x90, 0x2A, 0x6F, 0xB8));
            var grid = new Grid { Width = ViewW, Height = ViewH, ClipToBounds = true };
            grid.Children.Add(view);
            grid.Children.Add(new Rectangle { Width = Zoom, Fill = cross, HorizontalAlignment = HorizontalAlignment.Center });
            grid.Children.Add(new Rectangle { Height = Zoom, Fill = cross, VerticalAlignment = VerticalAlignment.Center });
            grid.Children.Add(new Rectangle { Width = Zoom + 2, Height = Zoom + 2, Stroke = Brushes.Black, StrokeThickness = 1 });

            var screen = new Bevel { Kind = BevelKind.InStrong, Child = grid };

            var hexRow = new StackPanel { Orientation = Orientation.Horizontal };
            hexRow.Children.Add(_swatch);
            hexRow.Children.Add(_hex);
            _hex.Margin = new Thickness(4, 0, 0, 0);

            var info = new StackPanel { Margin = new Thickness(2, 3, 2, 1) };
            info.Children.Add(_pos);
            info.Children.Add(_rgb);
            info.Children.Add(hexRow);

            var root = new StackPanel();
            root.Children.Add(screen);
            root.Children.Add(info);
            Child = root;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        }

        private static TextBlock Mono() => new()
        {
            FontFamily = new FontFamily("Lucida Console, Courier New"), FontSize = 10, Foreground = Brushes.Black,
        };

        /// <summary>Moves next to the cursor (flipping near screen edges) and refreshes the readout.</summary>
        public void Track(Point p, double screenW, double screenH)
        {
            int x = (int)p.X, y = (int)p.Y;
            double vw = ViewW / (double)Zoom, vh = ViewH / (double)Zoom;
            _brush.Viewbox = new Rect(x + 0.5 - vw / 2, y + 0.5 - vh / 2, vw, vh);

            var c = _shot.GetPixel(x, y);
            _pos.Text = $"POS {x,5},{y,5}";
            _rgb.Text = $"RGB {c.R,3},{c.G,3},{c.B,3}";
            _hex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            _swatch.Fill = new SolidColorBrush(c);

            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            // DesiredSize already includes the DPI LayoutTransform.
            double w = DesiredSize.Width, h = DesiredSize.Height;
            double left = p.X + 20, top = p.Y + 24;
            if (left + w > screenW) left = p.X - 20 - w;
            if (top + h > screenH) top = p.Y - 24 - h;
            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, top);
        }
    }
}
