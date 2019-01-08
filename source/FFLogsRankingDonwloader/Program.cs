using System;
using System.IO;
using System.Text;
using ACT.UltraScouter.Models.FFLogs;
using FFXIV.Framework.Common;

namespace FFLogsRankingDonwloader
{
    public static class Program
    {
        public static void Main(
            string[] args)
        {
            // ダミーで FFXIV.Framework を読み込む
            var span = CommonHelper.GetRandomTimeSpan();

            if (args == null ||
                args.Length < 2)
            {
                return;
            }

            var apiKey = args[0];
            var fileName = args[1];
            var isOnlyHistogram = args.Length >= 3 ? bool.Parse(args[2]) : false;

            StatisticsDatabase.Instance.APIKey = apiKey;

            if (!isOnlyHistogram)
            {
                StatisticsDatabase.Instance.CreateAsync(fileName).Wait();
                Console.WriteLine($"[FFLogs] rankings downloaded.");
            }

            StatisticsDatabase.Instance.CreateHistogramAsync(fileName).Wait();
            Console.WriteLine($"[FFLogs] histgram analyzed.");

            File.WriteAllLines(
                $"{fileName}.timestamp.txt",
                new[] { DateTime.Now.ToString() },
                new UTF8Encoding(false));

            Console.WriteLine($"[FFLogs] database completed. save to {fileName}.");
        }
    }
}
