using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using FFXIV.Framework.XIVHelper;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.Common
{
    public class JobIconDictionary
    {
        #region Singleton

        private static JobIconDictionary instance;

        public static JobIconDictionary Instance => instance ?? (instance = new JobIconDictionary());

        private JobIconDictionary()
        {
        }

        #endregion Singleton

        public Dictionary<JobIDs, BitmapSource> Icons { get; } = new Dictionary<JobIDs, BitmapSource>();

        public BitmapSource GetIcon(
            Actor.Job job)
        {
            var jobValue = (int)job;

            if (Enum.IsDefined(typeof(JobIDs), jobValue))
            {
                var jobID = (JobIDs)Enum.ToObject(typeof(JobIDs), jobValue);
                return this.GetIcon(jobID);
            }

            return null;
        }

        public BitmapSource GetIcon(
            JobIDs job)
        {
            if (!this.isLoaded)
            {
                this.Load();
            }

            return this.Icons.ContainsKey(job) ?
                this.Icons[job] :
                null;
        }

        private volatile bool isLoaded;

        public async void Load()
        {
            var dir = DirectoryHelper.FindSubDirectory(
                @"resources\icon\job");
            if (!Directory.Exists(dir))
            {
                return;
            }

            await WPFHelper.InvokeAsync(() =>
            {
                foreach (var job in (JobIDs[])Enum.GetValues(typeof(JobIDs)))
                {
                    var png = Path.Combine(dir, $"{job}.png");
                    if (!File.Exists(png))
                    {
                        continue;
                    }

                    var bmp = default(WriteableBitmap);
                    using (var ms = new WrappingStream(new MemoryStream(File.ReadAllBytes(png))))
                    {
                        bmp = new WriteableBitmap(BitmapFrame.Create(ms));
                    }

                    bmp.Freeze();
                    this.Icons[job] = bmp;
                }
            });

            this.isLoaded = true;
        }
    }
}
