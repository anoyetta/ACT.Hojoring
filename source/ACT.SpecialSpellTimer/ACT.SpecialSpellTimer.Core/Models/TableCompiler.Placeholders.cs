using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACT.SpecialSpellTimer.Config;
using ACT.SpecialSpellTimer.RaidTimeline;
using ACT.SpecialSpellTimer.Utility;
using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer.Models
{
    public partial class TableCompiler
    {
        #region プレースホルダに関するメソッド群
         
        private readonly PlaceholderContainer[] IDPlaceholders = new[]
        {
            new PlaceholderContainer("<id>", "[0-9a-fA-F]+", PlaceholderTypes.Custom),
            new PlaceholderContainer("<id4>", "[0-9a-fA-F]{4}", PlaceholderTypes.Custom),
            new PlaceholderContainer("<id8>", "[0-9a-fA-F]{8}", PlaceholderTypes.Custom),
            new PlaceholderContainer("<_duration>", @"(?<_duration>[\d\.]+)", PlaceholderTypes.Custom),
            new PlaceholderContainer("<job>", $"(?<_job>{string.Join("|", GetJobNames())})" , PlaceholderTypes.Custom),
            new PlaceholderContainer("<num>", $@"(?<_num>[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)" , PlaceholderTypes.Custom),
            new PlaceholderContainer("<posx>", $@"(?<_posx>[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)" , PlaceholderTypes.Custom),
            new PlaceholderContainer("<posy>", $@"(?<_posy>[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)" , PlaceholderTypes.Custom),
            new PlaceholderContainer("<posz>", $@"(?<_posz>[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)" , PlaceholderTypes.Custom),
        };

        private static string[] GetJobNames()
        {
            var list = new List<string>();

            var jobs = ((JobIDs[])Enum.GetValues(typeof(JobIDs)))
                .Where(x => x != JobIDs.Unknown)
                .Select(x => x.ToString());

            list.AddRange(jobs
                .Select(x => x.ToString().Substring(0, 1).ToUpper() + x.Substring(1).ToLower()));
            list.AddRange(jobs);

            return list.ToArray();
        }

        private volatile List<PlaceholderContainer> placeholderList =
            new List<PlaceholderContainer>();

        public IReadOnlyList<PlaceholderContainer> PlaceholderList =>
            this.GetPlaceholders(
                this.InSimulation || TimelineManager.Instance.InSimulation,
                false)
            as List<PlaceholderContainer>;

        private object PlaceholderListSyncRoot =>
            ((ICollection)this.placeholderList)?.SyncRoot;

        public IEnumerable<PlaceholderContainer> GetPlaceholders(
            bool inSimulation = false,
            bool forTimeline = false)
        {
            var placeholders = default(IEnumerable<PlaceholderContainer>);
            lock (this.PlaceholderListSyncRoot)
            {
                placeholders = new List<PlaceholderContainer>(this.placeholderList);
            }

            // Simulationモードでなければ抜ける
            if (!inSimulation)
            {
                if (forTimeline)
                {
                    placeholders = placeholders.Select(x =>
                        new PlaceholderContainer(
                            x.Placeholder
                                .Replace("<", "[")
                                .Replace(">", "]"),
                            x.ReplaceString,
                            x.Type));
                }

                return placeholders;
            }

            // プレースホルダ生成用のメソッド
            string createPH(string name) => !forTimeline ? $"<{name}>" : $"[{name}]";

            // シミュレータ向けプレースホルダのリストを生成する
            var placeholdersInSim = new List<PlaceholderContainer>(placeholders);
#if DEBUG
            placeholdersInSim.Clear();
#endif
            // 汎用プレースホルダ
            var idsInSim = new[]
            {
                new PlaceholderContainer(createPH("id"), @"([0-9a-fA-F]+|<id>|\[id\]|<id4>|\[id4\]|<id8>|\[id8\])", PlaceholderTypes.Custom),
                new PlaceholderContainer(createPH("id4"), @"([0-9a-fA-F]{4}|<id4>|\[id4\])", PlaceholderTypes.Custom),
                new PlaceholderContainer(createPH("id8"), @"([0-9a-fA-F]{8}|<id8>|\[id8\])", PlaceholderTypes.Custom),
                new PlaceholderContainer(createPH("_duration"), @"(?<_duration>[\d\.]+)", PlaceholderTypes.Custom),
                new PlaceholderContainer(createPH("job"), $"(?<_job>{string.Join("|", GetJobNames())})" , PlaceholderTypes.Custom)
            };

            var jobs = Enum.GetNames(typeof(JobIDs));
            var jobsPlacement = string.Join("|", jobs.Select(x => $@"\[{x}\]"));

            // JOB系プレースホルダ
            var jobsInSim = new List<PlaceholderContainer>();
            foreach (var job in jobs)
            {
                jobsInSim.Add(new PlaceholderContainer(createPH(job), $@"\[{job}\]", PlaceholderTypes.Party));
            }

            // PC系プレースホルダ
            var pcInSim = new[]
            {
                new PlaceholderContainer(createPH("mex"), @"(?<_mex>\[mex\])", PlaceholderTypes.Me),
                new PlaceholderContainer(createPH("nex"), $@"(?<_nex>{jobsPlacement}|\[nex\])", PlaceholderTypes.Party),
                new PlaceholderContainer(createPH("pc"), $@"(?<_pc>{jobsPlacement}|\[pc\]|\[mex\]|\[nex\])", PlaceholderTypes.Party),
            };

            // ID系を置き換える
            foreach (var ph in idsInSim)
            {
                var old = placeholdersInSim.FirstOrDefault(x => x.Placeholder == ph.Placeholder);
                if (old != null)
                {
                    old.ReplaceString = ph.ReplaceString;
                }
                else
                {
                    placeholdersInSim.Add(ph);
                }
            }

            // JOB系を追加する
            foreach (var ph in jobsInSim)
            {
                var old = placeholdersInSim.FirstOrDefault(x => x.Placeholder == ph.Placeholder);
                if (old != null)
                {
                    // NO-OP
                }
                else
                {
                    placeholdersInSim.Add(ph);
                }
            }

            // PC系を追加する
            foreach (var ph in pcInSim)
            {
                var old = placeholdersInSim.FirstOrDefault(x => x.Placeholder == ph.Placeholder);
                if (old != null)
                {
                    // NO-OP
                }
                else
                {
                    placeholdersInSim.Add(ph);
                }
            }

            return placeholdersInSim;
        }

        public void RefreshPartyPlaceholders()
        {
            if (string.IsNullOrEmpty(this.player.Name) ||
                this.player.ID == 0)
            {
                return;
            }

            // PC名辞書を更新する
            foreach (var pc in this.partyList)
            {
                PCNameDictionary.Instance.Add(pc.Name);
            }

            if (!Settings.Default.EnabledPartyMemberPlaceholder)
            {
                return;
            }

            var newList =
                new List<PlaceholderContainer>();

            // パーティメンバのいずれを示す <pc> を登録する
            var names = string.Join(
                "|",
                this.partyList.Select(x => x.NamesRegex).Concat(new[]
                {
                    @"\<pc\>",
                    @"\[pc\]",
                    @"\<mex\>",
                    @"\[mex\]",
                    @"\<nex\>",
                    @"\[nex\]",
                }));
            var oldValue = $"<pc>";
            var newValue = $"(?<_pc>{names})";
            newList.Add(new PlaceholderContainer(
                oldValue,
                newValue,
                PlaceholderTypes.Party));

            // FF14内部のPTメンバ自動ソート順で並び替える
            var partyListSorted =
                from x in this.SortedPartyList
                where
                x.ID != this.player.ID
                select
                x;

            // 自分以外のPTメンバを示す <nex> を登録する
            names = string.Join(
                "|",
                partyListSorted.Select(x => x.NamesRegex).Concat(new[]
                {
                    @"\<nex\>",
                    @"\[nex\]",
                }));
            oldValue = $"<nex>";
            newValue = $"(?<_nex>{names})";
            newList.Add(new PlaceholderContainer(
                oldValue,
                newValue,
                PlaceholderTypes.Party));

            // 通常のPTメンバ代名詞 <2>～<8> を登録する
            var index = 2;
            foreach (var combatant in partyListSorted)
            {
                newList.Add(new PlaceholderContainer(
                    $"<{index}>",
                    combatant.Name,
                    PlaceholderTypes.Party));

                newList.Add(new PlaceholderContainer(
                    $"<{index}ex>",
                    $"(?<_{index}ex>{combatant.NamesRegex})",
                    PlaceholderTypes.Party));

                index++;
            }

            // ジョブ名によるプレースホルダを登録する
            foreach (var job in Jobs.List)
            {
                // このジョブに該当するパーティメンバを抽出する
                var combatantsByJob = (
                    from x in this.partyList
                    where
                    x.Job == (int)job.ID
                    orderby
                    x.ID == this.player.ID ? 0 : 1,
                    x.ID descending
                    select
                    x).ToArray();

                if (!combatantsByJob.Any())
                {
                    continue;
                }

                // <JOBn>形式を登録する
                // ex. <PLD1> → Taro Paladin
                // ex. <PLD2> → Jiro Paladin
                for (int i = 0; i < combatantsByJob.Length; i++)
                {
                    newList.Add(new PlaceholderContainer(
                        $"<{job.ID.ToString().ToUpper()}{i + 1}>",
                        $"(?<_{job.ID.ToString().ToUpper()}{i + 1}>{ combatantsByJob[i].NamesRegex})",
                        PlaceholderTypes.Party));
                }

                // <JOB>形式を登録する ただし、この場合は正規表現のグループ形式とする
                // また、グループ名にはジョブの略称を設定する
                // ex. <PLD> → (?<_PLD>Taro Paladin|Jiro Paladin)
                names = string.Join(
                    "|",
                    combatantsByJob.Select(x => x.NamesRegex).Concat(new[]
                    {
                        $@"\<{job.ID.ToString().ToUpper()}\>",
                        $@"\[{job.ID.ToString().ToUpper()}\]",
                    }));
                oldValue = $"<{job.ID.ToString().ToUpper()}>";
                newValue = $"(?<_{job.ID.ToString().ToUpper()}>{names})";

                newList.Add(new PlaceholderContainer(
                    oldValue.ToUpper(),
                    newValue,
                    PlaceholderTypes.Party));
            }

            // ロールによるプレースホルダを登録する
            // ex. <TANK>   -> (?<_TANK>Taro Paladin|Jiro Paladin)
            // ex. <HEALER> -> (?<_HEALER>Taro Paladin|Jiro Paladin)
            // ex. <DPS>    -> (?<_DPS>Taro Paladin|Jiro Paladin)
            // ex. <MELEE>  -> (?<_MELEE>Taro Paladin|Jiro Paladin)
            // ex. <RANGE>  -> (?<_RANGE>Taro Paladin|Jiro Paladin)
            // ex. <MAGIC>  -> (?<_MAGIC>Taro Paladin|Jiro Paladin)
            var partyListByRole = CombatantsManager.Instance.GetPatryListByRole();
            foreach (var role in partyListByRole)
            {
                names = string.Join("|", role.Combatants.Select(x => x.NamesRegex).ToArray());
                oldValue = $"<{role.RoleLabel}>";
                newValue = $"(?<_{role.RoleLabel}>{names})";

                newList.Add(new PlaceholderContainer(
                    oldValue.ToUpper(),
                    newValue,
                    PlaceholderTypes.Party));
            }

            // <RoleN>形式のプレースホルダを登録する
            foreach (var role in partyListByRole)
            {
                for (int i = 0; i < role.Combatants.Count; i++)
                {
                    var label = $"{role.RoleLabel}{i + 1}";
                    var o = $"<{label}>";
                    var n = $"(?<_{label}>{role.Combatants[i].NamesRegex})";

                    newList.Add(new PlaceholderContainer(
                        o.ToUpper(),
                        n,
                        PlaceholderTypes.Party));
                }
            }

            // 新しく生成したプレースホルダを登録する
            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.RemoveAll(x => x.Type == PlaceholderTypes.Party);
                this.placeholderList.AddRange(newList);

                // ついでにID系プレースホルダを登録する
                var toAdds = IDPlaceholders.Where(x => !this.placeholderList.Any(y => y.Placeholder == x.Placeholder));
                this.placeholderList.AddRange(toAdds);
            }
        }

        public void RefreshPetPlaceholder()
        {
            if (!Settings.Default.EnabledPartyMemberPlaceholder)
            {
                return;
            }

            var playerJob = this.player.JobInfo;
            if (playerJob != null &&
                !playerJob.IsSummoner())
            {
                return;
            }

            void refreshPetID()
            {
                // 3秒毎に30秒間判定させる
                const int Interval = 3;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        var combatants = CombatantsManager.Instance.GetCombatants();
                        if (combatants == null)
                        {
                            continue;
                        }

                        var pet = (
                            from x in combatants
                            where
                            x.OwnerID == this.player.ID &&
                            (
                                x.Name.Contains("フェアリー・") ||
                                x.Name.Contains("・エギ") ||
                                x.Name.Contains("カーバンクル・")
                            )
                            select
                            x).FirstOrDefault();

                        if (pet != null)
                        {
                            lock (this.PlaceholderListSyncRoot)
                            {
                                this.placeholderList.RemoveAll(x => x.Type == PlaceholderTypes.Pet);
                                this.placeholderList.Add(new PlaceholderContainer(
                                    "<petid>",
                                    Convert.ToString((long)((ulong)pet.ID), 16).ToUpper(),
                                    PlaceholderTypes.Pet));
                            }

                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("refresh petid error:", ex);
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(Interval));
                }
            }

            Task.Run(() => refreshPetID())
                .ContinueWith((task) =>
                {
                    this.RecompileSpells();
                    this.RecompileTickers();
                });
        }

        public void RefreshPlayerPlacceholder()
        {
            if (string.IsNullOrEmpty(this.player.Name) ||
                this.player.ID == 0)
            {
                return;
            }

            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.RemoveAll(x => x.Type == PlaceholderTypes.Me);
                this.placeholderList.Add(new PlaceholderContainer(
                    "<me>",
                    this.player.Name,
                    PlaceholderTypes.Me));

                this.placeholderList.Add(new PlaceholderContainer(
                    "<mex>",
                    $@"(?<_mex>{this.player.NamesRegex}|\<mex\>|\[mex\])",
                    PlaceholderTypes.Me));
            }
        }

        #endregion プレースホルダに関するメソッド群

        #region カスタムプレースホルダに関するメソッド群

        /// <summary>
        /// カスタムプレースホルダーを削除する
        /// <param name="name">削除するプレースホルダーの名称</param>
        /// </summary>
        public void ClearCustomPlaceholder(string name)
        {
            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.RemoveAll(x =>
                    x.Placeholder == $"<{name}>" &&
                    x.Type == PlaceholderTypes.Custom);
            }

            this.RecompileSpells();
            this.RecompileTickers();
        }

        /// <summary>
        /// カスタムプレースホルダーを全て削除する
        /// </summary>
        public void ClearCustomPlaceholderAll()
        {
            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.RemoveAll(x =>
                    x.Type == PlaceholderTypes.Custom);
            }

            this.RecompileSpells();
            this.RecompileTickers();
        }

        /// <summary>
        /// カスタムプレースホルダーに追加する
        /// </summary>
        /// <param name="name">追加するプレースホルダーの名称</param>
        /// <paramname="value">置換する文字列</param>
        public void SetCustomPlaceholder(string name, string value)
        {
            lock (this.PlaceholderListSyncRoot)
            {
                this.placeholderList.Add(new PlaceholderContainer(
                    $"<{name}>",
                    value,
                    PlaceholderTypes.Custom));
            }

            this.RecompileSpells();
            this.RecompileTickers();
        }

        #endregion カスタムプレースホルダに関するメソッド群

        public class PlaceholderContainer
        {
            public PlaceholderContainer(
                string placeholder,
                string replaceString,
                PlaceholderTypes type)
            {
                this.Placeholder = placeholder;
                this.ReplaceString = replaceString;
                this.Type = type;
            }

            public string Placeholder { get; set; }
            public string ReplaceString { get; set; }
            public PlaceholderTypes Type { get; set; }
        }
    }
}
