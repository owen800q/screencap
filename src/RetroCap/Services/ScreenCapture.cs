using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RetroCap.Interop;

namespace RetroCap.Services
{
    /// <summary>A frozen snapshot of the whole virtual desktop, in physical pixels.</summary>
    public sealed class ScreenSnapshot
    {
        /// <summary>Virtual-screen origin (can be negative with monitors left of/above the primary).</summary>
        public Int32Rect Bounds { get; }

        /// <summary>Bgra32 pixels, 96 DPI so 1 WPF unit == 1 pixel.</summary>
        public BitmapSource Image { get; }

        public byte[] Pixels { get; }
        public int Stride => Bounds.Width * 4;

        /// <summary>Top-level window rectangles (top of z-order first), relative to <see cref="Bounds"/>.</summary>
        public IReadOnlyList<Rect> Windows { get; }

        private BitmapSource? _pixelated;

        public ScreenSnapshot(Int32Rect bounds, byte[] pixels, IReadOnlyList<Rect> windows)
        {
            Bounds = bounds;
            Pixels = pixels;
            Windows = windows;
            Image = BitmapSource.Create(bounds.Width, bounds.Height, 96, 96, PixelFormats.Bgra32, null, pixels, Stride);
            Image.Freeze();
        }

        public Color GetPixel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Bounds.Width || y >= Bounds.Height) return Colors.Black;
            int i = y * Stride + x * 4;
            return Color.FromRgb(Pixels[i + 2], Pixels[i + 1], Pixels[i]);
        }

        /// <summary>The snapshot pixelated in <paramref name="block"/>-sized cells (for the mosaic tool).</summary>
        public BitmapSource GetPixelated(int block = 12)
        {
            if (_pixelated != null) return _pixelated;
            int w = Bounds.Width, h = Bounds.Height, stride = Stride;
            var dst = new byte[Pixels.Length];
            for (int by = 0; by < h; by += block)
            {
                int bh = Math.Min(block, h - by);
                for (int bx = 0; bx < w; bx += block)
                {
                    int bw = Math.Min(block, w - bx);
                    long b = 0, g = 0, r = 0;
                    for (int y = by; y < by + bh; y++)
                    {
                        int i = y * stride + bx * 4;
                        for (int x = 0; x < bw; x++, i += 4) { b += Pixels[i]; g += Pixels[i + 1]; r += Pixels[i + 2]; }
                    }
                    int n = bw * bh;
                    byte cb = (byte)(b / n), cg = (byte)(g / n), cr = (byte)(r / n);
                    for (int y = by; y < by + bh; y++)
                    {
                        int i = y * stride + bx * 4;
                        for (int x = 0; x < bw; x++, i += 4) { dst[i] = cb; dst[i + 1] = cg; dst[i + 2] = cr; dst[i + 3] = 255; }
                    }
                }
            }
            _pixelated = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, dst, stride);
            _pixelated.Freeze();
            return _pixelated;
        }
    }

    public static class ScreenCapture
    {
        public static Int32Rect VirtualScreen => new(
            Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN),
            Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN),
            Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN),
            Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN));

        /// <summary>Grabs every monitor plus the top-level window layout, before the overlay is shown.</summary>
        public static ScreenSnapshot Capture(IntPtr? excludeWindow = null)
        {
            var vs = VirtualScreen;
            var windows = EnumerateWindows(vs, excludeWindow);

            IntPtr desktop = Native.GetDesktopWindow();
            IntPtr src = Native.GetWindowDC(desktop);
            IntPtr mem = Native.CreateCompatibleDC(src);
            IntPtr hbmp = Native.CreateCompatibleBitmap(src, vs.Width, vs.Height);
            IntPtr old = Native.SelectObject(mem, hbmp);
            try
            {
                Native.BitBlt(mem, 0, 0, vs.Width, vs.Height, src, vs.X, vs.Y, Native.SRCCOPY | Native.CAPTUREBLT);
                Native.SelectObject(mem, old);
                var raw = Imaging.CreateBitmapSourceFromHBitmap(hbmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                var bgra = new FormatConvertedBitmap(raw, PixelFormats.Bgra32, null, 0);
                var pixels = new byte[vs.Width * vs.Height * 4];
                bgra.CopyPixels(pixels, vs.Width * 4, 0);
                // GDI leaves alpha undefined; the screen is always opaque.
                for (int i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
                return new ScreenSnapshot(vs, pixels, windows);
            }
            finally
            {
                Native.DeleteObject(hbmp);
                Native.DeleteDC(mem);
                Native.ReleaseDC(desktop, src);
            }
        }

        private static List<Rect> EnumerateWindows(Int32Rect vs, IntPtr? exclude)
        {
            var list = new List<Rect>();
            var screen = new Rect(0, 0, vs.Width, vs.Height);
            Native.EnumWindows((hwnd, _) =>
            {
                if (hwnd == exclude) return true;
                if (!Native.IsWindowVisible(hwnd) || Native.IsIconic(hwnd) || Native.IsCloaked(hwnd)) return true;
                int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
                if ((ex & Native.WS_EX_TOOLWINDOW) != 0 && Native.GetWindowTextLength(hwnd) == 0) return true;
                if (!Native.TryGetVisibleBounds(hwnd, out var r) || r.Width < 8 || r.Height < 8) return true;
                var rect = new Rect(r.Left - vs.X, r.Top - vs.Y, r.Width, r.Height);
                rect.Intersect(screen);
                if (!rect.IsEmpty && rect.Width >= 8 && rect.Height >= 8) list.Add(rect);
                return true;
            }, IntPtr.Zero);
            list.Add(screen);
            return list;
        }
    }
}
