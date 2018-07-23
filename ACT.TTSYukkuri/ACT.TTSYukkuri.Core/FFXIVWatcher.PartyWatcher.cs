using System;
using System.Collections.Generic;
using System.Linq;
using ACT.TTSYukkuri.Config;
using FFXIV.Framework.FFXIVHelper;
using FFXIV.Framework.Globalization;

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
        /// 最後のMP通知日時
        /// </summary>
        private DateTime lastTPNotice = DateTime.MinValue;

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
            var player = FFXIVPlugin.Instance.GetPlayer();
            var partyList = FFXIVPlugin.Instance.GetPartyList();

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
                        TPRate = tpp
                    };

                    this.previouseParyMemberList.Add(previousePartyMember);
                }

                // 読上げ用の名前「ジョブ名＋イニシャル」とする
                var pcname = string.Empty;
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
                    }
                }
                else
                {
                    pcname = $"{partyMember.JobID.GetPhonetic()} { partyMember.Name.Trim().Substring(0, 1)}";
                }

                // 読上げ用のテキストを編集する
                var hpTextToSpeak = Settings.Default.StatusAlertSettings.HPTextToSpeack;
                var mpTextToSpeak = Settings.Default.StatusAlertSettings.MPTextToSpeack;
                var tpTextToSpeak = Settings.Default.StatusAlertSettings.TPTextToSpeack;

                hpTextToSpeak = hpTextToSpeak.Replace("<pcname>", pcname);
                hpTextToSpeak = hpTextToSpeak.Replace("<hp>", hp.ToString());
                hpTextToSpeak = hpTextToSpeak.Replace("<hpp>", decimal.ToInt32(hpp).ToString());
                hpTextToSpeak = hpTextToSpeak.Replace("<mp>", mp.ToString());
                hpTextToSpeak = hpTextToSpeak.Replace("<mpp>", decimal.ToInt32(mpp).ToString());
                hpTextToSpeak = hpTextToSpeak.Replace("<tp>", tp.ToString());
                hpTextToSpeak = hpTextToSpeak.Replace("<tpp>", decimal.ToInt32(tpp).ToString());

                mpTextToSpeak = mpTextToSpeak.Replace("<pcname>", pcname);
                mpTextToSpeak = mpTextToSpeak.Replace("<hp>", hp.ToString());
                mpTextToSpeak = mpTextToSpeak.Replace("<hpp>", decimal.ToInt32(hpp).ToString());
                mpTextToSpeak = mpTextToSpeak.Replace("<mp>", mp.ToString());
                mpTextToSpeak = mpTextToSpeak.Replace("<mpp>", decimal.ToInt32(mpp).ToString());
                mpTextToSpeak = mpTextToSpeak.Replace("<tp>", tp.ToString());
                mpTextToSpeak = mpTextToSpeak.Replace("<tpp>", decimal.ToInt32(tpp).ToString());

                tpTextToSpeak = tpTextToSpeak.Replace("<pcname>", pcname);
                tpTextToSpeak = tpTextToSpeak.Replace("<hp>", hp.ToString());
                tpTextToSpeak = tpTextToSpeak.Replace("<hpp>", decimal.ToInt32(hpp).ToString());
                tpTextToSpeak = tpTextToSpeak.Replace("<mp>", mp.ToString());
                tpTextToSpeak = tpTextToSpeak.Replace("<mpp>", decimal.ToInt32(mpp).ToString());
                tpTextToSpeak = tpTextToSpeak.Replace("<tp>", tp.ToString());
                tpTextToSpeak = tpTextToSpeak.Replace("<tpp>", decimal.ToInt32(tpp).ToString());

                // HPをチェックして読上げる
                if (Settings.Default.StatusAlertSettings.EnabledHPAlert &&
                    !string.IsNullOrWhiteSpace(hpTextToSpeak))
                {
                    if (this.IsWatchTarget(partyMember, player, "HP"))
                    {
                        if (hpp <= (decimal)Settings.Default.StatusAlertSettings.HPThreshold &&
                            previousePartyMember.HPRate > (decimal)Settings.Default.StatusAlertSettings.HPThreshold)
                        {
                            if ((DateTime.Now - this.lastHPNotice).TotalSeconds >= NoticeInterval)
                            {
                                this.Speak(hpTextToSpeak);
                                this.lastHPNotice = DateTime.Now;
                            }
                        }
                        else
                        {
                            if (hpp <= decimal.Zero && previousePartyMember.HPRate != decimal.Zero)
                            {
                                this.SpeakEmpty("HP", pcname);
                                this.lastHPNotice = DateTime.Now;
                            }
                        }
                    }
                }

                // MPをチェックして読上げる
                if (hp > 0)
                {
                    if (Settings.Default.StatusAlertSettings.EnabledMPAlert &&
                        !string.IsNullOrWhiteSpace(mpTextToSpeak))
                    {
                        if (this.IsWatchTarget(partyMember, player, "MP"))
                        {
                            if (mpp <= (decimal)Settings.Default.StatusAlertSettings.MPThreshold &&
                                previousePartyMember.MPRate > (decimal)Settings.Default.StatusAlertSettings.MPThreshold)
                            {
                                if ((DateTime.Now - this.lastMPNotice).TotalSeconds >= NoticeInterval)
                                {
                                    this.Speak(mpTextToSpeak);
                                    this.lastMPNotice = DateTime.Now;
                                }
                            }
                            else
                            {
                                if (mpp <= decimal.Zero && previousePartyMember.MPRate != decimal.Zero)
                                {
                                    this.SpeakEmpty("MP", pcname);
                                    this.lastMPNotice = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                // TPをチェックして読上げる
                if (hp > 0)
                {
                    if (Settings.Default.StatusAlertSettings.EnabledTPAlert &&
                        !string.IsNullOrWhiteSpace(tpTextToSpeak))
                    {
                        if (this.IsWatchTarget(partyMember, player, "TP"))
                        {
                            if (tpp <= (decimal)Settings.Default.StatusAlertSettings.TPThreshold &&
                                previousePartyMember.TPRate > (decimal)Settings.Default.StatusAlertSettings.TPThreshold)
                            {
                                if ((DateTime.Now - this.lastTPNotice).TotalSeconds >= NoticeInterval)
                                {
                                    this.Speak(tpTextToSpeak);
                                    this.lastTPNotice = DateTime.Now;
                                }
                            }
                            else
                            {
                                if (tpp <= decimal.Zero && previousePartyMember.TPRate != decimal.Zero)
                                {
                                    this.SpeakEmpty("TP", pcname);
                                    this.lastTPNotice = DateTime.Now;
                                }
                            }
                        }
                    }
                }

                // 今回の状態を保存する
                previousePartyMember.HPRate = hpp;
                previousePartyMember.MPRate = mpp;
                previousePartyMember.TPRate = tpp;
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
            string pcName)
        {
            var emptyJa = $"{pcName},{targetStatus}なし。";
            var emptyEn = $"{pcName}, {targetStatus} empty.";
            var diedJa = $"{pcName},戦闘不能。";
            var diedEn = $"{pcName}, dead.";

            var empty = string.Empty;
            var died = string.Empty;
            switch (Settings.Default.UILocale)
            {
                case Locales.EN:
                    empty = emptyEn;
                    died = diedEn;
                    break;

                case Locales.JA:
                    empty = emptyJa;
                    died = diedJa;
                    break;
            }

            var tts = string.Empty;
            switch (targetStatus.ToUpper())
            {
                case "HP":
                    tts = died;
                    break;

                default:
                    tts = empty;
                    break;
            }

            this.Speak(tts);
        }

        /// <summary>
        /// 監視対象か？
        /// </summary>
        /// <param name="targetInfo">監視候補の情報</param>
        /// <param name="playerInfo">プレイヤの情報</param>
        /// <param name="targetParameter">対象とするParameter</param>
        /// <returns>監視対象か？</returns>
        private bool IsWatchTarget(
            Combatant targetInfo,
            Combatant playerInfo,
            string targetParameter)
        {
            var r = false;

            var watchTarget = default(IList<AlertTarget>);
            switch (targetParameter.ToUpper())
            {
                case "HP":
                    watchTarget = (IList<AlertTarget>)Settings.Default.StatusAlertSettings.AlertTargetsHP;
                    break;

                case "MP":
                    watchTarget = (IList<AlertTarget>)Settings.Default.StatusAlertSettings.AlertTargetsMP;
                    break;

                case "TP":
                    watchTarget = (IList<AlertTarget>)Settings.Default.StatusAlertSettings.AlertTargetsTP;
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
            /// HP率
            /// </summary>
            public decimal HPRate { get; set; }

            /// <summary>
            /// ID
            /// </summary>
            public uint ID { get; set; }

            /// <summary>
            /// MP率
            /// </summary>
            public decimal MPRate { get; set; }

            /// <summary>
            /// 名前
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// TP率
            /// </summary>
            public decimal TPRate { get; set; }
        }
    }
}
