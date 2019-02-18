using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using ACT.UltraScouter.Models.FFLogs;
using FFXIV.Framework.Common;

namespace FFLogsRankingDonwloader
{
    public static class Program
    {
        static Program()
        {
            AssemblyResolver.Instance.Initialize();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Main(
            string[] args)
        {
            // ダミーで FFXIV.Framework を読み込む
            var span = CommonHelper.GetRandomTimeSpan(0.25);

            if (args == null ||
                args.Length < 2)
            {
                return;
            }

            var apiKey = args[0];
            var fileName = args[1];
            var isOnlyHistogram = args.Length >= 3 ? bool.Parse(args[2]) : false;
            var targetZoneID = args.Length >= 4 ? int.Parse(args[3]) : 0;

            StatisticsDatabase.Instance.APIKey = apiKey;

            if (!isOnlyHistogram)
            {
                StatisticsDatabase.Instance.CreateAsync(fileName, targetZoneID).Wait();
                Console.WriteLine($"[FFLogs] rankings downloaded.");
            }

            StatisticsDatabase.Instance.CreateHistogramAsync(fileName).Wait();
            Console.WriteLine($"[FFLogs] histgram analyzed.");

            File.WriteAllLines(
                $"{fileName}.timestamp.txt",
                new[] { DateTime.Now.ToString() },
                new UTF8Encoding(false));

            Console.WriteLine($"[FFLogs] database completed. save to {fileName}.");

            Thread.Sleep(span);
            Environment.Exit(0);
        }
    }
}
