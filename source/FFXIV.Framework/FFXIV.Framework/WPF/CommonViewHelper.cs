using System;
using System.Windows.Forms.Integration;
using FFXIV.Framework.WPF.Views;

namespace FFXIV.Framework.WPF
{
    public class CommonViewHelper
    {
        #region Lazy Singleton

        private static readonly Lazy<CommonViewHelper> LazyInstance = new Lazy<CommonViewHelper>(() => new CommonViewHelper());

        public static CommonViewHelper Instance => LazyInstance.Value;

        private CommonViewHelper()
        {
        }

        #endregion Lazy Singleton

        private bool isCommonViewAdded = false;

        public void AddCommonView(
            System.Windows.Forms.TabControl tabControl)
        {
            lock (this)
            {
                if (this.isCommonViewAdded)
                {
                    return;
                }

                this.isCommonViewAdded = true;
            }

            var tabPage = new System.Windows.Forms.TabPage("Hojoring");

            tabPage.Controls.Add(new ElementHost()
            {
                Child = new CommonView(),
                Dock = System.Windows.Forms.DockStyle.Fill,
            });

            tabControl.TabPages.Add(tabPage);
        }
    }
}
