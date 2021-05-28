using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FFXIV.Framework.Common;
using FFXIV.Framework.XIVHelper;
using Newtonsoft.Json;
using NLog;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class StatisticsDatabase
    {
        #region Singleton

        private static StatisticsDatabase instance;

        public static StatisticsDatabase Instance => instance ?? (instance = new StatisticsDatabase());

        private StatisticsDatabase()
        {
        }

        #endregion Singleton

        public string APIKey { get; set; }

        public Logger Logger { get; set; }

        private ZonesModel[] zones;
        private ClassesModel classes;

        public Dictionary<int, BasicEntryModel> SpecDictionary { get; set; }

        private static HttpClient httpClient;

        public HttpClient HttpClient
        {
            get
            {
                if (httpClient != null)
                {
                    return httpClient;
                }

                ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls;
                ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls11;
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://www.fflogs.com/v1/");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                return httpClient;
            }
        }

        public async Task CreateAsync(
            string rankingFileName,
            int targetZoneID = 0,
            string difficulty = null)
        {
            if (string.IsNullOrEmpty(this.APIKey))
            {
                return;
            }

            try
            {
                await this.LoadZonesAsync();
                await this.LoadClassesAsync();
                await this.CreateRankingsAsync(rankingFileName, targetZoneID, difficulty);
            }
            catch (Exception ex)
            {
                this.Log("[FFLogs] error statistics database.", ex);
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                await this.LoadRankingsAsync();
            }
            catch (Exception ex)
            {
                this.Log("[FFLogs] error statistics database.", ex);
            }
        }

        public async Task LoadZonesAsync()
        {
            var uri = "zones";
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["api_key"] = this.APIKey;
            uri += $"?{query.ToString()}";

            var res = await this.HttpClient.GetAsync(uri);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                return;
            }

            var json = await res.Content.ReadAsStringAsync();
            this.zones = JsonConvert.DeserializeObject<ZonesModel[]>(json);

            this.Log("[FFLogs] zones loaded.");
        }

        public async Task LoadClassesAsync()
        {
            var uri = "classes";
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["api_key"] = this.APIKey;
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
                this.SpecDictionary = this.classes.Specs.ToDictionary(x => x.ID);
                this.Log("[FFLogs] classes loaded.");
            }
        }

        public async Task CreateRankingsAsync(
            string rankingFileName,
            int targetZoneID = 0,
            string difficulty = null)
        {
            this.InitializeRankingsDatabase(rankingFileName);

            var targetEncounters = default(BasicEntryModel[]);
            if (targetZoneID == 0)
            {
                targetEncounters = this.zones
                    .OrderByDescending(x => x.ID)
                    .FirstOrDefault()?
                    .Enconters;
            }
            else
            {
                targetEncounters = this.zones
                    .FirstOrDefault(x => x.ID == targetZoneID)?
                    .Enconters;
            }

            var rankingBuffer = new List<RankingModel>(10000);

            foreach (var encounter in targetEncounters)
            {
                this.Log($@"[FFLogs] new rankings ""{encounter.Name}"".");

                var page = 1;
                var count = 0;
                var rankings = default(RankingsModel);

                do
                {
                    var uri = $"rankings/encounter/{encounter.ID}";
                    var query = HttpUtility.ParseQueryString(string.Empty);
                    query["api_key"] = this.APIKey;

                    if (!string.IsNullOrEmpty(difficulty))
                    {
                        query["difficulty"] = difficulty;
                    }

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
                            count += rankings.Count;
                            var targets = rankings.Rankings;
                            targets.AsParallel().ForAll(item =>
                            {
                                item.Database = this;
                                item.EncounterName = encounter.Name;

                                if (this.SpecDictionary != null &&
                                    this.SpecDictionary.ContainsKey(item.SpecID))
                                {
                                    item.Spec = this.SpecDictionary[item.SpecID].Name;
                                }
                            });

                            rankingBuffer.AddRange(rankings.Rankings);
                        }

                        if (page % 100 == 0)
                        {
                            this.InsertRanking(rankingFileName, rankingBuffer);
                            rankingBuffer.Clear();
                            this.Log($@"[FFLogs] new rankings downloaded. ""{encounter.Name}"" page={page} count={count}.");

#if DEBUG
                            // デバッグモードならば100ページで抜ける
                            break;
#endif
                        }

                        page++;
                    }
                    else
                    {
                        this.LogError(
                            $"[FFLogs] Error, REST API Response not OK. status_code={res.StatusCode}");
                        this.LogError(await res?.Content.ReadAsStringAsync());
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(0.10));
                } while (rankings != null && rankings.HasMorePages);

                if (rankingBuffer.Any())
                {
                    this.InsertRanking(rankingFileName, rankingBuffer);
                    rankingBuffer.Clear();
                    this.Log($@"[FFLogs] new rankings downloaded. ""{encounter.Name}"" page={page} count={count}.");
                }
            }

            this.Log($@"[FFLogs] new rankings downloaded.");
        }

        public async Task CreateHistogramAsync(
            string rankingFileName)
        {
            if (!File.Exists(rankingFileName))
            {
                return;
            }

            using (var cn = this.OpenRankingDatabaseConnection(rankingFileName))
            {
                using (var tran = cn.BeginTransaction())
                {
                    using (var cm = cn.CreateCommand())
                    {
                        cm.Transaction = tran;

                        var q = new StringBuilder();
                        q.AppendLine("DELETE FROM histograms;");
                        cm.CommandText = q.ToString();
                        await cm.ExecuteNonQueryAsync();
                    }

                    tran.Commit();
                }

                using (var db = new DataContext(cn))
                using (var tran = cn.BeginTransaction())
                {
                    db.Transaction = tran;
                    var rankings = db.GetTable<RankingModel>().ToArray();

                    var averages =
                        from x in rankings
                        group x by
                        x.CharacterHash
                        into g
                        select new
                        {
                            SpecName = g.First().Spec,
                            DPSAverage = g.Average(z => z.Total),
                            Rank = ((int)(g.Average(z => z.Total)) / 100) * 100,
                        };

                    var histograms =
                        from x in averages
                        group x by new
                        {
                            x.SpecName,
                            x.Rank
                        }
                        into g
                        select new
                        {
                            g.Key.SpecName,
                            g.Key.Rank,
                            RankFrom = g.Key.Rank,
                            Frequency = (double)g.Count(),
                        };

                    var id = 1;
                    var specs =
                        from x in histograms
                        orderby
                        x.SpecName,
                        x.Rank
                        group x by
                        x.SpecName;

                    var entities = new List<HistogramModel>(histograms.Count());

                    foreach (var spec in specs)
                    {
                        var totalCount = spec.Sum(x => x.Frequency);
                        var count = 0d;
                        var rankMin = spec.Min(x => x.Rank);
                        var rankMax = spec.Max(x => x.Rank);

                        for (int i = rankMin; i <= rankMax; i += 100)
                        {
                            var entry = spec.FirstOrDefault(x => x.Rank == i);
                            var f = entry?.Frequency ?? 0;

                            var hist = new HistogramModel()
                            {
                                ID = id++,
                                SpecName = spec.Key,
                                Rank = i,
                                RankFrom = i,
                                Frequency = f,
                                FrequencyPercent = round(f / totalCount * 100d),
                                RankPercentile = round(count / totalCount * 100d),
                            };

                            entities.Add(hist);
                            count += f;
                        }
                    }

                    var table = db.GetTable<HistogramModel>();
                    table.InsertAllOnSubmit<HistogramModel>(entities);
                    db.SubmitChanges();

                    // ランキングテーブルを消去する
                    using (var cm = cn.CreateCommand())
                    {
                        cm.Transaction = tran;

                        var q = new StringBuilder();
                        q.AppendLine("DELETE FROM rankings;");
                        cm.CommandText = q.ToString();
                        await cm.ExecuteNonQueryAsync();
                    }

                    tran.Commit();
                }

                // DBを最適化する
                using (var cm = cn.CreateCommand())
                {
                    var q = new StringBuilder();
                    q.AppendLine("VACUUM;");
                    q.AppendLine("PRAGMA Optimize;");
                    cm.CommandText = q.ToString();
                    await cm.ExecuteNonQueryAsync();
                }
            }

            double round(double value)
            {
                return float.Parse(value.ToString("N3"));
            }
        }

        private static readonly object DatabaseAccessLocker = new object();

        public async Task LoadRankingsAsync()
        {
            const string TimestampFileUri = @"https://drive.google.com/uc?id=1bauam699-r3vfVgFLsUOSrVUnUy2BWsc&export=download";
            const string DatabaseFileUri = @"https://drive.google.com/uc?id=1PZ8oPbk0XLgODI_PwoEXjAZllY2upzbm&export=download";

            ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls;
            ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls11;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var timestamp = $"{this.RankingDatabaseFileName}.timestamp.txt";
            var timestampTemp = $"{this.RankingDatabaseFileName}.timestamp_temp.txt";
            var database = this.RankingDatabaseFileName;
            var databaseTemp = this.RankingDatabaseFileName + ".temp";

            try
            {
                using (var client = new WebClient())
                {
                    deleteFile(timestampTemp);
                    await client.DownloadFileTaskAsync(new Uri(TimestampFileUri), timestampTemp);

                    DateTime oldTimestamp = DateTime.MinValue, newTimestamp = DateTime.MinValue;
                    if (File.Exists(timestamp))
                    {
                        DateTime.TryParse(File.ReadAllText(timestamp), out oldTimestamp);
                    }

                    DateTime.TryParse(File.ReadAllText(timestampTemp), out newTimestamp);

                    if (oldTimestamp >= newTimestamp)
                    {
                        this.Log("[FFLogs] statistics database is up-to-date.");
                        return;
                    }

                    deleteFile(databaseTemp);
                    await client.DownloadFileTaskAsync(new Uri(DatabaseFileUri), databaseTemp);

                    lock (DatabaseAccessLocker)
                    {
                        File.Copy(databaseTemp, database, true);
                        File.Copy(timestampTemp, timestamp, true);
                    }

                    this.Log("[FFLogs] statistics database is updated.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[FFLogs] error downloding statistics database.");
            }
            finally
            {
                deleteFile(timestampTemp);
                deleteFile(databaseTemp);
            }

            void deleteFile(string path)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private readonly Dictionary<string, HistogramsModel> HistogramDictionary = new Dictionary<string, HistogramsModel>(64);

        public HistogramsModel GetHistogram(
            string jobName)
            => this.GetHistogram(Jobs.FindFromName(jobName));

        public HistogramsModel GetHistogram(
            Job job)
        {
            var jobName = job?.NameEN ?? string.Empty;

            var result = new HistogramsModel()
            {
                SpecName = jobName,
            };

            if (string.IsNullOrEmpty(jobName))
            {
                return result;
            }

            if (!File.Exists(this.RankingDatabaseFileName))
            {
                return result;
            }

            if (this.HistogramDictionary.ContainsKey(jobName))
            {
                return this.HistogramDictionary[jobName];
            }

            lock (DatabaseAccessLocker)
            {
                using (var cn = this.OpenRankingDatabaseConnection(this.RankingDatabaseFileName))
                using (var db = new DataContext(cn))
                {
                    result.Ranks =
                        db.GetTable<HistogramModel>()
                        .Where(x => x.SpecName == result.SpecName)
                        .OrderBy(x => x.Rank);
                }
            }

            if (result.Ranks.Any())
            {
                result.MaxRank = result.Ranks.Max(x => x.Rank);
                result.MinRank = result.Ranks.Min(x => x.Rank);
                result.MaxFrequencyPercent = Math.Ceiling(result.Ranks.Max(x => x.FrequencyPercent));

                foreach (var rank in result.Ranks)
                {
                    rank.FrequencyRatioToMaximum = rank.FrequencyPercent / result.MaxFrequencyPercent;
                }
            }

            this.HistogramDictionary[jobName] = result;

            return result;
        }

        private string RankingDatabaseFileName =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"anoyetta\ACT\fflogs.db");

        private string RankingDatabaseMasterFileName =>
            Path.Combine(
                DirectoryHelper.FindSubDirectory("resources"),
                @"fflogs.master.db");

        private SQLiteConnection OpenRankingDatabaseConnection(
            string rankingDatabaseFileName)
        {
            var b = new SQLiteConnectionStringBuilder()
            {
                DataSource = rankingDatabaseFileName
            };

            var cn = new SQLiteConnection(b.ToString());
            cn.Open();
            return cn;
        }

        private void InitializeRankingsDatabase(
            string rankingDatabaseFileName)
        {
            FileHelper.CreateDirectory(rankingDatabaseFileName);

            if (!File.Exists(rankingDatabaseFileName))
            {
                File.Copy(
                    this.RankingDatabaseMasterFileName,
                    rankingDatabaseFileName,
                    true);
            }

            using (var cn = this.OpenRankingDatabaseConnection(rankingDatabaseFileName))
            using (var tran = cn.BeginTransaction())
            {
                using (var cm = cn.CreateCommand())
                {
                    cm.Transaction = tran;

                    var q = new StringBuilder();
                    q.AppendLine("DELETE FROM rankings;");
                    q.AppendLine("DELETE FROM histograms;");
                    cm.CommandText = q.ToString();
                    cm.ExecuteNonQuery();
                }

                tran.Commit();
            }
        }

        private void InsertRanking(
            string rankingDatabaseFileName,
            IEnumerable<RankingModel> rankings)
        {
            if (!rankings.Any())
            {
                return;
            }

            using (var cn = this.OpenRankingDatabaseConnection(rankingDatabaseFileName))
            using (var tran = cn.BeginTransaction())
            {
                using (var cm = cn.CreateCommand())
                {
                    cm.Transaction = tran;

                    cm.CommandText =
                        $"INSERT INTO rankings " +
                        $"(encounter_name, character_hash, spec_name, region, total) VALUES " +
                        $"(@encounter_name, @character_hash, @spec_name, @region, @total);";

                    foreach (var entry in rankings)
                    {
                        cm.Parameters.Clear();

                        cm.Parameters.AddWithValue("@encounter_name", entry.EncounterName);
                        cm.Parameters.AddWithValue("@character_hash", entry.CreateCharacterHash());
                        cm.Parameters.AddWithValue("@spec_name", entry.Spec);
                        cm.Parameters.AddWithValue("@region", entry.Region);
                        cm.Parameters.AddWithValue("@total", entry.Total);

                        cm.ExecuteNonQuery();
                    }
                }

                tran.Commit();
            }
        }

        private async Task<IEnumerable<RankingModel>> LoadRankingsFileAsync()
        {
            if (!File.Exists(this.RankingDatabaseFileName))
            {
                return null;
            }

            using (var sr = new StreamReader(this.RankingDatabaseFileName, new UTF8Encoding(false)))
            {
                var json = await sr.ReadToEndAsync();
                return JsonConvert.DeserializeObject<List<RankingModel>>(json);
            }
        }

        private void Log(
            string message,
            Exception ex = null)
        {
            if (ex == null)
            {
                this.Logger?.Trace(message);
                Console.WriteLine(message);
            }
            else
            {
                this.Logger?.Error(ex, message);
                Console.WriteLine(message);
                Console.WriteLine(ex);
            }
        }

        private void LogError(
            string message,
            Exception ex = null)
        {
            if (ex == null)
            {
                this.Logger?.Error(ex, message);
                Console.WriteLine(message);
            }
            else
            {
                this.Logger?.Error(ex, message);
                Console.WriteLine(message);
                Console.WriteLine(ex);
            }
        }
    }
}
