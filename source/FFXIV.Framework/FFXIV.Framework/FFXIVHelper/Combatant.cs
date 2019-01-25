using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using TamanegiMage.FFXIV_MemoryReader.Model;

namespace FFXIV.Framework.FFXIVHelper
{
    public class Combatant :
        CombatantV1,
        ICloneable,
        INotifyPropertyChanged
    {
        private static long currentIndex = 0;

        public Combatant()
        {
        }

        public Combatant(
            CombatantV1 v1)
        {
            // インデックスを採番する
            currentIndex++;
            this.index = currentIndex;
            if (currentIndex >= long.MaxValue)
            {
                currentIndex = 0;
            }

            this.ID = v1.ID;

            this.HorizontalDistance = v1.HorizontalDistance;
            this.Distance = v1.Distance;
            this.EffectiveDistance = v1.EffectiveDistance;
            this.IsAvailableEffectiveDictance = true;

            this.Heading = v1.Heading;
            this.PosX = v1.PosX;
            this.PosY = v1.PosY;
            this.PosZ = v1.PosZ;

            this.Casting = v1.Casting;
            this.Statuses = v1.Statuses;

            this.CurrentHP = v1.CurrentHP;
            this.MaxHP = v1.MaxHP;
            this.CurrentMP = v1.CurrentMP;
            this.MaxMP = v1.MaxMP;
            this.CurrentTP = v1.CurrentTP;
            this.MaxTP = v1.MaxTP;

            this.Name = v1.Name;
            this.Level = v1.Level;
            this.Job = v1.Job;
            this.TargetID = v1.TargetID;
            this.type = v1.type;
            this.Order = v1.Order;
            this.OwnerID = v1.OwnerID;

            this.IsCasting = this.Casting.IsValid();
            this.CastTargetID = this.Casting.TargetID;
            this.CastBuffID = this.Casting.ID;
            this.CastDurationCurrent = this.Casting.Progress;
            this.CastDurationMax = this.Casting.Time;
        }

        private long index = 0;

        public long Index => this.index;

        public uint TargetOfTargetID;

        public bool IsTargetOfTargetMe =>
            this.ID == this.TargetOfTargetID;

        public bool IsCasting;
        public uint CastTargetID;
        public int CastBuffID;
        public float CastDurationCurrent;
        public float CastDurationMax;
        public string CastSkillName = string.Empty;

        // FFXIV_ACT_Plugin v1.7.1.0
        private int worldID;

        public int WorldID
        {
            get => this.worldID;
            set => this.SetProperty(ref this.worldID, value);
        }

        // FFXIV_ACT_Plugin v1.7.1.0
        private string worldName;

        public string WorldName
        {
            get => this.worldName;
            set => this.SetProperty(ref this.worldName, value);
        }

        /// <summary>イニシャル Naoki Y.</summary>
        public string NameFI = string.Empty;

        /// <summary>イニシャル N. Yoshida</summary>
        public string NameIF = string.Empty;

        /// <summary>イニシャル N. Y.</summary>
        public string NameII = string.Empty;

        public string NameForDisplay
        {
            get
            {
                var name = this.Name;

                if (this.type == ObjectType.PC)
                {
                    switch (ConfigBridge.Instance.PCNameStyle)
                    {
                        case NameStyles.FullInitial:
                            name = this.NameFI;
                            break;

                        case NameStyles.InitialFull:
                            name = this.NameIF;
                            break;

                        case NameStyles.InitialInitial:
                            name = this.NameII;
                            break;
                    }
                }

                return name;
            }
        }

        public bool IsAvailableEffectiveDictance;
        public Combatant Player;

        public bool IsPlayer => this.ID == this.Player?.ID;

        public int DisplayOrder =>
            PCOrder.Instance.PCOrders.Any(x => x.Job == this.JobID) ?
            PCOrder.Instance.PCOrders.FirstOrDefault(x => x.Job == this.JobID).Order :
            int.MaxValue;

        public double CurrentHPRate =>
            Math.Round(
                this.MaxHP == 0 ?
                0 :
                (double)this.CurrentHP / (double)this.MaxHP,
            3, MidpointRounding.AwayFromZero);

        public double DistanceByPlayer =>
            this.Player != null ?
            this.GetDistance(this.Player) : 0;

        public double HorizontalDistanceByPlayer =>
            this.Player != null ?
            this.GetHorizontalDistance(this.Player) : 0;

        public double GetDistance(Combatant target) =>
            Math.Round(
            (double)Math.Sqrt(
                Math.Pow(this.PosX - target.PosX, 2) +
                Math.Pow(this.PosY - target.PosY, 2) +
                Math.Pow(this.PosZ - target.PosZ, 2)),
            1, MidpointRounding.AwayFromZero);

        public double GetHorizontalDistance(Combatant target) =>
            Math.Round(
            (double)Math.Sqrt(
                Math.Pow(this.PosX - target.PosX, 2) +
                Math.Pow(this.PosY - target.PosY, 2)),
            1, MidpointRounding.AwayFromZero);

        public JobIDs JobID =>
            Enum.IsDefined(typeof(JobIDs), (int)this.Job) ?
            (JobIDs)((int)this.Job) :
            JobIDs.Unknown;

        public float PosXMap => (float)ToHorizontalMapPosition(this.PosX);

        public float PosYMap => (float)ToHorizontalMapPosition(this.PosY);

        public float PosZMap => (float)ToVerticalMapPosition(this.PosZ);

        public double HeadingDegree => (this.Heading + 3.0) / 6.0 * 360.0 * -1.0;

        /// <summary>
        /// X,Y軸をゲーム内のマップ座標に変換する
        /// </summary>
        /// <param name="rawHorizontalPosition"></param>
        /// <returns></returns>
        public static double ToHorizontalMapPosition(
            double rawHorizontalPosition)
        {
            const double Offset = 21.5;
            const double Pitch = 50.0;
            return Offset + (rawHorizontalPosition / Pitch);
        }

        /// <summary>
        /// Z軸をゲーム内のマップ座標に変換する
        /// </summary>
        /// <param name="rawVerticalPosition"></param>
        /// <returns></returns>
        public static double ToVerticalMapPosition(
            double rawVerticalPosition)
        {
            const double Offset = 1.0;
            const double Pitch = 100.0;
            return ((rawVerticalPosition - Offset) / Pitch) + 0.01;
        }

        public string Names
        {
            get
            {
                var names = new List<string>();

                if (!string.IsNullOrEmpty(this.Name))
                {
                    names.Add(this.Name);
                }

                if (!string.IsNullOrEmpty(this.NameFI))
                {
                    names.Add(this.NameFI);
                }

                if (!string.IsNullOrEmpty(this.NameIF))
                {
                    names.Add(this.NameIF);
                }

                if (!string.IsNullOrEmpty(this.NameII))
                {
                    names.Add(this.NameII);
                }

                return string.Join("|", names.ToArray());
            }
        }

        public string NamesRegex =>
            this.Names.Replace(@".", @"\.");

        public Job AsJob() => Jobs.Find(this.Job);

        public Roles Role => this.AsJob()?.Role ?? Roles.Unknown;

        public void SetName(
            string fullName)
        {
            this.Name = fullName.Trim();

            if (this.type != ObjectType.PC)
            {
                return;
            }

            this.NameFI = NameToInitial(this.Name, NameStyles.FullInitial);
            this.NameIF = NameToInitial(this.Name, NameStyles.InitialFull);
            this.NameII = NameToInitial(this.Name, NameStyles.InitialInitial);
        }

        public static string NameToInitial(
            string name,
            NameStyles style)
        {
            var blocks = name.Split(' ');
            if (blocks.Length < 2)
            {
                return name;
            }

            switch (style)
            {
                case NameStyles.FullInitial:
                    name = $"{blocks[0]} {blocks[1].Substring(0, 1)}.";
                    break;

                case NameStyles.InitialFull:
                    name = $"{blocks[0].Substring(0, 1)}. {blocks[1]}";
                    break;

                case NameStyles.InitialInitial:
                    name = $"{blocks[0].Substring(0, 1)}. {blocks[1].Substring(0, 1)}.";
                    break;
            }

            return name;
        }

        public List<EnmityEntry> EnmityEntryList { get; set; } = new List<EnmityEntry>();

        public override string ToString() =>
            $"{this.Name}, {this.JobID}";

        public Combatant Clone()
        {
            var clone = (Combatant)this.MemberwiseClone();
            clone.EnmityEntryList = new List<EnmityEntry>(this.EnmityEntryList);
            return clone;
        }

        object ICloneable.Clone()
        {
            var clone = this.MemberwiseClone() as Combatant;
            clone.EnmityEntryList = new List<EnmityEntry>(this.EnmityEntryList);
            return clone;
        }

        #region Get Properties

        public void NotifyProperties()
        {
            var pis = this.GetType().GetProperties();

            WPFHelper.BeginInvoke(() =>
            {
                foreach (var pi in pis)
                {
                    this.RaisePropertyChanged(pi.Name);
                }
            });
        }

        public uint GetID => this.ID;

        public string GetName => this.Name;

        public int GetCurrentHP => this.CurrentHP;

        public int GetMaxHP => this.MaxHP;

        #endregion Get Properties

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        #endregion INotifyPropertyChanged
    }

    public static class NameEx
    {
        private static readonly Combatant combatant = new Combatant()
        {
            type = ObjectType.PC
        };

        public static string[] GetNames(
            this string fullName)
        {
            combatant.SetName(fullName);
            return new[] { combatant.Name, combatant.NameFI, combatant.NameIF, combatant.NameII };
        }
    }
}
