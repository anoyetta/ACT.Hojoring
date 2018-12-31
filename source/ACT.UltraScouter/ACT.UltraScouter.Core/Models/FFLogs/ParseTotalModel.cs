using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.FFXIVHelper;
using Newtonsoft.Json;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class ParseTotalModel :
        BindableBase
    {
        private bool isInitializing = false;

        public ParseTotalModel()
        {
            this.ParseList.CollectionChanged += (x, y) =>
            {
                if (!this.isInitializing)
                {
                    this.RaisePropertyChanged(nameof(this.BestPerfAvg));
                    this.RaisePropertyChanged(nameof(this.DPSAvg));
                    this.RaisePropertyChanged(nameof(this.Category));
                    this.RaisePropertyChanged(nameof(this.CategoryFillBrush));
                    this.RaisePropertyChanged(nameof(this.CategoryStrokeBrush));
                    this.RaisePropertyChanged(nameof(this.ExistsParses));
                }
            };
        }

        public static string CreateDataKey(
            string characterName,
            string server,
            FFLogsRegions region,
            Job characterJob)
            => $"{characterName ?? "Unknown"}-{server ?? "Unknown"}-{region}-{characterJob?.ToString() ?? "Unknown"}";

        public string DataKey => CreateDataKey(this.characterName, this.server, this.region, this.job);

        /// <summary>
        /// すべてのPropertiesの変更通知を発生させる
        /// </summary>
        public void RaiseAllPropertiesChanged()
        {
            foreach (var pi in this.GetType().GetProperties())
            {
                this.RaisePropertyChanged(pi.Name);
            }
        }

        private string characterName;

        public string CharacterName
        {
            get => this.characterName;
            set
            {
                if (this.SetProperty(ref this.characterName, value))
                {
                    this.RaisePropertyChanged(nameof(this.ExistsName));
                }
            }
        }

        public bool ExistsName => !string.IsNullOrEmpty(this.CharacterName);

        private string server;

        public string Server
        {
            get => this.server;
            set
            {
                if (this.SetProperty(ref this.server, value))
                {
                    this.RaisePropertyChanged(nameof(this.ServerToDisplay));
                }
            }
        }

        public string ServerToDisplay =>
            !string.IsNullOrEmpty(this.Server) ?
            $"({this.Server})" :
            null;

        private FFLogsRegions region = FFLogsRegions.JP;

        public FFLogsRegions Region
        {
            get => this.region;
            set => this.SetProperty(ref this.region, value);
        }

        private Job job = Jobs.Find(JobIDs.Unknown);

        public Job Job
        {
            get => this.job;
            set
            {
                if (this.SetProperty(ref this.job, value))
                {
                    this.RaisePropertyChanged(nameof(this.JobName));
                }
            }
        }

        public string JobName =>
            this.Job == null ? "All" :
                this.Job.ID == JobIDs.Unknown ?
                    string.Empty :
                    this.Job.NameEN;

        private DateTime timestamp = DateTime.MinValue;

        public DateTime Timestamp
        {
            get => this.timestamp;
            set => this.SetProperty(ref this.timestamp, value);
        }

        public ObservableCollection<ParseModel> ParseList { get; } = new ObservableCollection<ParseModel>();

        public bool ExistsParses => this.ParseList.Count > 0;

        public void AddRangeParse(
            IEnumerable<ParseModel> parses)
        {
            try
            {
                this.isInitializing = true;

                lock (this)
                {
                    this.ParseList.Clear();
                    this.ParseList.AddRange(parses);
                }
            }
            finally
            {
                this.isInitializing = false;
            }

            this.RaisePropertyChanged(nameof(this.BestPerfAvg));
            this.RaisePropertyChanged(nameof(this.DPSAvg));
            this.RaisePropertyChanged(nameof(this.Category));
            this.RaisePropertyChanged(nameof(this.CategoryFillBrush));
            this.RaisePropertyChanged(nameof(this.CategoryStrokeBrush));
            this.RaisePropertyChanged(nameof(this.ExistsParses));
        }

        public float BestPerfAvg =>
            this.ParseList.Any() ?
            this.ParseList.Average(x => x.Percentile) :
            0f;

        public float DPSAvg =>
            this.ParseList.Any() ?
            this.ParseList.Average(x => x.Total) :
            0f;

        public string Category => GetCategory(this.BestPerfAvg);

        public SolidColorBrush CategoryFillBrush => GetCategoryFillBrush(this.BestPerfAvg);

        public SolidColorBrush CategoryStrokeBrush => GetCategoryStrokeBrush(this.BestPerfAvg);

        public static string GetCategory(
            float perf)
        {
            if (perf >= 100)
            {
                return "A";
            }
            else if (perf < 100 && perf >= 95)
            {
                return "B";
            }
            else if (perf < 95 && perf >= 75)
            {
                return "C";
            }
            else if (perf < 75 && perf >= 50)
            {
                return "D";
            }
            else if (perf < 50 && perf >= 25)
            {
                return "E";
            }
            else
            {
                return "F";
            }
        }

        public static Color GetCategoryFillColor(
            float perf)
            => Settings.Instance.FFLogs.CategoryColorDictionary[GetCategory(perf)].FillColor;

        public static Color GetCategoryStrokeColor(
            float perf)
            => Settings.Instance.FFLogs.CategoryColorDictionary[GetCategory(perf)].StrokeColor;

        public static SolidColorBrush GetCategoryFillBrush(
            float perf)
            => GetCategoryFillColor(perf).ToBrush();

        public static SolidColorBrush GetCategoryStrokeBrush(
            float perf)
            => GetCategoryStrokeColor(perf).ToBrush();

        private static HttpClient httpClient;

        public HttpClient HttpClient
        {
            get
            {
                if (httpClient != null)
                {
                    return httpClient;
                }

                httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://www.fflogs.com/v1/");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                return httpClient;
            }
        }

        public HttpStatusCode HttpStatusCode { get; private set; }

        public string ResponseContent { get; private set; }

        private const string LoadingMessage = "Loading...";
        private const string NoAPIKeyMessage = "API Key Nothing";
        private const string TimeoutMessage = "Timeout";
        private const string NoDataMessage = "NO DATA";
        private string message = LoadingMessage;

        public string Message
        {
            get => this.message;
            private set => this.SetProperty(ref this.message, value);
        }

        private async void SetMessage(
            string message)
            => await WPFHelper.InvokeAsync(() => this.Message = message);

        private static volatile bool isDownloading = false;

        public async Task GetParseAsync(
            string characterName,
            string server,
            FFLogsRegions region,
            Job job,
            bool isTest = false)
        {
            // 前の処理の完了を1.5秒間待つ
            for (int i = 0; i < 15; i++)
            {
                if (!isDownloading)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(0.1));
            }

            if (isDownloading)
            {
                this.SetMessage(TimeoutMessage);
                return;
            }

            isDownloading = true;

            var code = HttpStatusCode.Continue;
            var json = string.Empty;
            var message = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(Settings.Instance.FFLogs.ApiKey))
                {
                    this.SetMessage(NoAPIKeyMessage);
                    Clear();
                    return;
                }

                if (!isTest)
                {
                    // 同じ条件でn分以内ならば再取得しない
                    if (characterName == this.CharacterName &&
                        server == this.Server &&
                        region == this.Region)
                    {
                        var interval = Settings.Instance.FFLogs.RefreshInterval;
                        if (!this.ExistsParses)
                        {
                            interval /= 2.0;
                        }

                        if (interval < 1.0d)
                        {
                            interval = 1.0d;
                        }

                        if ((DateTime.Now - this.Timestamp).TotalMinutes <= interval)
                        {
                            return;
                        }
                    }
                }

                this.SetMessage(LoadingMessage);

                var uri = string.Format(
                    "parses/character/{0}/{1}/{2}",
                    Uri.EscapeUriString(characterName),
                    Uri.EscapeUriString(server),
                    region.ToString());

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["timeframe"] = "historical";
                query["api_key"] = Settings.Instance.FFLogs.ApiKey;

                uri += $"?{query.ToString()}";

                var res = await this.HttpClient.GetAsync(uri);
                code = res.StatusCode;
                if (code != HttpStatusCode.OK)
                {
                    this.SetMessage(NoDataMessage);
                    Clear();
                    return;
                }

                json = await res.Content.ReadAsStringAsync();
                var parses = JsonConvert.DeserializeObject<ParseModel[]>(json);

                if (parses == null ||
                    parses.Length < 1)
                {
                    this.SetMessage(NoDataMessage);
                    Clear();
                    return;
                }

                var filter = default(Predicate<ParseModel>);
                if (job != null &&
                    parses.Any(x => string.Equals(x.Spec, job.NameEN, StringComparison.OrdinalIgnoreCase)))
                {
                    filter = (x) => string.Equals(x.Spec, job.NameEN, StringComparison.OrdinalIgnoreCase);
                }

                var bests =
                    from x in parses
                    where
                    filter?.Invoke(x) ?? true
                    orderby
                    x.EncounterID,
                    x.Percentile descending
                    group x by x.EncounterName into g
                    select
                    g.OrderByDescending(y => y.Percentile).First();

                await WPFHelper.InvokeAsync(() =>
                {
                    this.CharacterName = characterName;
                    this.Server = server;
                    this.Region = region;
                    this.Job = filter != null ? job : null;
                    this.AddRangeParse(bests);
                    this.Timestamp = DateTime.Now;

                    if (!bests.Any())
                    {
                        this.Message = NoDataMessage;
                    }

                    this.HttpStatusCode = code;
                    this.ResponseContent = json;
                });
            }
            finally
            {
                isDownloading = false;
            }

            async void Clear()
            {
                await WPFHelper.InvokeAsync(() =>
                {
                    this.CharacterName = characterName;
                    this.Server = server;
                    this.Region = region;
                    this.Job = job;
                    this.ParseList.Clear();
                    this.HttpStatusCode = code;
                    this.ResponseContent = json;
                });
            }
        }
    }
}
