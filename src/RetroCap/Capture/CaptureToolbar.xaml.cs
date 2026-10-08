using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using RetroCap.Ui;

namespace RetroCap.Capture
{
    public partial class CaptureToolbar : UserControl
    {
        public static readonly string[] PaletteColors =
        {
            "#000000", "#4A5566", "#8A95A5", "#FFFFFF", "#C8281E", "#FF5A4E", "#F0AB00", "#FFE600",
            "#2E8B3E", "#5CC46B", "#1F6BB8", "#5EA8F0", "#6B3F8A", "#B07CD8", "#003D7A", "#E87BB0",
        };

        private readonly ToggleButton[] _tools;
        private readonly ToggleButton[] _sizes;

        public event Action<Tool>? ToolSelected;
        public event Action<int>? SizeSelected;
        public event Action<Color>? ColorSelected;
        public event Action? UndoRequested, PinRequested, SaveRequested, CancelRequested, CopyRequested;

        public CaptureToolbar()
        {
            InitializeComponent();
            _tools = new[] { RectTool, EllipseTool, ArrowTool, PenTool, HighlighterTool, TextTool, MosaicTool };
            _sizes = new[] { Size0, Size1, Size2 };

            foreach (var t in _tools)
                t.Click += (s, _) =>
                {
                    var b = (ToggleButton)s;
                    var tool = b.IsChecked == true ? Enum.Parse<Tool>((string)b.Tag) : Tool.None;
                    ToolSelected?.Invoke(tool);
                };
            foreach (var s in _sizes)
                s.Click += (b, _) => SizeSelected?.Invoke(int.Parse((string)((ToggleButton)b).Tag));

            foreach (var hex in PaletteColors)
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var swatch = new Bevel
                {
                    Kind = BevelKind.In, Width = 14, Height = 14, Margin = new Thickness(0, 0, 1, 1),
                    Background = new SolidColorBrush(color), ToolTip = hex, Cursor = Cursors.Hand,
                };
                swatch.MouseLeftButtonDown += (_, e) => { e.Handled = true; ColorSelected?.Invoke(color); };
                Palette.Children.Add(swatch);
            }

            UndoButton.Click += (_, _) => UndoRequested?.Invoke();
            PinButton.Click += (_, _) => PinRequested?.Invoke();
            SaveButton.Click += (_, _) => SaveRequested?.Invoke();
            CancelButton.Click += (_, _) => CancelRequested?.Invoke();
            CopyButton.Click += (_, _) => CopyRequested?.Invoke();
            MoreColors.Click += (_, _) => PickCustomColor();

            // Keep clicks on the toolbar from reaching the overlay underneath.
            PreviewMouseRightButtonDown += (_, e) => e.Handled = true;
        }

        /// <summary>Syncs the toolbar to the overlay's state.</summary>
        public void Show(Tool tool, int sizeIndex, Color color, bool canUndo)
        {
            foreach (var t in _tools) t.IsChecked = (string)t.Tag == tool.ToString();
            for (int i = 0; i < _sizes.Length; i++) _sizes[i].IsChecked = i == sizeIndex;
            CurrentColor.Fill = new SolidColorBrush(color);
            PropertiesRow.Visibility = tool == Tool.None ? Visibility.Collapsed : Visibility.Visible;
            ColorSection.Visibility = tool == Tool.Mosaic ? Visibility.Collapsed : Visibility.Visible;
            UndoButton.IsEnabled = canUndo;

            var tips = tool == Tool.Text ? new[] { "Font Size 14", "Font Size 20", "Font Size 30" } : new[] { "Small", "Medium", "Large" };
            for (int i = 0; i < _sizes.Length; i++) _sizes[i].ToolTip = tips[i];
        }

        private void PickCustomColor()
        {
            var current = ((SolidColorBrush)CurrentColor.Fill).Color;
            using var dlg = new System.Windows.Forms.ColorDialog
            {
                FullOpen = true,
                Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B),
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ColorSelected?.Invoke(Color.FromRgb(dlg.Color.R, dlg.Color.G, dlg.Color.B));
        }
    }
}
