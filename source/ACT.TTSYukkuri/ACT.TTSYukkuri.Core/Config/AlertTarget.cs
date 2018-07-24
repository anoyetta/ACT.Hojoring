using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace ACT.TTSYukkuri.Config
{
    /// <summary>
    /// 監視対象
    /// </summary>
    [Serializable]
    public class AlertTarget :
        BindableBase
    {
        private AlertCategories category;
        private bool enabled;

        [XmlIgnore]
        public string Text => this.Category.GetText();

        [XmlAttribute]
        public AlertCategories Category
        {
            get => this.category;
            set => this.SetProperty(ref this.category, value);
        }

        [XmlAttribute]
        public bool Enabled
        {
            get => this.enabled;
            set => this.SetProperty(ref this.enabled, value);
        }

        [XmlIgnore]
        public static AlertTarget[] EnumlateAlertTargets
        {
            get
            {
                var list = new List<AlertTarget>();
                foreach (AlertCategories category in Enum.GetValues(typeof(AlertCategories)))
                {
                    list.Add(new AlertTarget() { Category = category });
                }

                return list.ToArray();
            }
        }
    }
}
