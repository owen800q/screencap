using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using RetroCap.Capture;
using RetroCap.Interop;
using RetroCap.Services;
using RetroCap.Ui;

namespace RetroCap
{
    public partial class App : Application
    {
        private const string InstanceName = "RetroCap.SingleInstance";
        private const uint VK_A = 0x41;

        private Mutex? _mutex;
        private EventWaitHandle? _wake;
        private HotkeyService? _hotkey;
        private TrayIcon? _tray;
        private MainWindow? _main;
        private CaptureWindow? _capture;
        private readonly Settings _settings = Settings.Load();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Second launch: poke the running instance (shows its window) and quit.
            _mutex = new Mutex(true, InstanceName, out bool first);
            if (!first)
            {
                try { EventWaitHandle.OpenExisting(InstanceName + ".Wake").Set(); } catch (WaitHandleCannotBeOpenedException) { }
                Shutdown();
                return;
            }
            _wake = new EventWaitHandle(false, EventResetMode.AutoReset, InstanceName + ".Wake");
            new Thread(() => { while (_wake.WaitOne()) Dispatcher.BeginInvoke(ShowMain); }) { IsBackground = true }.Start();

            _main = new MainWindow(_settings);
            MainWindow = _main;
            _main.CaptureRequested += () => StartCapture();
            _main.DelayedCaptureRequested += async () => { _main.Hide(); await Task.Delay(3000); StartCapture(forceHide: true); };
            _main.ExitRequested += ExitApp;

            _tray = new TrayIcon(() => StartCapture(), ShowMain, ExitApp);

            _hotkey = new HotkeyService();
            _hotkey.Pressed += (_, _) => StartCapture();
            bool registered = _hotkey.Register(Native.MOD_CONTROL | Native.MOD_ALT, VK_A);
            _main.SetHotkeyState(registered);
            _main.SetStatus(registered ? "Ready. Hotkey Ctrl+Alt+A registered." : "Warning: hotkey Ctrl+Alt+A could not be registered.");

            if (!e.Args.Contains("--tray")) _main.Show();
            if (e.Args.Contains("--capture")) StartCapture();
        }

        private void ShowMain()
        {
            if (_main == null) return;
            _main.Show();
            if (_main.WindowState == WindowState.Minimized) _main.WindowState = WindowState.Normal;
            _main.Activate();
        }

        private async void StartCapture(bool forceHide = false)
        {
            if (_capture != null || _main == null) return;

            bool restore = false;
            if (_main.IsVisible && (_settings.HideWindowOnCapture || forceHide))
            {
                _main.Hide();
                restore = !forceHide;
                await Task.Delay(300); // let the compositor drop the window before grabbing pixels
            }

            ScreenSnapshot shot;
            try
            {
                shot = ScreenCapture.Capture(new WindowInteropHelper(_main).Handle);
            }
            catch (Exception ex)
            {
                Toast.Show("Error: screen capture failed. " + ex.Message);
                if (restore) _main.Show();
                return;
            }

            _capture = new CaptureWindow(shot, _settings);
            _capture.Completed += msg =>
            {
                _main.SetStatus(msg);
                Toast.Show(msg);
            };
            _capture.PinRequested += (img, at) =>
            {
                new PinWindow(img, at, _settings).Show();
                _main.SetStatus($"Image pinned to screen. {img.PixelWidth} x {img.PixelHeight}");
            };
            _capture.Closed += (_, _) =>
            {
                _capture = null;
                if (restore) _main.Show();
            };
            _capture.Show();
        }

        private void ExitApp()
        {
            _hotkey?.Dispose();
            _tray?.Dispose();
            if (_main != null) { _main.AllowClose = true; _main.Close(); }
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hotkey?.Dispose();
            _tray?.Dispose();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
