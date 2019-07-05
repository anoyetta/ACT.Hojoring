using System.Linq;
using System.Net;
using Octokit;

namespace ACT.Hojoring
{
    public class UpdateChecker
    {
        public UpdateChecker()
        {
            CosturaUtility.Initialize();
        }

        public bool UsePreRelease { get; set; }

        public ReleaseInfo GetNewerVersion()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            return this.GetNewerVersionCore(this.UsePreRelease);
        }

        private ReleaseInfo GetNewerVersionCore(
            bool usePreRelease = false)
        {
            var client = new GitHubClient(new ProductHeaderValue("ACT.Hojoring.Updater"));

            var releases = client.Repository.Release.GetAll("anoyetta", "ACT.Hojoring").Result;

            var lastest = releases.FirstOrDefault();
            if (!usePreRelease)
            {
                if (lastest.Prerelease)
                {
                    lastest = releases.FirstOrDefault(x => !x.Prerelease);
                }
            }

            if (lastest == null)
            {
                return null;
            }

            var asset = lastest.Assets.FirstOrDefault(x => x.Name.Contains("ACT.Hojoring-v"));
            if (asset == null)
            {
                return null;
            }

            return new ReleaseInfo()
            {
                Version = lastest.Name,
                Tag = lastest.TagName,
                ReleasePageUrl = lastest.HtmlUrl,
                Note = lastest.Body,
                AssetName = asset.Name,
                AssetUrl = asset.BrowserDownloadUrl
            };
        }
    }

    public class ReleaseInfo
    {
        public string Version { get; set; }

        public string Tag { get; set; }

        public string ReleasePageUrl { get; set; }

        public string Note { get; set; }

        public string AssetName { get; set; }

        public string AssetUrl { get; set; }
    }
}
