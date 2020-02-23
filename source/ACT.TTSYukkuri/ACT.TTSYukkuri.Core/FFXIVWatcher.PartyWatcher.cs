using System;
using System.Collections.Generic;
using System.Linq;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Globalization;
using FFXIV.Framework.XIVHelper;

namespace ACT.TTSYukkuri
{
    /// <summary>
    /// FF14を監視する パーティメンバ監視部分
    /// </summary>
    public partial class FFXIVWatcher
    {
        /// <summary>
        /// 通知する感覚(sec)
        /// </summary>
        private const double NoticeInterval = 6.0;

        /// <summary>
        /// 最後のHP通知日時
        /// </summary>
        private DateTime lastHPNotice = DateTime.MinValue;

        /// <summary>
        /// 最後のMP通知日時
        /// </summary>
        private DateTime lastMPNotice = DateTime.MinValue;

        /// <summary>
        /// 最後のTP通知日時
        /// </summary>
        private DateTime lastTPNotice = DateTime.MinValue;

        /// <summary>
        /// 最後のGP通知日時
        /// </summary>
        private DateTime lastGPNotice = DateTime.MinValue;

        /// <summary>
        /// 直前のパーティメンバ情報
        /// </summary>
        private volatile List<PreviousPartyMemberStatus> previouseParyMemberList = new List<PreviousPartyMemberStatus>();

        /// <summary>
        /// パーティを監視する
        /// </summary>
        public void WatchParty()
        {
            // パーティメンバの情報を取得する
            var player = CombatantsManager.Instance.Player;
            var partyList = CombatantsManager.Instance.GetPartyList();

            // パーティリストに存在しないメンバの前回の状態を消去する
            this.previouseParyMemberList.RemoveAll(x =>
                !partyList.Any(y => y.ID == x.ID));

            foreach (var partyMember in partyList)
            {
                // このPTメンバの現在の状態を取得する
                var hp = partyMember.CurrentHP;
                var hpp =
                    partyMember.MaxHP != 0 ?
                    ((decimal)partyMember.CurrentHP / (decimal)partyMember.MaxHP) * 100m :
                    0m;

                var mp = partyMember.CurrentMP;
                var mpp =
                    partyMember.MaxMP != 0 ?
                    ((decimal)partyMember.CurrentMP / (decimal)partyMember.MaxMP) * 100m :
                    0m;

                var tp = partyMember.CurrentTP;
                var tpp = ((decimal)partyMember.CurrentTP / 1000m) * 100m;

                var gp = partyMember.CurrentGP;
                var gpp =
                    partyMember.MaxGP != 0 ?
                    ((decimal)partyMember.CurrentGP / (decimal)partyMember.MaxGP) * 100m :
                    0m;

                // このPTメンバの直前の情報を取得する
                var previousePartyMember = (
                    from x in this.previouseParyMemberList
                    where
                    x.ID == partyMember.ID
                    select
                    x).FirstOrDefault();

                if (previousePartyMember == null)
                {
                    previousePartyMember = new PreviousPartyMemberStatus()
                    {
                        ID = partyMember.ID,
                        Name = partyMember.Name,
                        HPRate = hpp,
                        MPRate = mpp,
                        TPRate = tpp,
                        GPRate = gpp,
                    };

                    this.previouseParyMemberList.Add(previousePartyMember);
                }

                // 読上げ用の名前「ジョブ名＋イニシャル」とする
                var pcname = string.Empty;
                var job = string.Empty;
                if (partyMember.IsPlayer)
                {
                    switch (Settings.Default.UILocale)
                    {
                        case Locales.EN:
                            pcname = "You";
                            break;

                        case Locales.JA:
                            pcname = "自分";
                            break;

                        case Locales.CN:
                            pcname = "你";
                            break;

                        default:
                            pcname = "You";
                            break;
                    }

                    job = pcname;
                }
                else
                {
                    if (Settings.Default.UILocale == Locales.CN)
                    {
                        pcname = $"{partyMember.Name.Trim()}";
                    }
                    else
                    {
                        pcname = $"{partyMember.JobID.GetPhonetic()} {partyMember.Name.Trim().Substring(0, 1)}";
                    }

                    job = partyMember.JobID.GetPhonetic();
                }

                // 読上げ用のテキストを編集する
                var hpTextToSpeak = Settings.Default.StatusAlertSettings.HPTextToSpeack;
                var mpTextToSpeak = Settings.Default.StatusAlertSettings.MPTextToSpeack;
                var tpTextToSpeak = Settings.Default.StatusAlertSettings.TPTextToSpeack;
                var gpTextToSpeak = Settings.Default.StatusAlertSettings.GPTextToSpeack;

                hpTextToSpeak = replaceTTS(hpTextToSpeak);
                mpTextToSpeak = replaceTTS(mpTextToSpeak);
                tpTextToSpeak = replaceTTS(tpTextToSpeak);
                gpTextToSpeak = replaceTTS(gpTextToSpeak);

                var isUsingJob = false;

                string replaceTTS(string tts)
                {
                    if (tts.Contains("<job>"))
                    {
                        isUsingJob = true;
                    }

                    tts = tts.Replace("<pcname>", pcname);
                    tts = tts.Replace("<job>", job);
                    tts = tts.Replace("<hp>", hp.ToString());
                    tts = tts.Replace("<hpp>", decimal.ToInt32(hpp).ToString());
                    tts = tts.Replace("<mp>", mp.ToString());
                    tts = tts.Replace("<mpp>", decimal.ToInt32(mpp).ToString());
                    tts = tts.Replace("<tp>", tp.ToString());
                    tts = tts.Replace("<tpp>", decimal.ToInt32(tpp).ToString());
                    tts = tts.Replace("<gp>", gp.ToString());
                    tts = tts.Replace("<gpp>", decimal.ToInt32(gpp).ToString());
                    return tts;
                }

                var deadman = isUsingJob ? job : pcname;

                // 設定へのショートカット
                var config = Settings.Default.StatusAlertSettings;

                // HPをチェックして読上げる
                if (config.EnabledHPAlert &&
                    !string.IsNullOrWhiteSpace(hpTextToSpeak))
                {
                    if (this.IsWatchTarget(partyMember, player, "HP"))
                    {
                        if (hpp <= (decimal)config.HPThreshold &&
                            previousePartyMember.HPRate > (decimal)config.HPThreshold)
                        {
                            if ((DateTime.Now - this.lastHPNotice).TotalSeconds >= NoticeInterval)
                            {
                                this.Speak(hpTextToSpeak, config.NoticeDeviceForHP, config.NoticeVoicePaletteForHP);
                                this.lastHPNotice = DateTime.Now;
                            }
                        }
                        else
                        {
                            if (hpp <= decimal.Zero && previousePartyMember.HPRate != decimal.Zero)
                            {
                                this.SpeakEmpty("HP", deadman, config.NoticeDeviceForHP, config.NoticeVoicePaletteForHP);
                                this.lastHPNotice = DateTime.Now;
                            }
                        }
                    }
                }

                // MPをチェックして読上げる
                if (hp > 0)
                {
                    if (config.EnabledMPAlert &&
                        !string.IsNullOrWhiteSpace(mpTextToSpeak))
                    {
                        if (this.IsWatchTarget(partyMember, player, "MP"))
                        {
                            if (mpp <= (decimal)config.MPThreshold &&
                                previousePartyMember.MPRate > (decimal)config.MPThreshold)
                            {
                                if ((DateTime.Now - this.lastMPNotice).TotalSeconds >= NoticeInterval)
                                {
                                    this.Speak(mpTextToSpeak, config.NoticeDeviceForMP, config.NoticeVoicePaletteForMP);
                                    this.lastMPNotice = DateTime.Now;
                                }
                            }
                            else
                            {
                                if (mpp <= decimal.Zero && previousePartyMember.MPRate != decimal.Zero)
                                {
                                    this.SpeakEmpty("MP", deadman, config.NoticeDeviceForMP, config.NoticeVoicePaletteForMP);
                                    this.lastMPNotice = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                // TPをチェックして読上げる
                if (hp > 0)
                {
                    if (config.EnabledTPAlert &&
                        !string.IsNullOrWhiteSpace(tpTextToSpeak))
                    {
                        if (this.IsWatchTarget(partyMember, player, "TP"))
                        {
                            if (tpp <= (decimal)config.TPThreshold &&
                                previousePartyMember.TPRate > (decimal)config.TPThreshold)
                            {
                                if ((DateTime.Now - this.lastTPNotice).TotalSeconds >= NoticeInterval)
                                {
                                    this.Speak(tpTextToSpeak, config.NoticeDeviceForTP, config.NoticeVoicePaletteForTP);
                                    this.lastTPNotice = DateTime.Now;
                                }
                            }
                            else
                            {
                                if (tpp <= decimal.Zero && previousePartyMember.TPRate != decimal.Zero)
                                {
                                    this.SpeakEmpty("TP", deadman, config.NoticeDeviceForTP, config.NoticeVoicePaletteForTP);
                                    this.lastTPNotice = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                // GPをチェックして読上げる
                if (hp > 0 &&
                    partyMember.MaxGP > 0)
                {
                    if (config.EnabledGPAlert &&
                        !string.IsNullOrWhiteSpace(gpTextToSpeak))
                    {
                        if (this.IsWatchTarget(partyMember, player, "GP"))
                        {
                            if (gpp >= (decimal)config.GPThreshold &&
                                previousePartyMember.GPRate < (decimal)config.GPThreshold)
                            {
                                if ((DateTime.Now - this.lastGPNotice).TotalSeconds >= NoticeInterval)
                                {
                                    this.Speak(gpTextToSpeak, config.NoticeDeviceForGP, config.NoticeVoicePaletteForGP);
                                    this.lastGPNotice = DateTime.Now;
                                }
                            }
                            else
                            {
                                if (gpp >= 100m && previousePartyMember.GPRate < 100m)
                                {
                                    this.SpeakEmpty("GP", deadman, config.NoticeDeviceForGP, config.NoticeVoicePaletteForGP);
                                    this.lastGPNotice = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                // 今回の状態を保存する
                previousePartyMember.HPRate = hpp;
                previousePartyMember.MPRate = mpp;
                previousePartyMember.TPRate = tpp;
                previousePartyMember.GPRate = gpp;
            }
        }

        /// <summary>
        /// 空になった旨を発言する
        /// </summary>
        /// <param name="targetStatus">
        /// 対象のステータス HP/MP/TP</param>
        /// <param name="pcName">
        /// PC名</param>
        private void SpeakEmpty(
            string targetStatus,
            string pcName,
            PlayDevices device = PlayDevices.Both,
            VoicePalettes voicePalette = VoicePalettes.Default)
        {
            var emptyJa = $"{pcName},{targetStatus}なし。";
            var emptyEn = $"{pcName}, {targetStatus} empty.";
            var diedJa = $"{pcName},戦闘不能。";
            var diedEn = $"{pcName}, dead.";
            var fullJa = $"{pcName},{targetStatus}満タン。";
            var fullEn = $"{pcName},{targetStatus} full.";

            var empty = string.Empty;
            var died = string.Empty;
            var full = string.Empty;
            switch (Settings.Default.UILocale)
            {
                case Locales.EN:
                    empty = emptyEn;
                    died = diedEn;
                    full = fullJa;
                    break;

                case Locales.JA:
                    empty = emptyJa;
                    died = diedJa;
                    full = fullEn;
                    break;
            }

            var tts = string.Empty;
            switch (targetStatus.ToUpper())
            {
                case "HP":
                    tts = died;
                    break;

                case "GP":
                    tts = full;
                    break;

                default:
                    tts = empty;
                    break;
            }

            this.Speak(tts, device, voicePalette);
        }

        /// <summary>
        /// 監視対象か？
        /// </summary>
        /// <param name="targetInfo">監視候補の情報</param>
        /// <param name="playerInfo">プレイヤの情報</param>
        /// <param name="targetParameter">対象とするParameter</param>
        /// <returns>監視対象か？</returns>
        private bool IsWatchTarget(
            CombatantEx targetInfo,
            CombatantEx playerInfo,
            string targetParameter)
        {
            var r = false;

            var watchTarget = default(IList<AlertTarget>);
            switch (targetParameter.ToUpper())
            {
                case "HP":
                    watchTarget = Settings.Default.StatusAlertSettings.AlertTargetsHP;
                    break;

                case "MP":
                    watchTarget = Settings.Default.StatusAlertSettings.AlertTargetsMP;
                    break;

                case "TP":
                    watchTarget = Settings.Default.StatusAlertSettings.AlertTargetsTP;
                    break;

                case "GP":
                    watchTarget = Settings.Default.StatusAlertSettings.AlertTargetsGP;
                    break;

                default:
                    return r;
            }

            if (targetInfo.JobID != JobIDs.Unknown)
            {
                var alertCategoryNo = (int)targetInfo.JobID.GetAlertCategory();
                if (alertCategoryNo < watchTarget.Count)
                {
                    r = watchTarget[alertCategoryNo].Enabled;
                }
            }

            // 自分自身か？
            if (targetInfo.ID == playerInfo.ID)
            {
                r = watchTarget[(int)AlertCategories.Me].Enabled;
            }

            return r;
        }

        /// <summary>
        /// 直前のPTメンバステータス
        /// </summary>
        private class PreviousPartyMemberStatus
        {
            /// <summary>
            /// ID
            /// </summary>
            public uint ID { get; set; }

            /// <summary>
            /// 名前
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// HP率
            /// </summary>
            public decimal HPRate { get; set; }

            /// <summary>
            /// MP率
            /// </summary>
            public decimal MPRate { get; set; }

            /// <summary>
            /// TP率
            /// </summary>
            public decimal TPRate { get; set; }

            /// <summary>
            /// GP率
            /// </summary>
            public decimal GPRate { get; set; }
        }
    }
}
