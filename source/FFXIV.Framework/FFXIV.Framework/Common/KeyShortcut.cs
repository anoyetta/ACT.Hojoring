using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using System.Xml.Serialization;
using Prism.Mvvm;
using WindowsInput;
using WindowsInput.Native;

namespace FFXIV.Framework.Common
{
    [Serializable]
    public class KeyShortcut :
        BindableBase
    {
        public KeyShortcut()
        {
            this.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(this.IsControl):
                    case nameof(this.IsShift):
                    case nameof(this.IsAlt):
                    case nameof(this.IsWin):
                    case nameof(this.Key):
                        this.RaisePropertyChanged(nameof(this.Text));
                        break;
                }
            };
        }

        private bool isControl;

        [XmlAttribute(AttributeName = "Control")]
        public bool IsControl
        {
            get => this.isControl;
            set => this.SetProperty(ref this.isControl, value);
        }

        private bool isShift;

        [XmlAttribute(AttributeName = "Shift")]
        public bool IsShift
        {
            get => this.isShift;
            set => this.SetProperty(ref this.isShift, value);
        }

        private bool isAlt;

        [XmlAttribute(AttributeName = "Alt")]
        public bool IsAlt
        {
            get => this.isAlt;
            set => this.SetProperty(ref this.isAlt, value);
        }

        private bool isWin;

        [XmlAttribute(AttributeName = "Win")]
        public bool IsWin
        {
            get => this.isWin;
            set => this.SetProperty(ref this.isWin, value);
        }

        private Key key;

        [XmlAttribute(AttributeName = "Key")]
        public Key Key
        {
            get => this.key;
            set => this.SetProperty(ref this.key, value);
        }

        [XmlIgnore]
        public string Text => string.Join("+", new[]
        {
            this.IsWin ? "Win" : string.Empty,
            this.IsControl ? "Ctrl" : string.Empty,
            this.IsShift ? "Shift" : string.Empty,
            this.IsAlt ? "Alt" : string.Empty,
            this.Key.ToString().Replace("VK_", string.Empty)
        }
        .Where(x => !string.IsNullOrEmpty(x))
        .ToArray());

        private static IntPtr xivHandle = IntPtr.Zero;
        private static readonly Lazy<InputSimulator> LazyInput = new Lazy<InputSimulator>(() => new InputSimulator());

        public void SendKey(
            int times = 1,
            int interval = 100)
        {
            if (xivHandle == IntPtr.Zero)
            {
                xivHandle = FindWindow(null, "FINAL FANTASY XIV");
            }

            if (xivHandle != IntPtr.Zero)
            {
                SetForegroundWindow(xivHandle);
            }

            var modifiers = this.GetModifiers();
            var keys = this.GetKeys();
            var sim = LazyInput.Value;

            foreach (var key in modifiers)
            {
                sim.Keyboard.KeyDown(key);
                Thread.Sleep(TimeSpan.FromMilliseconds(20));
            }

            for (int i = 0; i < times; i++)
            {
                if (i > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(interval));
                }

                sim.Keyboard.KeyPress(keys);
            }

            foreach (var key in modifiers.Reverse())
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(20));
                sim.Keyboard.KeyUp(key);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    public static class KeyShortcutExtensions
    {
        public static VirtualKeyCode[] GetModifiers(
            this KeyShortcut shortcut)
        {
            var keys = new List<VirtualKeyCode>();

            if (shortcut.IsWin)
            {
                keys.Add(VirtualKeyCode.LWIN);
            }

            if (shortcut.IsControl)
            {
                keys.Add(VirtualKeyCode.CONTROL);
            }

            if (shortcut.IsShift)
            {
                keys.Add(VirtualKeyCode.SHIFT);
            }

            if (shortcut.IsAlt)
            {
                keys.Add(VirtualKeyCode.MENU);
            }

            return keys.ToArray();
        }

        public static VirtualKeyCode[] GetKeys(this KeyShortcut shortcut) => new[] { ToVK(shortcut.Key) };

        private static VirtualKeyCode ToVK(Key key)
            => (VirtualKeyCode)Enum.ToObject(typeof(VirtualKeyCode), KeyInterop.VirtualKeyFromKey(key));
    }

    [Serializable]
    public class SendKeyConfig : BindableBase
    {
        private bool isEnabled;

        [XmlAttribute(AttributeName = "IsEnabled")]
        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetProperty(ref this.isEnabled, value);
        }

        private KeyShortcut keySet = new KeyShortcut();

        public KeyShortcut KeySet
        {
            get => this.keySet;
            set => this.SetProperty(ref this.keySet, value);
        }
    }
}
