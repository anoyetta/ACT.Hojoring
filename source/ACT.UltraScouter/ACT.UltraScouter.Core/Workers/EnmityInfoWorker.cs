using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACT.UltraScouter.Config;
using ACT.UltraScouter.Models;
using ACT.UltraScouter.Models.Enmity;
using ACT.UltraScouter.ViewModels;
using Advanced_Combat_Tracker;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.FFXIVHelper;
using Sharlayan.Core.Enums;

namespace ACT.UltraScouter.Workers
{
    public class EnmityInfoWorker :
        TargetInfoWorker
    {
        #region Singleton

        private static EnmityInfoWorker instance;

        public static new EnmityInfoWorker Instance => instance;

        public static new void Initialize() => instance = new EnmityInfoWorker();

        public static new void Free()
        {
            lock (LogLocker)
            {
                if (instance.logStream != null)
                {
                    instance.logStream.Flush();
                    instance.logStream.Close();
                    instance.logStream = null;
                }
            }

            instance = null;
        }

        private EnmityInfoWorker()
        {
        }

        #endregion Singleton

        /// <summary>
        /// サブオーバーレイである
        /// </summary>
        protected override bool IsSubOverlay => true;

        public override TargetInfoModel Model => TargetInfoModel.Instance;

        protected override void GetCombatant()
        {
            SharlayanHelper.Instance.IsSkipEnmity = !Settings.Instance.Enmity.Visible;

            if (!Settings.Instance.Enmity.Visible)
            {
                this.ClearCurrentEnmity();
                this.DiffSampleTimer.Stop();
                return;
            }

            var targetInfo = this.TargetInfo;

            if (targetInfo == null)
            {
                this.ClearCurrentEnmity();
                this.DiffSampleTimer.Stop();
                return;
            }

            if (!this.DiffSampleTimer.IsRunning)
            {
                this.DiffSampleTimer.Start();
            }

            if (!Settings.Instance.Enmity.IsDesignMode)
            {
                if (targetInfo.ActorType == Actor.Type.Monster)
                {
                    var enmityEntryList = SharlayanHelper.Instance.EnmityList;
                    this.RefreshCurrentEnmityModelList(enmityEntryList);
                }
                else
                {
                    this.ClearCurrentEnmity();
                }
            }
        }

        private void RefreshCurrentEnmityModelList(
            IEnumerable<EnmityEntry> enmityEntryList)
        {
            lock (this.CurrentEnmityModelList)
            {
                this.CurrentEnmityModelList.Clear();

                if (enmityEntryList == null)
                {
                    return;
                }

                var config = Settings.Instance.Enmity;
                var pcNameStyle = ConfigBridge.Instance.PCNameStyle;

                var count = 0;
                foreach (var entry in enmityEntryList)
                {
                    if (entry.ID == 0)
                    {
                        continue;
                    }

                    count++;

                    if (count > config.MaxCountOfDisplay)
                    {
                        break;
                    }

                    var name = entry.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = CombatantEx.UnknownName;
                    }
                    else
                    {
                        name = config.IsSelfDisplayYou && entry.IsMe ?
                            "YOU" :
                            CombatantEx.NameToInitial(entry.Name, pcNameStyle);
                    }

                    var model = new EnmityModel();
                    model.Index = count;
                    model.ID = entry.ID;
                    model.Name = name;
                    model.JobID = (JobIDs)entry.Job;
                    model.Enmity = (double)entry.Enmity;
                    model.HateRate = entry.HateRate / 100f;
                    model.IsMe = entry.IsMe;
                    model.IsPet = entry.IsPet;
                    model.IsTop = count <= 1;
                    this.CurrentEnmityModelList.Add(model);

                    Thread.Yield();
                }

                // EPSを更新する
                var isRefreshed = this.RefreshEPS();

                // ログを出力する
                if (isRefreshed)
                {
                    this.WriteEnmityLog(enmityEntryList);
                }
            }
        }

        private bool RefreshEPS()
        {
            if (this.DiffSampleTimer.Elapsed.TotalSeconds < 3.0)
            {
                return false;
            }

            this.DiffSampleTimer.Restart();

            var me = this.CurrentEnmityModelList.FirstOrDefault(x => x.IsMe);
            var sample = (
                from x in this.CurrentEnmityModelList
                where
                x.JobID == JobIDs.PLD ||
                x.JobID == JobIDs.WAR ||
                x.JobID == JobIDs.DRK
                orderby
                Math.Abs(x.Enmity - (me?.Enmity ?? 0)) ascending
                select
                x).FirstOrDefault();

            if (sample == null)
            {
                this.currentTankEPS = 0;
                this.currentNearThreshold = 0;

                return true;
            }

            var last = this.DiffEnmityList.LastOrDefault();

            var diff = Math.Abs(sample.Enmity - (last?.Value ?? 0));

            var now = DateTime.Now;

            this.DiffEnmityList.Add(new DiffEnmity()
            {
                Timestamp = now,
                SampleName = sample.Name,
                Value = sample.Enmity,
                Diff = diff,
            });

            var olds = this.DiffEnmityList
                .Where(x => x.Timestamp < now.AddSeconds(-30))
                .ToArray();

            foreach (var item in olds)
            {
                this.DiffEnmityList.Remove(item);
            }

            var parameters = this.DiffEnmityList
                .Where(x =>
                    this.currentTankEPS <= 0 ||
                    x.Diff <= (this.currentTankEPS * 10.0));

            // EPSと危険域閾値を算出する
            this.currentTankEPS = parameters.Any() ?
                parameters.Average(x => x.Diff) :
                0;
            this.currentNearThreshold = this.currentTankEPS * Settings.Instance.Enmity.NearThresholdRate;

            return true;
        }

        private void ClearCurrentEnmity()
        {
            if (this.CurrentEnmityModelList.Count > 0)
            {
                lock (this.CurrentEnmityModelList)
                {
                    this.CurrentEnmityModelList.Clear();
                }
            }
        }

        private volatile bool currentInCombat = false;
        private volatile string currentZone = string.Empty;
        private volatile int currentTake = 0;
        private volatile string currentLogFile = string.Empty;
        private static readonly object LogLocker = new object();
        private StreamWriter logStream;

        private async void WriteEnmityLog(
            IEnumerable<EnmityEntry> enmityEntryList)
        {
            var config = Settings.Instance.Enmity;

            if (string.IsNullOrEmpty(config.LogDirectory))
            {
                return;
            }

            var player = CombatantsManager.Instance.Player;
            if (player == null)
            {
                return;
            }

#if !DEBUG
            if (SharlayanHelper.Instance.PartyMemberCount < 4)
            {
                return;
            }
#endif

            if (enmityEntryList.Count() < 1)
            {
                return;
            }

            if (!Directory.Exists(config.LogDirectory))
            {
                Directory.CreateDirectory(config.LogDirectory);
            }

            var now = DateTime.Now;

            await Task.Run(() =>
            {
                lock (LogLocker)
                {
                    var inCombat = FFXIVPlugin.Instance.InCombat;
                    if (this.currentInCombat != inCombat)
                    {
                        this.currentInCombat = inCombat;

                        if (this.currentInCombat)
                        {
                            this.currentTake++;
                        }
                        else
                        {
                            this.logStream?.Flush();
                        }
                    }

                    var zone = ActGlobals.oFormActMain.CurrentZone;
                    if (this.currentZone != zone)
                    {
                        this.currentZone = zone;
                        this.logStream?.Flush();
                    }

                    var f = $"{now:yyyy-MM-dd}.Enmity.{zone}.take{this.currentTake}.csv";
                    f = string.Concat(f.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));

                    var fileName = Path.Combine(
                        config.LogDirectory,
                        f);

                    if (string.IsNullOrEmpty(this.currentLogFile) ||
                        this.currentLogFile != fileName)
                    {
                        this.currentLogFile = fileName;

                        if (this.logStream != null)
                        {
                            this.logStream.Flush();
                            this.logStream.Close();
                            this.logStream = null;
                        }

                        this.logStream = new StreamWriter(
                            this.currentLogFile,
                            false,
                            new UTF8Encoding(false));
                    }

                    if (this.logStream != null)
                    {
                        var fields = new List<string>(24);

                        fields.Add(now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        fields.Add("\"" + this.TargetInfo.Name + "\"");

                        var party = CombatantsManager.Instance.GetPartyList();

                        foreach (var member in party)
                        {
                            Thread.Yield();

                            var enmityData = enmityEntryList.FirstOrDefault(x =>
                                x.ID == member.ID);

                            fields.Add("\"" + member.Name + "\"");
                            fields.Add("\"" + member.JobID.ToString() + "\"");
                            fields.Add((enmityData?.Enmity ?? 0).ToString("#"));
                        }

                        this.logStream.WriteLine(string.Join(",", fields));
                    }
                }
            });
        }

        private readonly List<EnmityModel> CurrentEnmityModelList = new List<EnmityModel>(32);
        private readonly List<DiffEnmity> DiffEnmityList = new List<DiffEnmity>(64);
        private readonly Stopwatch DiffSampleTimer = new Stopwatch();
        private double currentTankEPS = 0;
        private double currentNearThreshold = 0;

        protected override NameViewModel NameVM => null;

        protected override HPViewModel HpVM => null;

        protected override HPBarViewModel HpBarVM => null;

        protected override ActionViewModel ActionVM => null;

        protected override DistanceViewModel DistanceVM => null;

        protected override FFLogsViewModel FFLogsVM => null;

        protected override EnmityViewModel EnmityVM =>
            this.enmityVM ?? (this.enmityVM = new EnmityViewModel(Settings.Instance.Enmity, this.Model));

        protected override bool IsAllViewOff =>
            !FFXIVPlugin.Instance.IsFFXIVActive ||
            !Settings.Instance.Enmity.Visible;

        public override CombatantEx TargetInfo => TargetInfoWorker.Instance.TargetInfo;

        public override CombatantEx TargetInfoClone => TargetInfoWorker.Instance.TargetInfoClone;

        private DateTime enmityTimestamp = DateTime.MinValue;

        protected override void RefreshEnmityView(CombatantEx targetInfo)
        {
            if (this.EnmityView == null)
            {
                return;
            }

            if (!this.EnmityView.ViewModel.OverlayVisible)
            {
                return;
            }

            var now = DateTime.Now;
            if ((now - this.enmityTimestamp).TotalMilliseconds <= 100d)
            {
                return;
            }

            this.enmityTimestamp = now;

            EnmityModel.CreateBrushes();

            if (targetInfo == null)
            {
                this.Model.RefreshEnmityList(null);
                return;
            }

            lock (this.CurrentEnmityModelList)
            {
                this.Model.RefreshEnmityList(
                    this.CurrentEnmityModelList,
                    this.currentTankEPS,
                    this.currentNearThreshold);
            }
        }
    }

    public class DiffEnmity
    {
        public DateTime Timestamp { get; set; }

        public string SampleName { get; set; }

        public double Value { get; set; }

        public double Diff { get; set; }
    }
}
