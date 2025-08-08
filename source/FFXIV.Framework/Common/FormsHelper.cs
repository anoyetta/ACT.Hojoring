using System;
using System.Windows.Forms;

namespace FFXIV.Framework.Common
{
    public static class FormsHelper
    {
        public static object Invoke(
            Control control,
            Action action,
            params object[] args)
        {
            if (control == null ||
                control.IsDisposed ||
                !control.IsHandleCreated)
            {
                return null;
            }

            if (control.InvokeRequired)
            {
                return control.Invoke(action, args);
            }
            else
            {
                action();
                return null;
            }
        }

        public static object TryInvoke(
            this Control control,
            Action action,
            params object[] args)
        {
            return FormsHelper.Invoke(control, action, args);
        }
    }
}
