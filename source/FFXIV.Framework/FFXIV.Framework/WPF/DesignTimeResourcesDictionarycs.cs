using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace FFXIV.Framework.WPF
{
    public class DesignTimeResourceDictionary : ResourceDictionary
    {
        public bool IsInDesignMode =>
            (bool)DependencyPropertyDescriptor.FromProperty(
                DesignerProperties.IsInDesignModeProperty,
                typeof(DependencyObject))
                .Metadata.DefaultValue;

        public new Uri Source
        {
            get => base.Source;
            set
            {
                if (!this.IsInDesignMode)
                {
                    return;
                }

                Debug.WriteLine("Setting Source = " + value);
                base.Source = value;
            }
        }
    }
}
