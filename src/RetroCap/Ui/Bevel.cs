using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RetroCap.Ui
{
    public enum BevelKind
    {
        None,
        /// <summary>Raised: white top/left, dark bottom/right (--bevel-out).</summary>
        Out,
        /// <summary>Raised with 1px black hairline (--bevel-out-strong).</summary>
        OutStrong,
        /// <summary>Sunken: dark top/left, white bottom/right (--bevel-in).</summary>
        In,
        /// <summary>Sunken with 1px black hairline (--bevel-in-strong).</summary>
        InStrong,
        /// <summary>Group panel: outer dark line, inner white line (--bevel-fieldset).</summary>
        Fieldset,
        /// <summary>Flat 1px mid line (--bevel-flat).</summary>
        Flat,
        /// <summary>1px black hairline only (tooltips).</summary>
        Hairline,
    }

    /// <summary>
    /// Draws the Retro SAP GUI 1px two-tone bevel around its child. Corner radius is always 0
    /// and there are no shadows: depth comes from the bevel lines alone.
    /// </summary>
    public class Bevel : Decorator
    {
        public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
            nameof(Kind), typeof(BevelKind), typeof(Bevel),
            new FrameworkPropertyMetadata(BevelKind.Out, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackgroundProperty = Panel.BackgroundProperty.AddOwner(
            typeof(Bevel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
            nameof(Padding), typeof(Thickness), typeof(Bevel),
            new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public BevelKind Kind { get => (BevelKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
        public Brush? Background { get => (Brush?)GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
        public Thickness Padding { get => (Thickness)GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

        static Bevel()
        {
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(Bevel), new FrameworkPropertyMetadata(true));
        }

        private static readonly Brush Light = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)));
        private static readonly Brush Dark = Freeze(new SolidColorBrush(Color.FromRgb(0x6E, 0x7A, 0x89)));
        private static readonly Brush Mid = Freeze(new SolidColorBrush(Color.FromRgb(0xA6, 0xB4, 0xC5)));
        private static readonly Brush Black = Freeze(new SolidColorBrush(Colors.Black));

        private static Brush Freeze(Brush b) { b.Freeze(); return b; }

        private double BorderWidth => Kind switch
        {
            BevelKind.None => 0,
            BevelKind.OutStrong or BevelKind.InStrong or BevelKind.Fieldset => 2,
            _ => 1,
        };

        protected override Size MeasureOverride(Size constraint)
        {
            double b = BorderWidth;
            var p = Padding;
            double w = 2 * b + p.Left + p.Right, h = 2 * b + p.Top + p.Bottom;
            if (Child == null) return new Size(w, h);
            Child.Measure(new Size(Math.Max(0, constraint.Width - w), Math.Max(0, constraint.Height - h)));
            return new Size(Child.DesiredSize.Width + w, Child.DesiredSize.Height + h);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double b = BorderWidth;
            var p = Padding;
            Child?.Arrange(new Rect(b + p.Left, b + p.Top,
                Math.Max(0, arrangeSize.Width - 2 * b - p.Left - p.Right),
                Math.Max(0, arrangeSize.Height - 2 * b - p.Top - p.Bottom)));
            return arrangeSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0) return;
            var r = new Rect(0, 0, w, h);

            if (Background != null) dc.DrawRectangle(Background, null, r);

            switch (Kind)
            {
                case BevelKind.Out:
                    Edge(dc, r, Light, Dark);
                    break;
                case BevelKind.OutStrong:
                    Frame(dc, r, Black);
                    Edge(dc, Deflate(r), Light, Dark);
                    break;
                case BevelKind.In:
                    Edge(dc, r, Dark, Light);
                    break;
                case BevelKind.InStrong:
                    Frame(dc, r, Black);
                    Edge(dc, Deflate(r), Dark, Light);
                    break;
                case BevelKind.Fieldset:
                    Frame(dc, r, Dark);
                    Frame(dc, Deflate(r), Light);
                    break;
                case BevelKind.Flat:
                    Frame(dc, r, Mid);
                    break;
                case BevelKind.Hairline:
                    Frame(dc, r, Black);
                    break;
            }
        }

        private static Rect Deflate(Rect r) => new Rect(r.X + 1, r.Y + 1, Math.Max(0, r.Width - 2), Math.Max(0, r.Height - 2));

        private static void Frame(DrawingContext dc, Rect r, Brush b) => Edge(dc, r, b, b);

        /// <summary>1px top/left lines in <paramref name="tl"/>, bottom/right in <paramref name="br"/>.</summary>
        private static void Edge(DrawingContext dc, Rect r, Brush tl, Brush br)
        {
            if (r.Width < 1 || r.Height < 1) return;
            dc.DrawRectangle(tl, null, new Rect(r.X, r.Y, r.Width, 1));
            dc.DrawRectangle(tl, null, new Rect(r.X, r.Y, 1, r.Height));
            dc.DrawRectangle(br, null, new Rect(r.X, r.Bottom - 1, r.Width, 1));
            dc.DrawRectangle(br, null, new Rect(r.Right - 1, r.Y, 1, r.Height));
        }
    }
}
