using System.Net;
using System.Runtime.Serialization.Json;
using FFXIV.Framework.Common;
using FFXIV.Framework.Globalization;

namespace XIVDBDownloader.Models
{
    public abstract class XIVDBApiBase<T>
    {
        public T ResultList { get; private set; }
        public abstract string Uri { get; }

        public T GET(
            Locales language)
        {
            // TLSプロトコルを設定する
            EnvironmentHelper.SetTLSProtocol();

            var resultList = default(T);

            if (string.IsNullOrEmpty(this.Uri))
            {
                this.ResultList = resultList;
                return resultList;
            }

            var uri = this.Uri + "&language=" + language.ToString().ToLower();
#if DEBUG
            uri += "&pretty=1";
#endif

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.ContentType = "application/json";
            request.Method = "GET";

            using (var response = request.GetResponse())
            using (var st = response.GetResponseStream())
            {
#if false
                using (var sr = new StreamReader(st))
                using (var sw = new StreamWriter("action_raw.txt", false, new UTF8Encoding(false)))
                {
                    sw.Write(sr.ReadToEnd());
                }
#else
                var serializer = new DataContractJsonSerializer(typeof(T));
                resultList = (T)serializer.ReadObject(st);
#endif
            }

            this.ResultList = resultList;
            return resultList;
        }
    }
}
