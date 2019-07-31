using System;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ACT.Hojoring.Activator.Models;

namespace ACT.Hojoring.Activator
{
    internal class ActivationManager
    {
        #region Lazy Singleton

        private static readonly Lazy<ActivationManager> LazyInstance = new Lazy<ActivationManager>(() => new ActivationManager());

        internal static ActivationManager Instance => LazyInstance.Value;

        private ActivationManager()
        {
        }

        #endregion Lazy Singleton

        private static readonly object LockObject = new object();

        private readonly Lazy<System.Timers.Timer> LazyTimer = new Lazy<System.Timers.Timer>(() =>
        {
            var timer = new System.Timers.Timer(TimeSpan.FromMinutes(60 * 3).TotalMilliseconds);
            timer.Elapsed += (_, __) => RefreshAccountList();
            return timer;
        });

        internal ActivationStatus CurrentStatus { get; set; } = ActivationStatus.Loading;

        internal void Start()
        {
            lock (LockObject)
            {
                if (this.LazyTimer.Value.Enabled)
                {
                    return;
                }

                ServicePointManager.DefaultConnectionLimit = 32;
                ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls;
                ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls11;
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                Task.Run(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));

                    if (this.LazyTimer.Value.Enabled)
                    {
                        return;
                    }

                    this.LazyTimer.Value.Start();
                    RefreshAccountList();
                });
            }
        }

        private DateTime activationTimestamp = DateTime.Now;
        private string activationAccount = string.Empty;

        internal Action ActivationDeniedCallback { get; set; }

        internal bool TryActivation(
            string name,
            string server,
            string guild)
        {
            var now = DateTime.Now;
            var account = $"{name}-{server}-{guild}";

            if (this.activationAccount != account ||
                (this.activationTimestamp - now).TotalMinutes > 60)
            {
                this.activationAccount = account;
                this.Activation(name, server, guild);

                if (this.CurrentStatus != ActivationStatus.Loading)
                {
                    this.activationTimestamp = now;
                }
            }

            switch (this.CurrentStatus)
            {
                case ActivationStatus.Loading:
                case ActivationStatus.Allow:
                    return true;

                default:
                    this.ActivationDeniedCallback?.Invoke();
                    return false;
            }
        }

        internal bool Activation(
            string name,
            string server,
            string guild)
        {
            switch (this.CurrentStatus)
            {
                case ActivationStatus.Loading:
                    return true;

                case ActivationStatus.Error:
                    return false;
            }

            var result = false;

            lock (LockObject)
            {
                if (!AccountList.Instance.CurrentList.Any())
                {
                    return result;
                }

                var isMatch = AccountList.Instance.CurrentList.Any(x => x.IsMatch(
                    name,
                    server,
                    guild));

                this.CurrentStatus = isMatch ? ActivationStatus.Deny : ActivationStatus.Allow;
                result = !isMatch;
            }

            var status = result ? "allow" : "deny";
            Logger.Instance.Write(
                $"n={name} s={server} g={guild} is {status}.");

            return result;
        }

        private static async void RefreshAccountList()
        {
            try
            {
                var json = string.Empty;

                using (var client = new HttpClient(new WebRequestHandler()
                {
                    CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore),
                }))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Hojoring/1.0");
                    client.Timeout = TimeSpan.FromSeconds(30);

                    json = await client.GetStringAsync(AccountListUrl);
                }

                lock (LockObject)
                {
                    AccountList.Instance.Load(json);
                }

                ActivationManager.Instance.CurrentStatus = ActivationStatus.Allow;
                Logger.Instance.Write("account list refreshed.");
            }
            catch (Exception ex)
            {
                ActivationManager.Instance.CurrentStatus = ActivationStatus.Error;
                Logger.Instance.Write("error on acount list download.", ex);
            }
        }

        private static readonly string AccountListUrl
            = "https://gist.githubusercontent.com/anoyetta/bc658cb51552ea12ce1aaa2899004e8c/raw/accounts.json";
    }

    internal enum ActivationStatus
    {
        Loading,
        Error,
        Allow,
        Deny,
    }
}
