using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FFXIV.Framework.Common;
using Microsoft.VisualBasic.FileIO;

namespace FFXIV.Framework.XIVHelper
{
    public class PCOrder
    {
        #region Singleton

        private static PCOrder instance;

        public static PCOrder Instance => instance ?? (instance = new PCOrder());

        private PCOrder()
        {
        }

        public static void Free() => instance = null;

        #endregion Singleton

        #region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        public IReadOnlyList<(JobIDs Job, int Order)> PCOrders => this.pcOrders;

        private readonly List<(JobIDs Job, int Order)> pcOrders = new List<(JobIDs Job, int Order)>();

        private static readonly string FileName = Path.Combine(
            DirectoryHelper.FindSubDirectory("resources"),
            "PCOrder.txt");

        public void Load()
        {
            this.pcOrders.Clear();

            if (!File.Exists(FileName))
            {
                return;
            }

            using (var sr = new StreamReader(FileName, new UTF8Encoding(false)))
            using (var parser = new TextFieldParser(sr)
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = new[] { " ", "\t", "," },
                CommentTokens = new[] { "#" },
                TrimWhiteSpace = true,
            })
            {
                while (!parser.EndOfData)
                {
                    var row = parser.ReadFields();

                    if (row != null &&
                        row.Length >= 2)
                    {
                        JobIDs job;
                        int order;

                        if (Enum.TryParse<JobIDs>(row[0], out job) &&
                            int.TryParse(row[1], out order))
                        {
                            this.pcOrders.Add((job, order));
                        }
                    }
                }
            }

            if (this.pcOrders.Count > 0)
            {
                AppLogger.Trace("pc orders loaded.");
            }
        }
    }
}
