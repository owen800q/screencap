using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RetroCap.Capture
{
    public enum Tool { None, Rectangle, Ellipse, Arrow, Pen, Highlighter, Text, Mosaic }

    /// <summary>An annotation drawn in snapshot pixel coordinates.</summary>
    public abstract class Annotation
    {
        public Color Color { get; init; }
        public double Size { get; init; }
        public abstract void Render(DrawingContext dc);

        protected Pen MakePen(double thickness, PenLineCap cap = PenLineCap.Round, PenLineJoin join = PenLineJoin.Round)
        {
            var pen = new Pen(new SolidColorBrush(Color), thickness)
            {
                StartLineCap = cap, EndLineCap = cap, LineJoin = join,
            };
            pen.Freeze();
            return pen;
        }
    }

    /// <summary>Annotations defined by a drag from <see cref="Start"/> to <see cref="End"/>.</summary>
    public abstract class DragAnnotation : Annotation
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        protected Rect Bounds => new(Start, End);
    }

    public sealed class RectangleAnnotation : DragAnnotation
    {
        public override void Render(DrawingContext dc)
        {
            var r = Bounds;
            // Inset by half the stroke so the outline stays inside the dragged rectangle.
            double h = Size / 2;
            if (r.Width > Size && r.Height > Size) r = new Rect(r.X + h, r.Y + h, r.Width - Size, r.Height - Size);
            dc.DrawRectangle(null, MakePen(Size, PenLineCap.Square, PenLineJoin.Miter), r);
        }
    }

    public sealed class EllipseAnnotation : DragAnnotation
    {
        public override void Render(DrawingContext dc)
        {
            var r = Bounds;
            dc.DrawEllipse(null, MakePen(Size), new Point(r.X + r.Width / 2, r.Y + r.Height / 2),
                Math.Max(0, r.Width / 2 - Size / 2), Math.Max(0, r.Height / 2 - Size / 2));
        }
    }

    /// <summary>Tapered filled arrow: thin at the tail, a solid head at the end point.</summary>
    public sealed class ArrowAnnotation : DragAnnotation
    {
        public override void Render(DrawingContext dc)
        {
            Vector d = End - Start;
            double len = d.Length;
            if (len < 2) return;
            Vector u = d / len, n = new(-u.Y, u.X);

            double headLen = Math.Min(len * 0.9, 10 + Size * 3.2);
            double headHalf = headLen * 0.5;
            double shaftHalf = Math.Max(1, Size * 0.75);
            Point baseP = End - u * headLen;
            Point neck = End - u * (headLen * 0.72); // notch so the head reads as a barb

            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(Start + n * 0.6, true, true);
                c.LineTo(neck + n * shaftHalf, true, true);
                c.LineTo(baseP + n * headHalf, true, true);
                c.LineTo(End, true, true);
                c.LineTo(baseP - n * headHalf, true, true);
                c.LineTo(neck - n * shaftHalf, true, true);
                c.LineTo(Start - n * 0.6, true, true);
            }
            g.Freeze();
            var brush = new SolidColorBrush(Color);
            brush.Freeze();
            dc.DrawGeometry(brush, new Pen(brush, 1) { LineJoin = PenLineJoin.Round }, g);
        }
    }

    /// <summary>Freehand stroke; also the base for the highlighter and mosaic brushes.</summary>
    public class StrokeAnnotation : Annotation
    {
        public List<Point> Points { get; } = new();

        protected Geometry BuildGeometry()
        {
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(Points[0], false, false);
                if (Points.Count == 1) c.LineTo(Points[0] + new Vector(0.01, 0), true, true);
                else c.PolyLineTo(Points.GetRange(1, Points.Count - 1), true, true);
            }
            g.Freeze();
            return g;
        }

        public override void Render(DrawingContext dc)
        {
            if (Points.Count == 0) return;
            dc.DrawGeometry(null, MakePen(Size), BuildGeometry());
        }
    }

    /// <summary>Translucent marker. Drawn as one geometry under a single opacity so overlaps don't darken.</summary>
    public sealed class HighlighterAnnotation : StrokeAnnotation
    {
        public override void Render(DrawingContext dc)
        {
            if (Points.Count == 0) return;
            dc.PushOpacity(0.42);
            dc.DrawGeometry(null, MakePen(Size, PenLineCap.Square), BuildGeometry());
            dc.Pop();
        }
    }

    /// <summary>Paints a pre-pixelated copy of the screen along the stroke.</summary>
    public sealed class MosaicAnnotation : StrokeAnnotation
    {
        public BitmapSource? Pixelated { get; init; }

        public override void Render(DrawingContext dc)
        {
            if (Points.Count == 0 || Pixelated == null) return;
            var area = BuildGeometry().GetWidenedPathGeometry(MakePen(Size, PenLineCap.Square));
            var brush = new ImageBrush(Pixelated)
            {
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, Pixelated.PixelWidth, Pixelated.PixelHeight),
                Stretch = Stretch.Fill,
            };
            brush.Freeze();
            dc.DrawGeometry(brush, null, area);
        }
    }

    public sealed class TextAnnotation : Annotation
    {
        public static readonly FontFamily Font = new("Microsoft YaHei UI, Microsoft JhengHei UI, Microsoft Sans Serif, Segoe UI");

        public Point Origin { get; set; }
        public string Text { get; set; } = "";

        public static FormattedText Format(string text, double size, Color color, double pixelsPerDip = 1.0)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(Font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), size, brush, pixelsPerDip);
        }

        public override void Render(DrawingContext dc)
        {
            if (string.IsNullOrEmpty(Text)) return;
            dc.DrawText(Format(Text, Size, Color), Origin);
        }
    }
}
