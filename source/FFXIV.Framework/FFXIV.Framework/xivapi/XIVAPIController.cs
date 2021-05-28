using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.xivapi.Models;
using FFXIV.Framework.XIVHelper;
using Newtonsoft.Json;

namespace FFXIV.Framework.xivapi
{
    public class XIVAPIController
    {
        #region Singleton

        private static XIVAPIController instance;

        public static XIVAPIController Instance => instance ?? (instance = new XIVAPIController());

        private XIVAPIController()
        {
            this.InitializeClient();
        }

        #endregion Singleton

        public static readonly Uri BaseAddress = new Uri("https://xivapi.com/");

        private readonly HttpClient client = new HttpClient();

#if DEBUG
        private static readonly string ApiKey = "b1a9c1f77b9a434e8ac05f04";
#else
        private static readonly string ApiKey = "3eb773dfd2bb43a18ac8";
#endif
        private const int DefaultLimitCount = 3000;

        private void InitializeClient()
        {
            this.client.BaseAddress = BaseAddress;
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Locales Language { get; set; } = Locales.EN;

        public NameValueCollection CreateQueryStringParser()
            => HttpUtility.ParseQueryString(string.Empty);

        public async Task<HttpResponseMessage> GetAsync(
            string uri,
            NameValueCollection queryString = null)
        {
            if (queryString != null)
            {
                uri += "?" + queryString.ToString();
            }

            return await this.client.GetAsync(uri);
        }

        public delegate void ProgressDownloadCallbak(DownloadProgressEventArgs args);

        #region xivapi.com/Action

        public async Task<IEnumerable<ActionModel>> GetActionsAsync(
            ProgressDownloadCallbak callback = null)
        {
            const string MethodUri = "Action";

            var result = default(ApiResultModel<List<ActionModel>>);
            var actionList = new List<ActionModel>();
            var currentPage = 1;

            do
            {
                var query = this.CreateQueryStringParser();
                query["language"] = this.Language.ToString().ToLower();
                query["limit"] = DefaultLimitCount.ToString();
                query["page"] = currentPage.ToString();
                query["columns"] = ActionModel.HandledColumns;
#if DEBUG
                query["pretty"] = "1";
#endif
                if (!string.IsNullOrEmpty(ApiKey))
                {
                    query["key"] = ApiKey;
                }

                var res = await this.GetAsync(MethodUri, query);
                var json = await res?.Content?.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    result = JsonConvert.DeserializeObject<ApiResultModel<List<ActionModel>>>(json);
#if DEBUG
                    /*
                    System.Diagnostics.Debug.WriteLine(json);
                    */
#endif
                }

                if (result == null ||
                    result.Pagination == null)
                {
                    break;
                }

                actionList.AddRange(
                    from x in result.Results
                    where
                    x.ActionCategory.ID.HasValue &&
                    x.ClassJobCategory.ID.HasValue &&
                    x.ActionCategory.ID != (int)ActionCategory.AutoAttack &&
                    x.ActionCategory.ID != (int)ActionCategory.Item &&
                    x.ActionCategory.ID != (int)ActionCategory.Event &&
                    x.ActionCategory.ID != (int)ActionCategory.System &&
                    x.ActionCategory.ID != (int)ActionCategory.Mount &&
                    x.ActionCategory.ID != (int)ActionCategory.Glamour &&
                    x.ActionCategory.ID != (int)ActionCategory.AdrenalineRush &&
                    !string.IsNullOrEmpty(x.Name)
                    select
                    x);

                if (callback != null)
                {
                    callback.Invoke(
                        new DownloadProgressEventArgs()
                        {
                            Current = currentPage,
                            Max = result.Pagination.PageTotal,
                            CurrentObject = result.Results
                        });
                }

                currentPage++;
                await Task.Delay(TimeSpan.FromSeconds(1.05));
            } while (
                result.Pagination.Page.HasValue &&
                result.Pagination.Page < result.Pagination.PageTotal);

            var sorted =
                from x in actionList
                orderby
                x.ClassJob.ID,
                x.ID ?? 0
                select
                x;
#if DEBUG
            foreach (var action in sorted)
            {
                System.Diagnostics.Debug.WriteLine(action.ToString());
            }
#endif
            return sorted;
        }

        public async Task DownloadActionIcons(
            string saveDirectory,
            IEnumerable<ActionModel> actions,
            ProgressDownloadCallbak callback = null)
        {
            // ファイル名に使えない文字を取得しておく
            var invalidChars = Path.GetInvalidFileNameChars();

            var iconBaseDirectory = Path.Combine(
                saveDirectory,
                "Action icons");

            var iconBaseDirectoryBack = iconBaseDirectory + ".back";
            if (Directory.Exists(iconBaseDirectory))
            {
                if (Directory.Exists(iconBaseDirectoryBack))
                {
                    Directory.Delete(iconBaseDirectoryBack, true);
                }

                Directory.Move(iconBaseDirectory, iconBaseDirectoryBack);
            }

            var i = 1;
            var jobIDs = Jobs.PopularJobIDs
                .Select(x => new
                {
                    No = i++,
                    ID = x,
                    Name = Jobs.Find(x)?.NameEN
                });

            using (var wc = new WebClient())
            {
                foreach (var jobID in jobIDs)
                {
                    var actionsByJob =
                        from x in actions
                        where
                        x.ContainsJob(jobID.ID)
                        group x by
                        x.Name;

                    var dirName = $"{jobID.No:00}_{jobID.Name}";
                    var dir = Path.Combine(iconBaseDirectory, dirName);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var current = 1;
                    foreach (var group in actionsByJob)
                    {
                        var action = group.First();

                        var fileName = $"{(action.ID ?? 0):0000}_{action.Name}.png";

                        // ファイル名に使えない文字を除去する
                        fileName = string.Concat(fileName.Where(c =>
                            !invalidChars.Contains(c)));

                        var file = Path.Combine(dir, fileName);
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }

                        var uri = BaseAddress.ToString() + "/" + action.Icon;
                        await wc.DownloadFileTaskAsync(uri, file);

                        if (callback != null)
                        {
                            callback.Invoke(new DownloadProgressEventArgs()
                            {
                                Current = current,
                                Max = actionsByJob.Count(),
                                CurrentObject = Path.Combine(dirName, fileName),
                            });
                        }

                        current++;
                        await Task.Delay(1);
                    }
                }
            }

            if (Directory.Exists(iconBaseDirectoryBack))
            {
                Directory.Delete(iconBaseDirectoryBack, true);
            }
        }

        #endregion xivapi.com/Action
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public int Current { get; set; }
        public int Max { get; set; }
        public object CurrentObject { get; set; }
    }
}
