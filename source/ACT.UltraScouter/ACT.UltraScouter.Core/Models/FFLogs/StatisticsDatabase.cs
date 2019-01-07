using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using ACT.UltraScouter.Config;
using FFXIV.Framework.Common;
using Newtonsoft.Json;
using NLog;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class StatisticsDatabase
    {
        #region Logger

        private static Logger Logger => AppLog.DefaultLogger;

        #endregion Logger

        #region Singleton

        private static StatisticsDatabase instance;

        public static StatisticsDatabase Instance => instance ?? (instance = new StatisticsDatabase());

        private StatisticsDatabase()
        {
        }

        #endregion Singleton

        private ZonesModel[] zones;
        private ClassesModel classes;
        private RankingDatabase rankingDatabase;

        private Config.FFLogs Config => Settings.Instance.FFLogs;

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

        public async Task LoadAsync()
        {
            if (string.IsNullOrEmpty(this.Config.ApiKey))
            {
                return;
            }

            try
            {
                await this.LoadZonesAsync();
                await this.LoadClassesAsync();
                await this.LoadRankingsAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[FFLogs] error statistics database.");
            }
        }

        public async Task LoadZonesAsync()
        {
            var uri = "zones";
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["api_key"] = Settings.Instance.FFLogs.ApiKey;
            uri += $"?{query.ToString()}";

            var res = await this.HttpClient.GetAsync(uri);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                return;
            }

            var json = await res.Content.ReadAsStringAsync();
            this.zones = JsonConvert.DeserializeObject<ZonesModel[]>(json);

            Logger.Trace("[FFLogs] zones loaded.");
        }

        public async Task LoadClassesAsync()
        {
            var uri = "classes";
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["api_key"] = Settings.Instance.FFLogs.ApiKey;
            uri += $"?{query.ToString()}";

            var res = await this.HttpClient.GetAsync(uri);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                return;
            }

            var json = await res.Content.ReadAsStringAsync();
            var classes = JsonConvert.DeserializeObject<ClassesModel[]>(json);
            if (classes != null &&
                classes.Any())
            {
                this.classes = classes.FirstOrDefault();
                Logger.Trace("[FFLogs] classes loaded.");
            }
        }

        public async Task LoadRankingsAsync()
        {
            this.rankingDatabase = new RankingDatabase()
            {
                SpecDictionary = this.classes.Specs.ToDictionary(x => x.ID)
            };

            var targetEncounters = this.zones
                .OrderByDescending(x => x.ID)
                .FirstOrDefault()?
                .Enconters;

            foreach (var encounter in targetEncounters)
            {
                var rankings = default(RankingsModel);
                var page = 1;

                do
                {
                    var uri = $"rankings/encounter/{encounter.ID}";
                    var query = HttpUtility.ParseQueryString(string.Empty);
                    query["api_key"] = Settings.Instance.FFLogs.ApiKey;
                    query["page"] = page.ToString();
                    uri += $"?{query.ToString()}";

                    rankings = null;
                    var res = await this.HttpClient.GetAsync(uri);
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        rankings = JsonConvert.DeserializeObject<RankingsModel>(json);
                        if (rankings != null)
                        {
                            this.rankingDatabase.AddRankings(
                                encounter.Name,
                                rankings.Rankings);
                        }

                        if (page % 100 == 0)
                        {
                            Logger.Trace($"[FFLogs] rankings loaded. {encounter.Name} page {page}.");
                        }

                        page++;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(0.51));
                } while (rankings != null && rankings.HasMorePages);
            }

            Logger.Trace("[FFLogs] rankings loaded.");
        }
    }
}
