using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.Common;

namespace ACT.SpecialSpellTimer.Models
{
    /// <summary>
    /// Panel設定
    /// </summary>
    public class SpellPanelTable
    {
        #region Singleton

        public static SpellPanelTable Instance { get; } = new SpellPanelTable();

        #endregion Singleton

        private volatile ObservableCollection<SpellPanel> table = new ObservableCollection<SpellPanel>();

        public ObservableCollection<SpellPanel> Table => this.table;

        public string DefaultFile => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"anoyetta\ACT\ACT.SpecialSpellTimer.Panels.xml");

        public void Load()
        {
            lock (this)
            {
                try
                {
                    // サイズ0のファイルがもしも存在したら消す
                    if (File.Exists(this.DefaultFile))
                    {
                        var fi = new FileInfo(this.DefaultFile);
                        if (fi.Length <= 0)
                        {
                            File.Delete(this.DefaultFile);
                        }
                    }

                    if (!File.Exists(this.DefaultFile))
                    {
                        return;
                    }

                    // 旧形式を置換する
                    var text = File.ReadAllText(
                        this.DefaultFile,
                        new UTF8Encoding(false));
                    text = text.Replace("DocumentElement", "ArrayOfPanelSettings");
                    File.WriteAllText(
                        this.DefaultFile,
                        text,
                        new UTF8Encoding(false));

                    using (var sr = new StreamReader(this.DefaultFile, new UTF8Encoding(false)))
                    {
                        if (sr.BaseStream.Length > 0)
                        {
                            var xs = new XmlSerializer(this.table.GetType());
                            var data = xs.Deserialize(sr) as IList<SpellPanel>;

                            this.table.Clear();

                            foreach (var x in data)
                            {
                                // 旧Generalパネルの名前を置換える
                                if (x.PanelName == "General")
                                {
                                    x.PanelName = SpellPanel.GeneralPanel.PanelName;
                                }

                                // NaNを潰す
                                x.Top = double.IsNaN(x.Top) ? 0 : x.Top;
                                x.Left = double.IsNaN(x.Left) ? 0 : x.Left;
                                x.Margin = double.IsNaN(x.Margin) ? 0 : x.Margin;

                                // ソートオーダーを初期化する
                                if (x.SortOrder == SpellOrders.None)
                                {
                                    if (x.FixedPositionSpell)
                                    {
                                        x.SortOrder = SpellOrders.Fixed;
                                    }
                                    else
                                    {
                                        if (!Settings.Default.AutoSortEnabled)
                                        {
                                            x.SortOrder = SpellOrders.SortMatchTime;
                                        }
                                        else
                                        {
                                            if (!Settings.Default.AutoSortReverse)
                                            {
                                                x.SortOrder = SpellOrders.SortRecastTimeASC;
                                            }
                                            else
                                            {
                                                x.SortOrder = SpellOrders.SortRecastTimeDESC;
                                            }
                                        }
                                    }
                                }

                                this.table.Add(x);
                            }
                        }
                    }
                } catch (Exception ex)
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

                    var result = MessageBox.Show("faild config load\n\n" + DefaultFile + "\n" + info + "\n\ntry to load backup?", "error!", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (EnvironmentHelper.RestoreFile(DefaultFile))
                        {
                            Load();
                        }
                    }
                }
                finally
                {
                    var generalPanel = this.table.FirstOrDefault(x => x.PanelName == SpellPanel.GeneralPanel.PanelName);
                    if (generalPanel != null)
                    {
                        SpellPanel.SetGeneralPanel(generalPanel);
                    }
                    else
                    {
                        this.table.Add(SpellPanel.GeneralPanel);
                    }
                }
            }
        }

        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        public void Save()
        {
            lock (this)
            {
                if (this.table == null ||
                    this.table.Count < 1)
                {
                    return;
                }

                var dir = Path.GetDirectoryName(this.DefaultFile);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                using (var sw = new StreamWriter(this.DefaultFile, false, DefaultEncoding))
                {
                    var xs = new XmlSerializer(this.table.GetType());
                    xs.Serialize(sw, this.table, ns);
                    sw.Close();
                }
            }
        }
    }
}
