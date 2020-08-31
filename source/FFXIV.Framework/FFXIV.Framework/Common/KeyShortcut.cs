using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using Prism.Mvvm;
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
