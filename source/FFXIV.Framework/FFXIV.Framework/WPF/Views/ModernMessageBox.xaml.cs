using System;
using System.ComponentModel;
using System.Media;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.WPF.Views
{
    /// <summary>
    /// TagView.xaml の相互作用ロジック
    /// </summary>
    public partial class ModernMessageBox :
        Window,
        INotifyPropertyChanged
    {
        public ModernMessageBox()
        {
            this.InitializeComponent();

            // ウィンドウのスタート位置を決める
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            this.MouseLeftButtonDown += (x, y) => this.DragMove();

            this.PreviewKeyUp += (x, y) =>
            {
                if (y.Key == Key.Escape)
                {
                    this.DialogResult = false;
                    this.Close();
                }
            };

            this.CloseButton.Click += (x, y) =>
            {
                this.DialogResult = false;
                this.Close();
            };

            this.OKButton.Click += (x, y) =>
            {
                this.DialogResult = true;
            };
        }

        public static bool ShowDialog(
            string message,
            string caption,
            MessageBoxButton button = MessageBoxButton.OK,
            Exception ex = null)
        {
            var view = new ModernMessageBox()
            {
                Message = message,
                Caption = caption,
            };

            if (button == MessageBoxButton.OKCancel)
            {
                view.CancelButton.Visibility = Visibility.Visible;
            }
            else
            {
                view.CancelButton.Visibility = Visibility.Collapsed;
            }

            view.Details = string.Empty;

            if (ex != null)
            {
                var info = ex.GetType().ToString() + Environment.NewLine + Environment.NewLine;
                info += ex.Message + Environment.NewLine;
                info += ex.StackTrace.ToString();

                if (ex.InnerException != null)
                {
                    info += Environment.NewLine + Environment.NewLine;
                    info += "Inner Exception :" + Environment.NewLine;
                    info += ex.InnerException.GetType().ToString() + Environment.NewLine + Environment.NewLine;
                    info += ex.InnerException.Message + Environment.NewLine;
                    info += ex.InnerException.StackTrace.ToString();
                }

                view.Details = info;
            }

            if (view.HasDetails)
            {
                view.WindowBorderBrush = Brushes.OrangeRed;
                SystemSounds.Beep.Play();
            }
            else
            {
                view.WindowBorderBrush = Brushes.Gold;
                view.SizeToContent = SizeToContent.WidthAndHeight;
                SystemSounds.Asterisk.Play();
            }

            view.RaisePropertyChanged(nameof(WindowBorderBrush));
            view.RaisePropertyChanged(nameof(Caption));
            view.RaisePropertyChanged(nameof(Message));
            view.RaisePropertyChanged(nameof(Details));
            view.RaisePropertyChanged(nameof(HasDetails));

            return view.ShowDialog() ?? false;
        }

        public Brush WindowBorderBrush { get; set; } = Brushes.Gold;

        public string Caption { get; set; } = "キャプション";

        public string Message { get; set; } = "スペルを削除しますか？";

        public string Details { get; set; } =
            "メッセージ: NullReferenceExceotion" + Environment.NewLine +
            "あいうえおかきくけこさしすせそ" + Environment.NewLine +
            "あいうえおかきくけこさしすせそ" + Environment.NewLine +
            "あいうえおかきくけこさしすせそ" + Environment.NewLine +
            "あいうえおかきくけこさしすせそ";

        public bool HasDetails =>
            WPFHelper.IsDesignMode ?
            true :
            !string.IsNullOrEmpty(this.Details);

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }
}
