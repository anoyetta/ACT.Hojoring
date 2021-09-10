using ACT.TTSYukkuri.SAPI5;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.WPF.Views;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace ACT.TTSYukkuri.Config.ViewModels
{
    public class PollyConfigViewModel : BindableBase
    {
        private VoicePalettes VoicePalette { get; set; }

        public PollyConfigViewModel(VoicePalettes voicePalette = VoicePalettes.Default)
        {
            this.VoicePalette = voicePalette;
        }

        public PollyConfigs Config
        {
            get
            {
                PollyConfigs config;
                switch (VoicePalette)
                {
                    case VoicePalettes.Default:
                        config = Settings.Default.PollySettings;
                        break;

                    case VoicePalettes.Ext1:
                        config = Settings.Default.PollySettingsExt1;
                        break;

                    case VoicePalettes.Ext2:
                        config = Settings.Default.PollySettingsExt2;
                        break;

                    case VoicePalettes.Ext3:
                        config = Settings.Default.PollySettingsExt3;
                        break;

                    default:
                        config = Settings.Default.PollySettings;
                        break;
                }
                return config;
            }
        }

        public IEnumerable<dynamic> Regions =>
            RegionEndpoint.EnumerableAllRegions
            .Select(x => new
            {
                Name = x.DisplayName,
                Value = x.SystemName
            });

        public ObservableCollection<PollyConfigs.PollyVoice> Voices => Settings.Default.PollyVoices;

        public IEnumerable<dynamic> Volumes =>
            Enum.GetValues(typeof(Volumes))
            .Cast<Volumes>()
            .Select(x => new
            {
                Name = x.ToXML(),
                Value = x,
            });

        public IEnumerable<dynamic> Rates =>
            Enum.GetValues(typeof(Rates))
            .Cast<Rates>()
            .Select(x => new
            {
                Name = x.ToXML(),
                Value = x,
            });

        public IEnumerable<dynamic> Pitches =>
            Enum.GetValues(typeof(Pitches))
            .Cast<Pitches>()
            .Select(x => new
            {
                Name = x.ToXML(),
                Value = x,
            });

        private DelegateCommand _getVoicesCommand;

        public DelegateCommand GetVoicesCommand =>
            this._getVoicesCommand ?? (this._getVoicesCommand = new DelegateCommand(this.ExecuteGetVoicesCommand));

        private async void ExecuteGetVoicesCommand()
        {
            if (string.IsNullOrEmpty(this.Config.AccessKey) ||
                string.IsNullOrEmpty(this.Config.SecretKey))
            {
                ModernMessageBox.ShowDialog(
                    "Enter your access key and secret key.",
                    "ACT.Hojoring");

                return;
            }

            var endpoint = this.Config.Endpoint;
            var chain = new CredentialProfileStoreChain();

            var hash = (this.Config.Region + this.Config.AccessKey + this.Config.SecretKey).GetHashCode().ToString("X4");
            var profileName = $"polly_profile_{hash}";

            AWSCredentials awsCredentials;
            if (!chain.TryGetAWSCredentials(
                profileName,
                out awsCredentials))
            {
                var options = new CredentialProfileOptions
                {
                    AccessKey = this.Config.AccessKey,
                    SecretKey = this.Config.SecretKey,
                };

                var profile = new CredentialProfile(profileName, options);
                profile.Region = endpoint;

                chain.RegisterProfile(profile);

                chain.TryGetAWSCredentials(
                    profileName,
                    out awsCredentials);
            }

            if (awsCredentials == null)
            {
                return;
            }

            var voice = this.Config.Voice;

            using (var pc = new AmazonPollyClient(
                awsCredentials,
                endpoint))
            {
                var res = await pc.DescribeVoicesAsync(new DescribeVoicesRequest());

                if (res == null ||
                    res.HttpStatusCode != HttpStatusCode.OK)
                {
                    ModernMessageBox.ShowDialog(
                        "Voices update is failed.",
                        "ACT.Hojoring");

                    return;
                }

                this.Voices.Clear();
                this.Config.Voice = string.Empty;

                foreach (var v in
                    from x in res.Voices
                    orderby
                    x.LanguageCode.ToString(),
                    x.Gender.ToString(),
                    x.Name
                    select
                    x)
                {
                    this.Voices.Add(
                        new PollyConfigs.PollyVoice { Name = $"{v.Id.Value} ({v.LanguageCode}, {v.Gender})", Value = v.Id });
                }
            }

            if (this.Voices.Any(x => x.Value == voice))
            {
                this.Config.Voice = voice;
            }

            Settings.Default.Save();

            ModernMessageBox.ShowDialog(
                "Voices update is completed.",
                "ACT.Hojoring");
        }
    }
}
