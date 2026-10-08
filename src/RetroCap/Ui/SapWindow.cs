using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RetroCap.Ui
{
    /// <summary>
    /// Window with Retro SAP GUI chrome: glossy blue title bar, bevel-out-strong frame,
    /// 16x14 beveled caption buttons. The chrome is drawn by WPF (WindowStyle=None) so
    /// it looks the same on every Windows theme.
    /// </summary>
    public class SapWindow : Window
    {
        public static readonly DependencyProperty TitleIconProperty = DependencyProperty.Register(
            nameof(TitleIcon), typeof(ImageSource), typeof(SapWindow));

        public static readonly DependencyProperty CanMinimizeProperty = DependencyProperty.Register(
            nameof(CanMinimize), typeof(bool), typeof(SapWindow), new PropertyMetadata(true));

        public ImageSource? TitleIcon { get => (ImageSource?)GetValue(TitleIconProperty); set => SetValue(TitleIconProperty, value); }
        public bool CanMinimize { get => (bool)GetValue(CanMinimizeProperty); set => SetValue(CanMinimizeProperty, value); }

        static SapWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SapWindow), new FrameworkPropertyMetadata(typeof(SapWindow)));
        }

        public SapWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            SetResourceReference(StyleProperty, typeof(SapWindow));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("PART_TitleBar") is UIElement bar)
                bar.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
            if (GetTemplateChild("PART_Close") is Button close)
                close.Click += (_, _) => Close();
            if (GetTemplateChild("PART_Min") is Button min)
                min.Click += (_, _) => WindowState = WindowState.Minimized;
        }
    }
}
