using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ACT.TTSYukkuri.Config;

namespace ACT.TTSYukkuri.Yukkuri
{
    public enum AQBaseVoices : int
    {
        F1E = 0,
        F2E = 1,
        M1E = 2,
    }

    public enum AQPresets
    {
        Custom = 0,
        F1,
        F2,
        F3,
        M1,
        M2,
        R1,
        R2,
    }

    public class AQPreset
    {
        public string Display => this.Key.ToDisplay();
        public AQPresets Key { get; set; } = AQPresets.Custom;
        public AQTK_VOICE Parameter { get; set; } = new AQTK_VOICE();
    }

    public static class AQPresetsExtensions
    {
        public static AQTK_VOICE GetPreset(
            this AQPresets preset)
        {
            switch (preset)
            {
                case AQPresets.F1: return AQVoicePresets.F1;
                case AQPresets.F2: return AQVoicePresets.F2;
                case AQPresets.F3: return AQVoicePresets.F3;
                case AQPresets.M1: return AQVoicePresets.M1;
                case AQPresets.M2: return AQVoicePresets.M2;
                case AQPresets.R1: return AQVoicePresets.R1;
                case AQPresets.R2: return AQVoicePresets.R2;

                case AQPresets.Custom:
                default:
                    return new AQTK_VOICE();
            }
        }

        public static string ToDisplay(
            this AQPresets preset)
        {
            switch (preset)
            {
                case AQPresets.F1: return "Female F1 (YUKKURI)";
                case AQPresets.F2: return "Female F2";
                case AQPresets.F3: return "Female F3";
                case AQPresets.M1: return "Male M1";
                case AQPresets.M2: return "Male M2";
                case AQPresets.R1: return "Robot R1";
                case AQPresets.R2: return "Robot R2";

                case AQPresets.Custom:
                default:
                    return "Custom";
            }
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AQTK_VOICE
    {
        /// <summary>
        /// 基本素片
        /// </summary>
        /// <remarks>
        /// F1E/F2E/M1E (0/1/2)</remarks>
        [MarshalAs(UnmanagedType.I4)]
        public int bas;

        /// <summary>
        /// 話速
        /// </summary>
        /// <remarks>
        /// 50-300 default:100</remarks>
        [MarshalAs(UnmanagedType.I4)]
        public int spd;

        /// <summary>
        /// 音量
        /// </summary>
        /// <remarks>
        /// 0-300</remarks>
        [MarshalAs(UnmanagedType.I4)]
        public int vol;

        /// <summary>
        /// 高さ
        /// </summary>
        /// <remarks>
        /// 20-200</remarks>
        [MarshalAs(UnmanagedType.I4)]
        public int pit;

        /// <summary>
        /// アクセント
        /// </summary>
        /// <remarks>
        /// 0-200</remarks>
        [MarshalAs(UnmanagedType.I4)]
        public int acc;

        /// <summary>
        /// 音程1
        /// </summary>
        /// <remarks>
        /// 0-200 default:100</remarks>
        [MarshalAs(UnmanagedType.I4)]
        public int lmd;

        /// <summary>
        /// 音程2
        /// </summary>
        /// <remarks>
        /// 50-200 default:100</remarks>
        [MarshalAs(UnmanagedType.I4)]
        public int fsc;

        public YukkuriConfig ToConfig() => new YukkuriConfig()
        {
            BaseVoice = (AQBaseVoices)this.bas,
            Speed = this.spd,
            Volume = this.vol,
            Pitch = this.pit,
            Accent = this.acc,
            LMD = this.lmd,
            FSC = this.fsc,
        };
    }

    public static class AQVoicePresets
    {
        private static IReadOnlyList<AQPreset> presets;

        public static IReadOnlyList<AQPreset> Presets =>
            AQVoicePresets.presets ?? (AQVoicePresets.presets = new AQPreset[]
            {
                new AQPreset { Key = AQPresets.F1, Parameter = AQVoicePresets.F1 },
                new AQPreset { Key = AQPresets.F2, Parameter = AQVoicePresets.F2 },
                new AQPreset { Key = AQPresets.F3, Parameter = AQVoicePresets.F3 },
                new AQPreset { Key = AQPresets.M1, Parameter = AQVoicePresets.M1 },
                new AQPreset { Key = AQPresets.M2, Parameter = AQVoicePresets.M2 },
                new AQPreset { Key = AQPresets.R1, Parameter = AQVoicePresets.R1 },
                new AQPreset { Key = AQPresets.R2, Parameter = AQVoicePresets.R2 },
                new AQPreset { Key = AQPresets.Custom },
            });

        /// <summary>
        /// 女性 F1 (ゆっくり)
        /// </summary>
        public static readonly AQTK_VOICE F1 = new AQTK_VOICE()
        {
            bas = (int)AQBaseVoices.F1E,
            spd = 100,
            vol = 100,
            pit = 100,
            acc = 100,
            lmd = 100,
            fsc = 100,
        };

        /// <summary>
        /// 女性 F2
        /// </summary>
        public static readonly AQTK_VOICE F2 = new AQTK_VOICE()
        {
            bas = (int)AQBaseVoices.F2E,
            spd = 100,
            vol = 100,
            pit = 77,
            acc = 150,
            lmd = 100,
            fsc = 100,
        };

        /// <summary>
        /// 女性 F3
        /// </summary>
        public static readonly AQTK_VOICE F3 = new AQTK_VOICE()
        {
            bas = (int)AQBaseVoices.F1E,
            spd = 80,
            vol = 100,
            pit = 100,
            acc = 100,
            lmd = 61,
            fsc = 148,
        };

        /// <summary>
        /// 男性 M1
        /// </summary>
        public static readonly AQTK_VOICE M1 = new AQTK_VOICE()
        {
            bas = (int)AQBaseVoices.M1E,
            spd = 100,
            vol = 100,
            pit = 30,
            acc = 100,
            lmd = 100,
            fsc = 100,
        };

        /// <summary>
        /// 男性 M2
        /// </summary>
        public static readonly AQTK_VOICE M2 = new AQTK_VOICE()
        {
            bas = (int)AQBaseVoices.M1E,
            spd = 105,
            vol = 100,
            pit = 45,
            acc = 130,
            lmd = 120,
            fsc = 100,
        };

        /// <summary>
        /// ロボット R1
        /// </summary>
        public static readonly AQTK_VOICE R1 = new AQTK_VOICE()
        {
            bas = (int)AQBaseVoices.M1E,
            spd = 100,
            vol = 100,
            pit = 30,
            acc = 20,
            lmd = 190,
            fsc = 100,
        };

        /// <summary>
        /// ロボット R2
        /// </summary>
        public static readonly AQTK_VOICE R2 = new AQTK_VOICE()
        {
            bas = (int)AQBaseVoices.F2E,
            spd = 70,
            vol = 100,
            pit = 50,
            acc = 50,
            lmd = 50,
            fsc = 180,
        };
    }
}
