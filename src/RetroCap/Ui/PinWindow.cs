using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RetroCap.Services;

namespace RetroCap.Ui
{
    /// <summary>
    /// A screenshot pinned on top of everything (QQ "pin to screen"). Drag to move, wheel to zoom,
    /// double-click or Esc to close, right-click for Copy / Save As.
    /// </summary>
    public sealed class PinWindow : Window
    {
        private readonly BitmapSource _image;
        private readonly Image _view;
        private readonly Settings _settings;
        private double _zoom = 1;
        private bool _pressed;
        private Point _pressAt;
        private double _dpiX = 1, _dpiY = 1;

        public PinWindow(BitmapSource image, Rect pixelBounds, Settings settings)
        {
            _image = image;
            _settings = settings;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.Manual;
            UseLayoutRounding = true;
            Title = "RetroCap Pin";

            var dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow ?? this);
            _dpiX = dpi.DpiScaleX;
            _dpiY = dpi.DpiScaleY;
            // The frame is 2px; offset so the image lands exactly where it was captured.
            Left = pixelBounds.X / _dpiX - 2;
            Top = pixelBounds.Y / _dpiY - 2;

            _view = new Image { Source = image, Stretch = Stretch.Fill };
            RenderOptions.SetBitmapScalingMode(_view, BitmapScalingMode.HighQuality);
            ApplyZoom();

            Content = new Bevel { Kind = BevelKind.OutStrong, Background = Brushes.White, Child = _view };
            ToolTip = "Double-click to close. Wheel to zoom.";
            ContextMenu = BuildMenu();

            // Only start DragMove once the mouse actually moves, so plain clicks (and
            // double-click to close) are never swallowed by DragMove's modal loop.
            MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2) { Close(); return; }
                _pressAt = e.GetPosition(this);
                _pressed = true;
            };
            MouseLeftButtonUp += (_, _) => _pressed = false;
            MouseMove += (_, e) =>
            {
                if (!_pressed || e.LeftButton != MouseButtonState.Pressed) return;
                if ((e.GetPosition(this) - _pressAt).Length < 3) return;
                _pressed = false;
                DragMove();
            };
            KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
            MouseWheel += (_, e) =>
            {
                _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.1 : 1 / 1.1), 0.1, 5);
                ApplyZoom();
            };
        }

        private void ApplyZoom()
        {
            _view.Width = _image.PixelWidth * _zoom / _dpiX;
            _view.Height = _image.PixelHeight * _zoom / _dpiY;
        }

        private ContextMenu BuildMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Copy", "Icon.copy", "Ctrl+C", () => ImageOutput.CopyToClipboard(_image)));
            menu.Items.Add(Item("Save As...", "Icon.save", "Ctrl+S", () => ImageOutput.SaveWithDialog(_image, _settings.SaveFolder, this)));
            menu.Items.Add(Item("Original Size", "Icon.find", "", () => { _zoom = 1; ApplyZoom(); }));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Close", "Icon.x", "Esc", Close));
            return menu;
        }

        internal static MenuItem Item(string header, string icon, string gesture, Action onClick)
        {
            var mi = new MenuItem
            {
                Header = header,
                InputGestureText = gesture,
                Icon = new Image { Source = (ImageSource)Application.Current.FindResource(icon), Style = (Style)Application.Current.FindResource("Sap.Icon"), Width = 14, Height = 14 },
            };
            mi.Click += (_, _) => onClick();
            return mi;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            if (ctrl && e.Key == Key.C) ImageOutput.CopyToClipboard(_image);
            if (ctrl && e.Key == Key.S) ImageOutput.SaveWithDialog(_image, _settings.SaveFolder, this);
        }
    }
}
