using System;
using System.Collections.Generic;
using System.Linq;
using ACT.TTSYukkuri.SAPI5;
using Amazon;
using Amazon.Polly;
using Prism.Mvvm;
using FFXIV.Framework.Bridge;

namespace ACT.TTSYukkuri.Config.ViewModels
{

    public class PollyConfigViewModel : BindableBase
    {
        VoicePalettes VoicePalette { get; set; }

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

        public IEnumerable<dynamic> Voices => new[]
        {
            new { Name = $"{VoiceId.Ivy.Value} (en-US, Female)", Value = VoiceId.Ivy.Value },
            new { Name = $"{VoiceId.Joanna.Value} (en-US, Female)", Value = VoiceId.Joanna.Value },
            new { Name = $"{VoiceId.Joey.Value} (en-US, Male)", Value = VoiceId.Joey.Value },
            new { Name = $"{VoiceId.Justin.Value} (en-US, Male)", Value = VoiceId.Justin.Value },
            new { Name = $"{VoiceId.Kendra.Value} (en-US, Female)", Value = VoiceId.Kendra.Value },
            new { Name = $"{VoiceId.Kimberly.Value} (en-US, Female)", Value = VoiceId.Kimberly.Value },
            new { Name = $"{VoiceId.Matthew.Value} (en-US, Male)", Value = VoiceId.Matthew.Value },
            new { Name = $"{VoiceId.Salli.Value} (en-US, Female)", Value = VoiceId.Salli.Value },

            new { Name = $"{VoiceId.Celine.Value} (fr-FR, Female)", Value = VoiceId.Celine.Value },
            new { Name = $"{VoiceId.Mathieu.Value} (fr-FR, Male)", Value = VoiceId.Mathieu.Value },

            new { Name = $"{VoiceId.Hans.Value} (de-DE, Male)", Value = VoiceId.Hans.Value },
            new { Name = $"{VoiceId.Marlene.Value} (de-DE, Female)", Value = VoiceId.Marlene.Value },
            new { Name = $"{VoiceId.Vicki.Value} (de-DE, Female)", Value = VoiceId.Vicki.Value },

            new { Name = $"{VoiceId.Mizuki.Value} (ja-JP, Female)", Value = VoiceId.Mizuki.Value },
            new { Name = $"{VoiceId.Takumi.Value} (ja-JP, Male)", Value = VoiceId.Takumi.Value },

            new { Name = $"{VoiceId.Seoyeon.Value} (ko-KR, Female)", Value = VoiceId.Seoyeon.Value },
        };

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
    }
}
