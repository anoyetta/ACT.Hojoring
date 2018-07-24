using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.Image
{
    public class IconController
    {
        #region Singleton

        private static IconController instance = new IconController();

        public static IconController Instance => instance;

        #endregion Singleton

        /// <summary>
        /// Blankビットマップ
        /// </summary>
        public static readonly BitmapSource BlankBitmap = BitmapImage.Create(
            2,
            2,
            96,
            96,
            PixelFormats.Indexed1,
            new BitmapPalette(new List<Color> { Colors.Transparent }),
            new byte[] { 0, 0, 0, 0 },
            1);

        private string[] iconDirectories;

        public string[] IconDirectories
        {
            get
            {
                lock (this)
                {
                    if (this.iconDirectories == null)
                    {
                        var dirs = new List<string>();

                        var actDirectory = string.Empty;

                        // ACTのパスを取得する
                        var asm = Assembly.GetEntryAssembly();
                        if (asm != null)
                        {
                            actDirectory = Path.GetDirectoryName(asm.Location);

                            var dir1 = Path.Combine(actDirectory, @"resources\icon");
                            if (Directory.Exists(dir1))
                            {
                                dirs.Add(dir1);
                            }

                            var dir2 = Path.Combine(actDirectory, @"resources\xivdb\Action icons");
                            if (Directory.Exists(dir2))
                            {
                                dirs.Add(dir2);
                            }
                        }

                        // 自身の場所を取得する
                        var selfDirectory = PluginCore.Instance?.Location ?? string.Empty;
                        if (Path.GetFullPath(selfDirectory).ToLower() !=
                            Path.GetFullPath(actDirectory).ToLower())
                        {
                            var dir3 = Path.Combine(selfDirectory, @"resources\icon");
                            if (Directory.Exists(dir3))
                            {
                                dirs.Add(dir3);
                            }

                            var dir4 = Path.Combine(selfDirectory, @"resources\xivdb\Action icons");
                            if (Directory.Exists(dir4))
                            {
                                dirs.Add(dir4);
                            }
                        }

                        this.iconDirectories = dirs.ToArray();
                    }
                }

                return this.iconDirectories;
            }
        }

        private IconFile[] iconFiles;

        /// <summary>
        /// Iconファイルを列挙する
        /// </summary>
        /// <returns>
        /// Iconファイルのコレクション</returns>
        public IconFile[] EnumerateIcon()
        {
            lock (this)
            {
                if (this.iconFiles == null)
                {
                    var list = new List<IconFile>();

                    // 未選択用のダミーをセットしておく
                    list.Add(new IconFile()
                    {
                        FullPath = string.Empty,
                    });

                    foreach (var dir in this.IconDirectories)
                    {
                        if (Directory.Exists(dir))
                        {
                            list.AddRange(this.EnumerateIcon(dir));
                        }
                    }

                    this.iconFiles = (
                        from x in list
                        orderby
                        x.DirectoryName,
                        x.Name
                        select
                        x).Distinct().ToArray();
                }
            }

            return this.iconFiles;
        }

        /// <summary>
        /// キャッシュされたアイコン情報を更新する
        /// </summary>
        public void RefreshIcon()
        {
            lock (this)
            {
                this.iconFiles = null;
            }

            this.EnumerateIcon();
        }

        private IconFile[] EnumerateIcon(
            string directory)
        {
            var list = new List<IconFile>();

            foreach (var dir in Directory.GetDirectories(directory))
            {
                list.AddRange(this.EnumerateIcon(dir));
            }

            foreach (var file in Directory.GetFiles(directory, "*.png"))
            {
                var icon = new IconFile()
                {
                    FullPath = file,
                };

                list.Add(icon);
            }

            return list.ToArray();
        }

        public IconFile GetIconFile(
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (name.Contains("\\"))
            {
                name = name.Split('\\').LastOrDefault();
            }

            return this.EnumerateIcon().FirstOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileNameWithoutExtension(x.Name), name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Iconファイル
        /// </summary>
        public class IconFile :
            BindableBase,
            IEquatable<IconFile>
        {
            private static readonly Regex SkillNameRegex = new Regex(
                @"\d\d\d\d_(?<skillName>.+?)\.png",
                RegexOptions.Compiled);

            private string fullPath;

            public string FullPath
            {
                get => this.fullPath;
                set
                {
                    if (this.SetProperty(ref this.fullPath, value))
                    {
                        if (string.IsNullOrEmpty(this.Name))
                        {
                            return;
                        }

                        var match = SkillNameRegex.Match(this.Name);
                        if (match.Success)
                        {
                            this.SkillName = match.Groups["skillName"].Value;
                        }
                        else
                        {
                            this.SkillName = this.Name;
                        }
                    }
                }
            }

            public string Directory =>
                !string.IsNullOrEmpty(this.FullPath) ?
                Path.GetDirectoryName(this.FullPath) :
                string.Empty;

            /// <summary>
            /// 直上のディレクトリ名
            /// </summary>
            public string DirectoryName =>
                this.Directory.Split('\\').LastOrDefault() ?? string.Empty;

            public string Name =>
                !string.IsNullOrWhiteSpace(this.FullPath) ?
                    Path.GetFileName(this.FullPath) :
                    string.Empty;

            public string SkillName { get; private set; } = string.Empty;

            public override string ToString() => this.Name;

            public BitmapImage BitmapImage => this.CreateBitmapImage();

            public BitmapImage CreateBitmapImage()
            {
                if (!File.Exists(this.FullPath))
                {
                    return null;
                }

                var img = default(BitmapImage);
                var path = this.FullPath.ToLower();

                lock (iconDictionary)
                {
                    if (iconDictionary.ContainsKey(path))
                    {
                        img = iconDictionary[path];
                    }
                    else
                    {
                        img = new BitmapImage();
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.CreateOptions = BitmapCreateOptions.None;
                        img.UriSource = new Uri(path);
                        img.EndInit();
                        img.Freeze();

                        iconDictionary[path] = img;
                    }
                }

                return img;
            }

            public bool Equals(
                IconFile other)
            {
                if (other == null)
                {
                    return false;
                }

                return string.Equals(this.FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
            }

            private static Dictionary<string, BitmapImage> iconDictionary = new Dictionary<string, BitmapImage>();
        }
    }
}
