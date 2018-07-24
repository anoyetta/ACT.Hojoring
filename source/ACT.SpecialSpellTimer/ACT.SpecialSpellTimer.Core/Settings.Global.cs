#if false
namespace ACT.SpecialSpellTimer
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    public class Settings
    {
        public static Settings def = new Settings();

        private string xmlpath =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
            "\\Advanced Combat Tracker\\Config\\ACT.SpecialSpellTimer.config.xml";

        public Settings()
        {
            Dictionary<string, object> def = DefaultValue;
            foreach (KeyValuePair<string, object> v in def)
            {
                GetType().GetProperty(v.Key).SetValue(this, v.Value);
            }

            Load();
        }

        public static Settings Default
        {
            get => def;
            set => def = value;
        }

        public bool AutoSortEnabled { get; set; }
        public bool AutoSortReverse { get; set; }
        public Color BackgroundColor { get; set; }
        public bool ClickThroughEnabled { get; set; }
        public long CombatLogBufferSize { get; set; }
        public bool CombatLogEnabled { get; set; }
        public bool DetectPacketDump { get; set; }
        public string DQXPlayerName { get; set; }
        public bool DQXUtilityEnabled { get; set; }
        public bool EnabledNotifyNormalSpellTimer { get; set; }
        public bool EnabledPartyMemberPlaceholder { get; set; }
        public bool EnabledSpellTimerNoDecimal { get; set; }
        public Font Font { get; set; }
        public Color FontColor { get; set; }
        public Color FontOutlineColor { get; set; }
        public bool HideWhenNotActive { get; set; }
        public string Language { get; set; } = "EN";
        public DateTime LastUpdateDateTime { get; set; }
        public long LogPollSleepInterval { get; set; }
        public int MaxFPS { get; set; }
        public string NotifyNormalSpellTimerPrefix { get; set; }
        public int Opacity { get; set; }
        public bool OverlayForceVisible { get; set; }
        public bool OverlayVisible { get; set; }
        public string OverText { get; set; }
        public double PlayerInfoRefreshInterval { get; set; }
        public Color ProgressBarColor { get; set; }
        public Color ProgressBarOutlineColor { get; set; }
        public Size ProgressBarSize { get; set; }
        public string ReadyText { get; set; }
        public int ReduceIconBrightness { get; set; }
        public long RefreshInterval { get; set; }
        public bool RemoveTooltipSymbols { get; set; }
        public bool ResetOnWipeOut { get; set; }
        public bool SaveLogEnabled { get; set; }
        public string SaveLogFile { get; set; }
        public bool SimpleRegex { get; set; }
        public bool TelopAlwaysVisible { get; set; }
        public double TextBlurGain { get; set; }
        public double TextOutlineThicknessGain { get; set; }
        public double TimeOfHideSpell { get; set; }
        public double UpdateCheckInterval { get; set; }
        public bool UseOtherThanFFXIV { get; set; }
        public Color WarningFontColor { get; set; }

        public Color WarningFontOutlineColor { get; set; }

        private Dictionary<string, object> DefaultValue
        {
            get
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("LastUpdateDateTime", new DateTime(2000, 1, 1));
                dic.Add("ProgressBarSize", new Size(190, 8));
                dic.Add("ProgressBarColor", Color.White);
                dic.Add("FontColor", Color.AliceBlue);
                dic.Add("FontOutlineColor", Color.FromArgb(22, 120, 157));
                dic.Add("WarningFontColor", Color.OrangeRed);
                dic.Add("WarningFontOutlineColor", Color.DarkRed);
                dic.Add("ProgressBarOutlineColor", Color.FromArgb(22, 120, 157));
                dic.Add("BackgroundColor", Color.Transparent);
                dic.Add("Language", "EN");
                dic.Add("NotifyNormalSpellTimerPrefix", "spespe_");
                dic.Add("ReadyText", "Ready");
                dic.Add("OverText", "Over");
                dic.Add("SaveLogFile", "");
                dic.Add("UpdateCheckInterval", 12.0D);
                dic.Add("TimeOfHideSpell", 0.0D);
                dic.Add("PlayerInfoRefreshInterval", 3.0D);
                dic.Add("LogPollSleepInterval", 100L);
                dic.Add("RefreshInterval", 100L);
                dic.Add("CombatLogBufferSize", 30000L);
                dic.Add("ReduceIconBrightness", 55);
                dic.Add("Opacity", 10);
                dic.Add("MaxFPS", 15);
                dic.Add("Font", new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold));
                dic.Add("OverlayVisible", true);
                dic.Add("AutoSortEnabled", true);
                dic.Add("ClickThroughEnabled", false);
                dic.Add("AutoSortReverse", false);
                dic.Add("TelopAlwaysVisible", false);
                dic.Add("EnabledPartyMemberPlaceholder", false);
                dic.Add("CombatLogEnabled", false);
                dic.Add("OverlayForceVisible", false);
                dic.Add("EnabledSpellTimerNoDecimal", false);
                dic.Add("EnabledNotifyNormalSpellTimer", false);
                dic.Add("SaveLogEnabled", false);
                dic.Add("HideWhenNotActive", false);
                dic.Add("UseOtherThanFFXIV", false);
                dic.Add("DQXUtilityEnabled", false);
                dic.Add("DQXPlayerName", "");
                dic.Add("ResetOnWipeOut", false);
                dic.Add("SimpleRegex", false);
                dic.Add("RemoveTooltipSymbols", false);
                dic.Add("DetectPacketDump", false);
                dic.Add("TextBluerGain", 2.0d);
                dic.Add("TextOutlineThicknessGain", 1.0d);

                return dic;
            }
        }

        public void Load()
        {
            if (File.Exists(xmlpath))
            {
                XmlDocument xd = new XmlDocument();
                xd.Load(xmlpath);

                foreach (XmlElement xe in xd.SelectNodes("/Config/SettingsSerializer/*"))
                {
                    try
                    {
                        string name = xe.GetAttribute("Name");
                        string value = xe.GetAttribute("Value");
                        PropertyInfo prop = GetType().GetProperty(name);

                        switch (xe.Name)
                        {
                            case "Font":
                                prop.SetValue(this, new FontConverter().ConvertFromString(value));
                                break;

                            case "Color":
                                prop.SetValue(this, IntToColor(Convert.ToUInt32(value)));
                                break;

                            case "Boolean":
                                prop.SetValue(this, (value == "True" ? true : false));
                                break;

                            case "Double":
                                prop.SetValue(this, Convert.ToDouble(value));
                                break;

                            case "Int64":
                                prop.SetValue(this, Convert.ToInt64(value));
                                break;

                            case "Int32":
                                prop.SetValue(this, Convert.ToInt32(value));
                                break;

                            case "String":
                                prop.SetValue(this, value.Trim());
                                break;

                            case "Size":
                                int w = 0, h = 0;
                                foreach (XmlElement sizeattr in xe.ChildNodes)
                                {
                                    switch (sizeattr.Name)
                                    {
                                        case "Width":
                                            w = Convert.ToInt32(sizeattr.InnerText);
                                            break;

                                        case "Height":
                                            h = Convert.ToInt32(sizeattr.InnerText);
                                            break;
                                    }
                                }
                                prop.SetValue(this, new Size(w, h));
                                break;

                            case "DateTime":
                                prop.SetValue(this, DateTime.Parse(value));
                                break;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void Reset()
        {
            Dictionary<string, object> def = DefaultValue;
            foreach (KeyValuePair<string, object> v in def)
            {
                GetType().GetProperty(v.Key).SetValue(this, v.Value);
            }
        }

        public void Save()
        {
            Dictionary<string, object> def = DefaultValue;
            XmlDocument xd = new XmlDocument();
            XmlElement Config = xd.CreateElement("Config");
            XmlElement Setting = xd.CreateElement("SettingsSerializer");

            foreach (KeyValuePair<string, object> val in def)
            {
                XmlElement xe = xd.CreateElement(val.Value.GetType().ToString().Replace("System.", "").Replace("Drawing.", ""));
                xe.SetAttribute("Name", val.Key);

                if (val.Value.GetType() == typeof(Font))
                {
                    xe.SetAttribute("Value", new FontConverter().ConvertToString(GetType().GetProperty(val.Key).GetValue(this)));
                }
                else if (val.Value.GetType() == typeof(Color))
                {
                    xe.SetAttribute("Value", ColorToInt((Color)GetType().GetProperty(val.Key).GetValue(this)).ToString());
                }
                else if (val.Value.GetType() == typeof(double))
                {
                    xe = xd.CreateElement("Double");
                    xe.SetAttribute("Name", val.Key);
                    xe.SetAttribute("Value", GetType().GetProperty(val.Key).GetValue(this).ToString());
                }
                else if (val.Value.GetType() == typeof(long))
                {
                    xe = xd.CreateElement("Int64");
                    xe.SetAttribute("Name", val.Key);
                    xe.SetAttribute("Value", GetType().GetProperty(val.Key).GetValue(this).ToString());
                }
                else if (val.Value.GetType() == typeof(Size))
                {
                    Size s = (Size)GetType().GetProperty(val.Key).GetValue(this);
                    XmlElement Width = xd.CreateElement("Width");
                    Width.InnerText = s.Width.ToString();
                    XmlElement Height = xd.CreateElement("Height");
                    Height.InnerText = s.Height.ToString();

                    xe.AppendChild(Width);
                    xe.AppendChild(Height);
                }
                else
                {
                    xe.SetAttribute("Value", GetType().GetProperty(val.Key).GetValue(this).ToString());
                }

                Setting.AppendChild(xe);
            }

            Config.AppendChild(Setting);
            xd.AppendChild(Config);
            xd.Save(xmlpath);
        }

        private uint ColorToInt(Color color)
        {
            return (uint)(
                (color.A << 24) |
                (color.R << 16) |
                (color.G << 8) |
                (color.B << 0));
        }

        private Color IntToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);

            return Color.FromArgb(a, r, g, b);
        }
    }
}
#endif