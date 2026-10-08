using System;
using System.Windows.Interop;
using RetroCap.Interop;

namespace RetroCap.Services
{
    /// <summary>System-wide hotkey via RegisterHotKey on a message-only window.</summary>
    public sealed class HotkeyService : IDisposable
    {
        private const int Id = 0x5243; // "RC"
        private readonly HwndSource _source;
        private bool _registered;

        public event EventHandler? Pressed;

        public HotkeyService()
        {
            var p = new HwndSourceParameters("RetroCapHotkeySink") { Width = 0, Height = 0, WindowStyle = 0, ParentWindow = new IntPtr(-3) /* HWND_MESSAGE */ };
            _source = new HwndSource(p);
            _source.AddHook(WndProc);
        }

        /// <summary>Registers e.g. Ctrl+Alt+A. Returns false if another program already owns it.</summary>
        public bool Register(uint modifiers, uint virtualKey)
        {
            Unregister();
            _registered = Native.RegisterHotKey(_source.Handle, Id, modifiers | Native.MOD_NOREPEAT, virtualKey);
            return _registered;
        }

        public void Unregister()
        {
            if (_registered) Native.UnregisterHotKey(_source.Handle, Id);
            _registered = false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_HOTKEY && wParam.ToInt32() == Id)
            {
                handled = true;
                Pressed?.Invoke(this, EventArgs.Empty);
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            _source.RemoveHook(WndProc);
            _source.Dispose();
        }
    }
}
