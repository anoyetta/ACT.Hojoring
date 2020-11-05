using System;
using System.Collections.Generic;
using System.Linq;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;
using FFXIV_ACT_Plugin.Common.Models;
using Prism.Mvvm;
using PropertyChanged;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.XIVHelper
{
    [AddINotifyPropertyChangedInterface]
    public partial class CombatantEx :
        BindableBase,
        ICloneable
    {
        public static readonly GenericEqualityComparer<CombatantEx> CombatantExEqualityComparer = new GenericEqualityComparer<CombatantEx>(
            (x, y) => x.UUID == y.UUID,
            (obj) => obj.GetHashCode());

        public static void CopyToEx(
            Combatant source,
            CombatantEx destination)
        {
            var src = source;
            var dst = destination;

            dst.LastUpdateTimestamp = DateTime.Now;

            dst.ID = src.ID;
            dst.OwnerID = src.OwnerID;
            dst.Type = src.type;
            dst.Job = src.Job;
            dst.Level = src.Level;
            dst.Name = src.Name;
            dst.CurrentHP = src.CurrentHP;
            dst.MaxHP = src.MaxHP;
            dst.CurrentMP = src.CurrentMP;
            dst.CurrentTP = src.CurrentTP;
            dst.MaxTP = src.MaxTP;
            dst.CurrentCP = src.CurrentCP;
            dst.MaxCP = src.MaxCP;
            dst.CurrentGP = src.CurrentGP;
            dst.MaxGP = src.MaxGP;
            dst.IsCasting = src.CastBuffID != 0;
            dst.CastBuffID = src.CastBuffID;
            dst.CastTargetID = src.CastTargetID;
            dst.CastDurationCurrent = src.CastDurationCurrent;
            dst.CastDurationMax = src.CastDurationMax;
            dst.PosX = src.PosX;
            dst.PosY = src.PosY;
            dst.PosZ = src.PosZ;
            dst.Heading = src.Heading;
            dst.CurrentWorldID = src.CurrentWorldID;
            dst.WorldID = src.WorldID;
            dst.WorldName = src.WorldName;
            dst.BNpcNameID = src.BNpcNameID;
            dst.BNpcID = src.BNpcID;
            dst.TargetID = src.TargetID;
            dst.EffectiveDistance = src.EffectiveDistance;
            dst.PartyType = src.PartyType;
            dst.Pointer = src.Pointer;
            dst.Order = src.Order;

            dst.IncomingAbilities = src.IncomingAbilities;
            dst.OutgoingAbility = src.OutgoingAbility;
            dst.Effects = src.Effects;
            dst.FlyingText = src.FlyingText;

            dst.SetName(src.Name);
            SetSkillName(dst);
        }

        public static void CopyToEx(
            CombatantEx source,
            CombatantEx destination)
        {
            var src = source;
            var dst = destination;

            dst.LastUpdateTimestamp = DateTime.Now;

            dst.ID = src.ID;
            dst.OwnerID = src.OwnerID;
            dst.Type = src.Type;
            dst.Job = src.Job;
            dst.Level = src.Level;
            dst.Name = src.Name;
            dst.CurrentHP = src.CurrentHP;
            dst.MaxHP = src.MaxHP;
            dst.CurrentMP = src.CurrentMP;
            dst.CurrentTP = src.CurrentTP;
            dst.MaxTP = src.MaxTP;
            dst.CurrentCP = src.CurrentCP;
            dst.MaxCP = src.MaxCP;
            dst.CurrentGP = src.CurrentGP;
            dst.MaxGP = src.MaxGP;
            dst.IsCasting = src.CastBuffID != 0;
            dst.CastBuffID = src.CastBuffID;
            dst.CastTargetID = src.CastTargetID;
            dst.CastDurationCurrent = src.CastDurationCurrent;
            dst.CastDurationMax = src.CastDurationMax;
            dst.PosX = src.PosX;
            dst.PosY = src.PosY;
            dst.PosZ = src.PosZ;
            dst.Heading = src.Heading;
            dst.CurrentWorldID = src.CurrentWorldID;
            dst.WorldID = src.WorldID;
            dst.WorldName = src.WorldName;
            dst.BNpcNameID = src.BNpcNameID;
            dst.BNpcID = src.BNpcID;
            dst.TargetID = src.TargetID;
            dst.EffectiveDistance = src.EffectiveDistance;
            dst.PartyType = src.PartyType;
            dst.Pointer = src.Pointer;
            dst.Order = src.Order;

            dst.IncomingAbilities = src.IncomingAbilities;
            dst.OutgoingAbility = src.OutgoingAbility;
            dst.Effects = src.Effects;
            dst.FlyingText = src.FlyingText;

            dst.SetName(src.Name);
            dst.CastSkillName = src.CastSkillName;
            dst.IsDummy = src.IsDummy;
        }

        #region Additional Properties

        public uint UniqueObjectID => !this.IsNPC() ? this.ID : this.BNpcID;

        public Guid UUID { get; } = Guid.NewGuid();

        public bool IsDummy { get; set; } = false;

        public DateTime Timestamp { get; } = DateTime.Now;

        public DateTime LastUpdateTimestamp { get; private set; } = DateTime.Now;

        public ActorItemBase ActorItem { get; set; }

        public CombatantEx Player => CombatantsManager.Instance.Player;

        public bool IsPlayer => this.ID == this.Player?.ID;

        public Actor.Type ActorType => CombatantExtensions.ParseOrDefaultToActorType(this.Type);

        public int DisplayOrder =>
            PCOrder.Instance.PCOrders.Any(x => x.Job == this.JobID) ?
            PCOrder.Instance.PCOrders.FirstOrDefault(x => x.Job == this.JobID).Order :
            int.MaxValue;

        public JobIDs JobID =>
            Enum.IsDefined(typeof(JobIDs), (int)this.Job) ?
            (JobIDs)((int)this.Job) :
            JobIDs.Unknown;

        public bool IsPetJob
        {
            get
            {
                var jobID = this.JobID;
                return
                    jobID == JobIDs.SCH ||
                    jobID == JobIDs.SMN;
            }
        }

        private Job jobInfo = JobIDs.Unknown.GetInfo();

        public Job JobInfo => this.jobInfo.ID == this.JobID ?
            this.jobInfo :
            this.jobInfo = this.JobID.GetInfo();

        public Roles Role => this.JobInfo.Role;

        public string CastSkillName { get; set; } = string.Empty;

        public AttackTypes CastSkillType { get; set; } = AttackTypes.Unknown;

        public double CurrentHPRate => this.MaxHP != 0 ? ((double)this.CurrentHP / (double)this.MaxHP) : 0;

        public uint TargetOfTargetID { get; set; }

        public bool IsTargetOfTargetMe => this.ID == this.TargetOfTargetID;

        public bool IsAvailableEffectiveDistance => true;

        public double DistanceByPlayer =>
            this.Player != null ?
            GetDistance(this.Player, this) : 0;

        public double HorizontalDistanceByPlayer =>
            this.Player != null ?
            GetHorizontalDistance(this.Player, this) : 0;

        public static double GetDistance(CombatantEx from, CombatantEx to) =>
            Math.Round(
            (double)Math.Sqrt(
                Math.Pow(from.PosX - to.PosX, 2) +
                Math.Pow(from.PosY - to.PosY, 2) +
                Math.Pow(from.PosZ - to.PosZ, 2)),
            1, MidpointRounding.AwayFromZero);

        public static double GetHorizontalDistance(CombatantEx from, CombatantEx to) =>
            Math.Round(
            (double)Math.Sqrt(
                Math.Pow(from.PosX - to.PosX, 2) +
                Math.Pow(from.PosY - to.PosY, 2)),
            1, MidpointRounding.AwayFromZero);

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

        public static void SetSkillName(
            CombatantEx combatant)
        {
            if (combatant == null)
            {
                return;
            }

            if (combatant.ActorType != Actor.Type.PC &&
                combatant.ActorType != Actor.Type.Monster)
            {
                combatant.CastSkillName = string.Empty;
                return;
            }

            var skills = XIVPluginHelper.Instance.SkillList;

            if (combatant.IsCasting)
            {
                if (skills != null &&
                    skills.ContainsKey((uint)combatant.CastBuffID))
                {
                    var skill = skills[(uint)combatant.CastBuffID];
                    combatant.CastSkillName = skill.Name;
                    combatant.CastSkillType = skill.AttackType;
                }
                else
                {
                    combatant.CastSkillName =
                        $"UNKNOWN:{combatant.CastBuffID}";
                }
            }
            else
            {
                combatant.CastSkillName = string.Empty;
            }
        }

        #endregion Additional Properties

        #region Additional Properties - Names

        public string NameFI { get; set; } = string.Empty;

        public string NameIF { get; set; } = string.Empty;

        public string NameII { get; set; } = string.Empty;

        public string NameForDisplay => GetNameForDisplay(this);

        private static string GetNameForDisplay(CombatantEx com)
        {
            var name = com.Name;

            if (com.ActorType == Actor.Type.PC)
            {
                name = ConfigBridge.Instance.PCNameStyle switch
                {
                    NameStyles.FullInitial => com.NameFI,
                    NameStyles.InitialFull => com.NameIF,
                    NameStyles.InitialInitial => com.NameII,
                    _ => name,
                };
            }

            return name;
        }

        public bool ContainsName(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.Name) ||
                string.IsNullOrEmpty(this.NameFI) ||
                string.IsNullOrEmpty(this.NameIF) ||
                string.IsNullOrEmpty(this.NameII))
            {
                return false;
            }

            return
                text.ContainsIgnoreCase(this.Name) ||
                text.ContainsIgnoreCase(this.NameFI) ||
                text.ContainsIgnoreCase(this.NameIF) ||
                text.ContainsIgnoreCase(this.NameII);
        }

        public string Names { get; set; } = string.Empty;

        public void SetName(
            string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                this.Name = string.Empty;
                return;
            }

            fullName = fullName.Trim();

            if (this.ActorType != Actor.Type.PC)
            {
                this.Name = fullName;
                return;
            }

            var name1 = NameToInitial(fullName, NameStyles.FullName);
            var name2 = NameToInitial(fullName, NameStyles.FullInitial);
            var name3 = NameToInitial(fullName, NameStyles.InitialFull);
            var name4 = NameToInitial(fullName, NameStyles.InitialInitial);

            this.Name = name1;
            this.NameFI = name2;
            this.NameIF = name3;
            this.NameII = name4;

            var names = new List<string>() { name1, name2, name3, name4 };
            if (this.IsPlayer)
            {
                names.Add("You");
            }

            this.Names = string.Join("|", names.ToArray());
        }

        private static readonly char[] PCNameValidSymbols = new[] { '-', '\'' };

        public string NamesRegex
        {
            get
            {
                var result = this.Names
                    .Replace(@".", @"\.")
                    .Replace(@"-", @"\-");

                // 記号が含まれている？
                if (result.IndexOfAny(PCNameValidSymbols) > -1)
                {
                    // i オプションを付与する
                    result = $"(?i:{result})";
                }

                return result;
            }
        }

        public static readonly string UnknownName = "Unknown";

        public static string NameToInitial(
            string name,
            NameStyles style)
        {
            if (string.IsNullOrEmpty(name) ||
                name == UnknownName)
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
                case NameStyles.FullName:
                    name = $"{ToCamelCase(blocks[0])} {ToCamelCase(blocks[1])}";
                    break;

                case NameStyles.FullInitial:
                    name = $"{ToCamelCase(blocks[0])} {blocks[1].Substring(0, 1)}.";
                    break;

                case NameStyles.InitialFull:
                    name = $"{blocks[0].Substring(0, 1)}. {ToCamelCase(blocks[1])}";
                    break;

                case NameStyles.InitialInitial:
                    name = $"{blocks[0].Substring(0, 1)}. {blocks[1].Substring(0, 1)}.";
                    break;
            }

            return name;
        }

        private static string ToCamelCase(
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            if (name.Length == 1)
            {
                return name.ToUpper();
            }

            return Char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();
        }

        private static readonly CombatantEx EmptyCombatant = new CombatantEx()
        {
            Type = (byte)Actor.Type.PC
        };

        public static string[] GetNames(
            string fullName)
        {
            EmptyCombatant.SetName(fullName);
            return new[] { EmptyCombatant.Name, EmptyCombatant.NameFI, EmptyCombatant.NameIF, EmptyCombatant.NameII };
        }

        #endregion Additional Properties - Names

        #region Original Combatant

        public uint ID
        {
            get;
            set;
        }

        public uint OwnerID
        {
            get;
            set;
        }

        public byte Type
        {
            get;
            set;
        }

        public int Job
        {
            get;
            set;
        }

        public int Level
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public uint CurrentHP
        {
            get;
            set;
        }

        public uint MaxHP
        {
            get;
            set;
        }

        public uint CurrentMP
        {
            get;
            set;
        }

        public static uint ConstMaxMP = 10000;

        public uint MaxMP => ConstMaxMP;

        public uint CurrentTP
        {
            get;
            set;
        }

        public uint MaxTP
        {
            get;
            set;
        }

        public uint CurrentCP
        {
            get;
            set;
        }

        public uint MaxCP
        {
            get;
            set;
        }

        public uint CurrentGP
        {
            get;
            set;
        }

        public uint MaxGP
        {
            get;
            set;
        }

        public bool IsCasting
        {
            get;
            set;
        }

        public uint CastBuffID
        {
            get;
            set;
        }

        public uint CastTargetID
        {
            get;
            set;
        }

        public float CastDurationCurrent
        {
            get;
            set;
        }

        public float CastDurationMax
        {
            get;
            set;
        }

        public float PosX
        {
            get;
            set;
        }

        public float PosY
        {
            get;
            set;
        }

        public float PosZ
        {
            get;
            set;
        }

        public float Heading
        {
            get;
            set;
        }

        public uint CurrentWorldID
        {
            get;
            set;
        }

        public uint WorldID
        {
            get;
            set;
        }

        public string WorldName
        {
            get;
            set;
        }

        public uint BNpcNameID
        {
            get;
            set;
        }

        public uint BNpcID
        {
            get;
            set;
        }

        public uint TargetID
        {
            get;
            set;
        }

        public byte EffectiveDistance
        {
            get;
            set;
        }

        public PartyTypeEnum PartyType
        {
            get;
            set;
        }

        public IntPtr Pointer
        {
            get;
            set;
        }

        public int Order
        {
            get;
            set;
        }

        public IncomingAbility[] IncomingAbilities
        {
            get;
            set;
        }

        public OutgoingAbility OutgoingAbility
        {
            get;
            set;
        }

        public Effect[] Effects
        {
            get;
            set;
        }

        public FlyingText FlyingText
        {
            get;
            set;
        }

        #endregion Original Combatant

        #region ICloneable

        object ICloneable.Clone() => this.MemberwiseClone();

        public CombatantEx Clone() => (CombatantEx)this.MemberwiseClone();

        #endregion ICloneable

        public override string ToString() => $"{this.Name} {this.JobID}";
    }

    public static class CombatantExtensions
    {
        public static Actor.Type GetActorType(
            this Combatant c)
            => ParseOrDefaultToActorType(c?.type ?? 0);

        public static Actor.Type GetActorType(
            CombatantEx c)
            => ParseOrDefaultToActorType(c?.Type ?? 0);

        public static Actor.Type ParseOrDefaultToActorType(
            byte actorTypeValue)
        {
            switch (actorTypeValue)
            {
                case (byte)Actor.Type.PC:
                    return Actor.Type.PC;

                case (byte)Actor.Type.Monster:
                    return Actor.Type.Monster;

                case (byte)Actor.Type.NPC:
                    return Actor.Type.NPC;

                case (byte)Actor.Type.TreasureCoffer:
                    return Actor.Type.TreasureCoffer;

                case (byte)Actor.Type.Aetheryte:
                    return Actor.Type.Aetheryte;

                case (byte)Actor.Type.Gathering:
                    return Actor.Type.Gathering;

                case (byte)Actor.Type.EventObject:
                    return Actor.Type.EventObject;

                case (byte)Actor.Type.Mount:
                    return Actor.Type.Mount;

                case (byte)Actor.Type.Minion:
                    return Actor.Type.Minion;

                default:
                    return Actor.Type.Unknown;
            }
        }
    }
}
