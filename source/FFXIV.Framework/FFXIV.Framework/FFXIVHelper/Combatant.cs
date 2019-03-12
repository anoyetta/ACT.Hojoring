using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.FFXIVHelper
{
    public class Combatant :
        ICloneable,
        INotifyPropertyChanged
    {
        private static long currentIndex = 0;

        public Combatant()
        {
            // インデックスを採番する
            currentIndex++;
            this.Index = currentIndex;
            if (currentIndex >= long.MaxValue)
            {
                currentIndex = 0;
            }
        }

        public static readonly GenericEqualityComparer<Combatant> CombatantEqualityComparer = new GenericEqualityComparer<Combatant>(
            (x, y) => x.GUID == y.GUID,
            (obj) => obj.GetHashCode());

        public Guid GUID { get; } = Guid.NewGuid();

        public long Index { get; private set; }

        public uint ID;
        public uint OwnerID;
        public int Order;
        public uint TargetID;

        public byte Job;
        public byte Level;
        public string Name;

        public int CurrentHP;
        public int MaxHP;
        public int CurrentMP;
        public int MaxMP;
        public short CurrentTP;
        public short MaxTP;
        public int CurrentCP;
        public int MaxCP;
        public int CurrentGP;
        public int MaxGP;

        public Single PosX;
        public Single PosY;
        public Single PosZ;
        public Single Heading;
        public bool IsAvailableEffectiveDictance;
        public byte EffectiveDistance;
        public string Distance;
        public string HorizontalDistance;

        public bool IsCasting;
        public uint CastTargetID;
        public int CastBuffID;
        public float CastDurationCurrent;
        public float CastDurationMax;
        public string CastSkillName = string.Empty;

        public DateTime Timestamp = DateTime.Now;

        public uint TargetOfTargetID;

        public Combatant Player => FFXIVPlugin.Instance.GetPlayer();

        public bool IsPlayer => this.ID == this.Player?.ID;

        public Actor.Type ObjectType { get; set; }

        public ActorItemBase ActorItem { get; internal set; }

        public bool IsTargetOfTargetMe => this.ID == this.TargetOfTargetID;

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

                if (this.ObjectType == Actor.Type.PC)
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

            if (this.ObjectType != Actor.Type.PC)
            {
                return;
            }

            this.NameFI = NameToInitial(this.Name, NameStyles.FullInitial);
            this.NameIF = NameToInitial(this.Name, NameStyles.InitialFull);
            this.NameII = NameToInitial(this.Name, NameStyles.InitialInitial);
        }

        public static readonly string UnknownName = "Unknown";

        public static string NameToInitial(
            string name,
            NameStyles style)
        {
            if (string.IsNullOrEmpty(name) || name == UnknownName)
            {
                return UnknownName;
            }

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
            ObjectType = Actor.Type.PC
        };

        public static string[] GetNames(
            this string fullName)
        {
            combatant.SetName(fullName);
            return new[] { combatant.Name, combatant.NameFI, combatant.NameIF, combatant.NameII };
        }
    }
}
