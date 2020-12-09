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
using System.Windows.Media.Imaging;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV.Framework.XIVHelper;
using Newtonsoft.Json;
using NLog;
using Prism.Mvvm;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class ParseTotalModel :
        BindableBase
    {
        #region Logger

        private static Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

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

        public string DataKey => CreateDataKey(this.CharacterNameFull, this.server, this.region, this.job);

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
            private set
            {
                if (this.SetProperty(ref this.characterName, value))
                {
                    this.RaisePropertyChanged(nameof(this.ExistsName));
                }
            }
        }

        private string characterNameFull;

        public string CharacterNameFull
        {
            get => this.characterNameFull;
            set
            {
                if (this.SetProperty(ref this.characterNameFull, value))
                {
                    this.RefreshCharacterName();
                    this.RaisePropertyChanged(nameof(this.IsSpecial));
                }
            }
        }

        public void RefreshCharacterName()
        {
            this.CharacterName = CombatantEx.NameToInitial(this.characterNameFull, ConfigBridge.Instance.PCNameStyle);
        }

        private static readonly string[] SpecialNameHash = new[]
        {
            "55DEC1C85CCEE22E636C10D9B966F39C",
            "B4222EFB5F74777300623471742B7C9C",
            "669EE61736B3F08180610BAD05F02FFD",
            "70478F65D38F55B8C00C70017AFA63AC"
        };

        public bool IsSpecial => SpecialNameHash.Contains(this.CharacterNameFull.GetMD5());

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
                    this.RaisePropertyChanged(nameof(this.JobIcon));
                    this.RaisePropertyChanged(nameof(this.IsExistsJobIcon));
                }
            }
        }

        public string JobName => this.GetJobName();

        private string GetJobName()
        {
            var text = string.Empty;

            if (this.Job == null)
            {
                text = "All";
            }
            else
            {
                if (this.Job.ID == JobIDs.Unknown)
                {
                    text = string.Empty;
                }
                else
                {
                    text = this.Job.GetName(Settings.Instance.UILocale);
                }
            }

            return text;
        }

        public BitmapSource JobIcon => JobIconDictionary.Instance.GetIcon(this.job?.ID ?? JobIDs.Unknown);

        public bool IsExistsJobIcon => JobIconDictionary.Instance.Icons.ContainsKey(this.job?.ID ?? JobIDs.Unknown);

        private string bestJobName;

        public string BestJobName
        {
            get => this.bestJobName;
            set
            {
                if (this.SetProperty(ref this.bestJobName, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsExistsBestJobName));
                    this.RaisePropertyChanged(nameof(this.BestJobIcon));
                }
            }
        }

        public BitmapSource BestJobIcon
        {
            get
            {
                var result = default(BitmapSource);

                var job = Jobs.FindFromName(this.bestJobName);
                if (job != null)
                {
                    result = JobIconDictionary.Instance.GetIcon(job.ID);
                }

                return result;
            }
        }

        public bool IsExistsBestJobName => !string.IsNullOrEmpty(this.bestJobName);

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

        private static readonly HistogramsModel EmptyHistogram = new HistogramsModel();

        private HistogramsModel histogram;

        public HistogramsModel Histogram
        {
            get => this.histogram;
            set
            {
                if (this.SetProperty(ref this.histogram, value))
                {
                    this.histogram.RaiseAllPropertiesChanged();
                }
            }
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

                var lang = "www";
                switch (Settings.Instance.UILocale)
                {
                    case FFXIV.Framework.Globalization.Locales.JA:
                        lang = "ja";
                        break;

                    case FFXIV.Framework.Globalization.Locales.FR:
                        lang = "fr";
                        break;

                    case FFXIV.Framework.Globalization.Locales.DE:
                        lang = "de";
                        break;

                    case FFXIV.Framework.Globalization.Locales.KO:
                        lang = "ko";
                        break;

                    case FFXIV.Framework.Globalization.Locales.TW:
                    case FFXIV.Framework.Globalization.Locales.CN:
                        lang = "cn";
                        break;
                }

                httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri($"https://{lang}.fflogs.com/v1/");
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
        private const string NoCharacterNameMessage = "Character Name Nothing";
        private const string NoServerMessage = "Server Nothing";
        private const string ErrorMessage = "ERROR";
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

        private static object DownlodingLocker = new object();
        private static volatile bool isDownloading = false;

        private const int CheckLimit = 2500;
        private const int CheckInterval = 250;
        private static readonly Random Random = new Random(DateTime.Now.Second);
        private FFLogsPartitions previousPartition = FFLogsPartitions.Current;

        private static readonly string ServerPrefixKR = "Kr";

        public async Task GetParseAsync(
            string characterName,
            string server,
            FFLogsRegions region,
            Job job,
            bool isTest = false)
        {
            // 前の処理の完了を待つ
            for (int i = 0; i < (CheckLimit / CheckInterval); i++)
            {
                if (!isDownloading)
                {
                    break;
                }

                var wait = Random.Next(CheckInterval - 50, CheckInterval);
                await Task.Delay(wait);
            }

            lock (DownlodingLocker)
            {
                if (isDownloading)
                {
                    return;
                }

                isDownloading = true;
            }

            var code = HttpStatusCode.Continue;
            var json = string.Empty;
            var message = string.Empty;

            try
            {
                // サーバー名からKrプレフィックスを除去する
                if (server.StartsWith(ServerPrefixKR))
                {
                    server = server.Replace(ServerPrefixKR, string.Empty);
                }

                if (string.IsNullOrEmpty(Settings.Instance.FFLogs.ApiKey))
                {
                    this.SetMessage(NoAPIKeyMessage);
                    Clear();
                    return;
                }

                if (string.IsNullOrEmpty(characterName))
                {
                    this.SetMessage(NoCharacterNameMessage);
                    Clear();
                    return;
                }

                if (string.IsNullOrEmpty(server))
                {
                    this.SetMessage(NoServerMessage);
                    Clear();
                    return;
                }

                var partition = Settings.Instance.FFLogs.Partition;

                if (!isTest)
                {
                    // 同じ条件でn分以内ならば再取得しない
                    if (characterName == this.CharacterNameFull &&
                        server == this.Server &&
                        region == this.Region &&
                        partition == this.previousPartition)
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

                this.Timestamp = DateTime.Now;
                this.SetMessage(LoadingMessage);

                var uri = string.Format(
                    "parses/character/{0}/{1}/{2}",
                    Uri.EscapeUriString(characterName),
                    Uri.EscapeUriString(server),
                    region.ToString());

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["timeframe"] = "historical";
                query["api_key"] = Settings.Instance.FFLogs.ApiKey;

                if (partition != FFLogsPartitions.Current)
                {
                    query["partition"] = ((int)partition).ToString();
                }

                uri += $"?{query.ToString()}";

                this.previousPartition = partition;

                var parses = default(ParseModel[]);

                try
                {
                    var res = await this.HttpClient.GetAsync(uri);
                    code = res.StatusCode;
                    if (code != HttpStatusCode.OK)
                    {
                        if (code != HttpStatusCode.BadRequest)
                        {
                            this.SetMessage($"{NoDataMessage} ({(int)code})");
                        }
                        else
                        {
                            this.SetMessage(NoDataMessage);
                        }

                        Clear();
                        return;
                    }

                    json = await res.Content.ReadAsStringAsync();
                    parses = JsonConvert.DeserializeObject<ParseModel[]>(json);

                    if (parses == null ||
                        parses.Length < 1)
                    {
                        this.SetMessage(NoDataMessage);
                        Clear();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        ex,
                        $"Error FFLogs API. charactername={characterName} server={server} region={region} uri={uri}");

                    this.SetMessage(ErrorMessage);
                    Clear();
                    return;
                }

                var filter = default(Predicate<ParseModel>);
                if (job != null &&
                    parses.Any(x => x.IsEqualToSpec(job)))
                {
                    filter = x => x.IsEqualToSpec(job);
                }

                var bests =
                    from x in parses
                    where
                    (filter?.Invoke(x) ?? true) &&
                    x.Difficulty >= (int)Settings.Instance.FFLogs.Difficulty
                    orderby
                    x.EncounterID,
                    x.Percentile descending
                    group x by x.EncounterName into g
                    select
                    g.OrderByDescending(y => y.Percentile).First();

                var bestJob = string.Empty;
                if (filter == null)
                {
                    bestJob = parses
                        .OrderByDescending(x => x.Percentile)
                        .FirstOrDefault()?
                        .Spec;
                }

                // Histogramを編集する
                var histogram = default(HistogramsModel);
                if (Settings.Instance.FFLogs.VisibleHistogram)
                {
                    histogram = filter != null ?
                        StatisticsDatabase.Instance.GetHistogram(job) :
                        StatisticsDatabase.Instance.GetHistogram(bestJob);
                }

                if (histogram == null)
                {
                    histogram = EmptyHistogram;
                }

                await WPFHelper.InvokeAsync(() =>
                {
                    this.CharacterNameFull = characterName;
                    this.RefreshCharacterName();
                    this.Server = server;
                    this.Region = region;
                    this.Job = filter != null ? job : null;
                    this.BestJobName = bestJob;
                    this.AddRangeParse(bests);

                    this.Histogram = histogram;

                    foreach (var rank in histogram.Ranks)
                    {
                        rank.IsCurrent = false;
                    }

                    var currentRank = histogram.Ranks
                        .OrderByDescending(x => x.Rank)
                        .FirstOrDefault(x => x.Rank <= this.DPSAvg);
                    if (currentRank != null)
                    {
                        currentRank.IsCurrent = true;
                    }

                    if (!bests.Any())
                    {
                        this.Message = NoDataMessage;
                    }

                    this.HttpStatusCode = code;
                    this.ResponseContent = json;
                });
            }
            catch (Exception)
            {
                Clear();
                throw;
            }
            finally
            {
                lock (DownlodingLocker)
                {
                    isDownloading = false;
                }
            }

            async void Clear()
            {
                await WPFHelper.InvokeAsync(() =>
                {
                    this.CharacterNameFull = characterName;
                    this.RefreshCharacterName();
                    this.Server = server;
                    this.Region = region;
                    this.Job = job;
                    this.BestJobName = string.Empty;
                    this.ParseList.Clear();
                    this.Histogram = EmptyHistogram;
                    this.HttpStatusCode = code;
                    this.ResponseContent = json;
                });
            }
        }
    }
}
