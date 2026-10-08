using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using RetroCap.Services;
using RetroCap.Ui;

namespace RetroCap
{
    public partial class MainWindow : SapWindow
    {
        private readonly Settings _settings;

        /// <summary>Set by the app on exit so closing really closes instead of hiding to tray.</summary>
        public bool AllowClose { get; set; }

        public event Action? CaptureRequested;
        public event Action? DelayedCaptureRequested;
        public event Action? ExitRequested;

        public MainWindow(Settings settings)
        {
            _settings = settings;
            InitializeComponent();

            FolderBox.Text = settings.SaveFolder;
            AutoSaveCheck.IsChecked = settings.AutoSave;
            HideCheck.IsChecked = settings.HideWindowOnCapture;
            StartupCheck.IsChecked = settings.StartWithWindows;

            CaptureButton.Click += (_, _) => CaptureRequested?.Invoke();
            CaptureNowButton.Click += (_, _) => CaptureRequested?.Invoke();
            DelayButton.Click += (_, _) => DelayedCaptureRequested?.Invoke();
            HideButton.Click += (_, _) => Hide();
            ExitButton.Click += (_, _) => ExitRequested?.Invoke();
            OpenFolderButton.Click += (_, _) => OpenFolder();
            BrowseButton.Click += (_, _) => Browse();

            FolderBox.LostFocus += (_, _) => { _settings.SaveFolder = FolderBox.Text.Trim(); _settings.Save(); };
            AutoSaveCheck.Click += (_, _) => { _settings.AutoSave = AutoSaveCheck.IsChecked == true; _settings.Save(); };
            HideCheck.Click += (_, _) => { _settings.HideWindowOnCapture = HideCheck.IsChecked == true; _settings.Save(); };
            StartupCheck.Click += (_, _) =>
            {
                _settings.StartWithWindows = StartupCheck.IsChecked == true;
                Autostart.Apply(_settings.StartWithWindows);
                _settings.Save();
            };
        }

        public void SetStatus(string text) => StatusText.Text = text;

        public void SetHotkeyState(bool registered)
        {
            HotkeyTagText.Text = registered ? "Registered" : "In use by another program";
            HotkeyTag.Background = (System.Windows.Media.Brush)FindResource(registered ? "Sap.StatusSuccessBg" : "Sap.StatusWarningBg");
            HotkeyTag.BorderBrush = (System.Windows.Media.Brush)FindResource(registered ? "Sap.StatusSuccess" : "Sap.StatusError");
            if (!registered)
                HintText.Text = "Ctrl+Alt+A is taken (QQ/WeChat?). Use New Capture or the tray icon.";
        }

        private void Browse()
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Save Folder",
                UseDescriptionForTitle = true,
                SelectedPath = Directory.Exists(FolderBox.Text) ? FolderBox.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            };
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            FolderBox.Text = dlg.SelectedPath;
            _settings.SaveFolder = dlg.SelectedPath;
            _settings.Save();
            SetStatus("Save folder changed.");
        }

        private void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(_settings.SaveFolder);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_settings.SaveFolder}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // The close box hides to the notification area; Exit really quits.
            if (!AllowClose)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnClosing(e);
        }
    }
}
