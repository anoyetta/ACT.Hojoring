using System;
using System.IO;
using System.Text;
using Hjson;
using Newtonsoft.Json;

namespace ACT.Hojoring.Activator.Models
{
    internal class AccountList
    {
        #region Lazy Singleton

        private static readonly Lazy<AccountList> LazyAccountList = new Lazy<AccountList>(() => new AccountList());

        internal static AccountList Instance => LazyAccountList.Value;

        private AccountList()
        {
        }

        #endregion Lazy Singleton

        internal Account[] Load(
            string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                json = "{}";
            }

            var data = default(Account[]);

            data = JsonConvert.DeserializeObject(
                HjsonValue.Parse(json).ToString(),
                typeof(Account[]),
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                }) as Account[];

            this.CurrentList = data;

            return data;
        }

        internal void Save(
            string fileName,
            Account[] currentList = null)
        {
            if (currentList == null)
            {
                currentList = this.CurrentList;
            }

            var json = JsonConvert.SerializeObject(
                currentList,
                Formatting.Indented,
                new JsonSerializerSettings()
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                });

            var dir = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(dir))
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            File.WriteAllText(
                fileName,
                json + Environment.NewLine,
                new UTF8Encoding(false));
        }

        internal Account[] CurrentList { get; set; }
    }
}
