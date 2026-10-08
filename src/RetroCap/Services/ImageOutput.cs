using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RetroCap.Services
{
    public static class ImageOutput
    {
        public static string DefaultFileName() => $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        /// <summary>Clipboard can be briefly locked by clipboard managers; retry a few times.</summary>
        public static bool CopyToClipboard(BitmapSource image)
        {
            for (int i = 0; i < 5; i++)
            {
                try { Clipboard.SetImage(image); return true; }
                catch (COMException) { Thread.Sleep(60); }
                catch (ExternalException) { Thread.Sleep(60); }
            }
            return false;
        }

        public static void SavePng(BitmapSource image, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(image));
            using var fs = File.Create(path);
            enc.Save(fs);
        }

        /// <summary>Shows a Save As dialog. Returns the chosen path, or null when cancelled.</summary>
        public static string? SaveWithDialog(BitmapSource image, string folder, Window? owner)
        {
            Directory.CreateDirectory(folder);
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Screenshot",
                FileName = DefaultFileName(),
                InitialDirectory = folder,
                DefaultExt = ".png",
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap (*.bmp)|*.bmp",
            };
            if (dlg.ShowDialog(owner) != true) return null;

            BitmapEncoder enc = Path.GetExtension(dlg.FileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 92 },
                ".bmp" => new BmpBitmapEncoder(),
                _ => new PngBitmapEncoder(),
            };
            enc.Frames.Add(BitmapFrame.Create(image));
            using var fs = File.Create(dlg.FileName);
            enc.Save(fs);
            return dlg.FileName;
        }
    }
}
