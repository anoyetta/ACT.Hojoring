using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FFXIV.Framework.Bridge;
using FFXIV.Framework.Common;
using FFXIV_ACT_Plugin.Common.Models;
using PropertyChanged;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace FFXIV.Framework.FFXIVHelper
{
    public static class Combatanttensions
    {
        private static readonly ConditionalWeakTable<Combatant, Dictionary<string, object>> ExtensionValues = new ConditionalWeakTable<Combatant, Dictionary<string, object>>();

        public static void InitExtension(
            this Combatant com)
        {
            ExtensionValues.GetOrCreateValue(com)["GUID"] = Guid.NewGuid();
            ExtensionValues.GetOrCreateValue(com)["Timestamp"] = DateTime.Now;
        }

        public static readonly GenericEqualityComparer<Combatant> CombatantEqualityComparer = new GenericEqualityComparer<Combatant>(
            (x, y) => x.GetGuid() == y.GetGuid(),
            (obj) => obj.GetHashCode());

        public static GenericEqualityComparer<Combatant> GetCombatantEqualityComparer(this Combatant com)
            => CombatantEqualityComparer;

        public static Guid GetGuid(this Combatant com)
            => (Guid)ExtensionValues.GetOrCreateValue(com)["GUID"];

        public static DateTime GetTimestamp(this Combatant com)
            => (DateTime)ExtensionValues.GetOrCreateValue(com)["Timestamp"];

        public static ActorItemBase GetActor(this Combatant com)
            => (ActorItemBase)ExtensionValues.GetOrCreateValue(com)["Actor"];

        public static void SetActor(this Combatant com, ActorItemBase value)
            => ExtensionValues.GetOrCreateValue(com)["Actor"] = value;

        public static Actor.Type GetActorType(this Combatant com)
            => (Actor.Type)Enum.ToObject(typeof(Actor.Type), com.type);

        public static void SetActorType(this Combatant com, Actor.Type value)
            => com.type = (byte)value;

        public static string GetCastSkillName(this Combatant com)
            => (string)ExtensionValues.GetOrCreateValue(com)["CastSkillName"];

        public static void SetCastSkillName(this Combatant com, string value)
            => ExtensionValues.GetOrCreateValue(com)["CastSkillName"] = value;

        public static bool IsAvailableEffectiveDistance(this Combatant com)
            => true;

        public static double GetCurrentHPRate(this Combatant com)
            => com.MaxHP != 0 ? (com.CurrentHP / com.MaxHP) : 0;

        public static uint GetTargetOfTargetID(this Combatant com)
            => (uint)ExtensionValues.GetOrCreateValue(com)["TargetOfTargetID"];

        public static void SetTargetOfTarget(this Combatant com, uint value)
            => ExtensionValues.GetOrCreateValue(com)["TargetOfTargetID"] = value;

        public static bool IsTargetOfTargetMe(this Combatant com)
            => com.ID == com.GetTargetOfTargetID();

        public static Combatant GetPlayer(this Combatant com)
            => FFXIVPlugin.Instance.GetPlayer();

        public static bool IsPlayer(this Combatant com)
            => com.ID == com.GetPlayer()?.ID;

        public static double GetDistanceByPlayer(this Combatant com) =>
            com.GetPlayer() != null ?
            com.GetDistance(com.GetPlayer()) : 0;

        public static double GetHorizontalDistanceByPlayer(this Combatant com) =>
            com.GetPlayer() != null ?
            com.GetHorizontalDistance(com.GetPlayer()) : 0;

        public static double GetDistance(this Combatant com, Combatant target) =>
            Math.Round(
            (double)Math.Sqrt(
                Math.Pow(com.PosX - target.PosX, 2) +
                Math.Pow(com.PosY - target.PosY, 2) +
                Math.Pow(com.PosZ - target.PosZ, 2)),
            1, MidpointRounding.AwayFromZero);

        public static double GetHorizontalDistance(this Combatant com, Combatant target) =>
            Math.Round(
            (double)Math.Sqrt(
                Math.Pow(com.PosX - target.PosX, 2) +
                Math.Pow(com.PosY - target.PosY, 2)),
            1, MidpointRounding.AwayFromZero);

        #region Names

        public static string GetNameFI(this Combatant com)
            => (string)ExtensionValues.GetOrCreateValue(com)["NameFI"];

        public static string GetNameIF(this Combatant com)
            => (string)ExtensionValues.GetOrCreateValue(com)["NameIF"];

        public static string GetNameII(this Combatant com)
            => (string)ExtensionValues.GetOrCreateValue(com)["NameII"];

        public static string GetNameForDisplay(this Combatant com)
        {
            var name = com.Name;

            if (com.GetActorType() == Actor.Type.PC)
            {
                name = ConfigBridge.Instance.PCNameStyle switch
                {
                    NameStyles.FullInitial => com.GetNameFI(),
                    NameStyles.InitialFull => com.GetNameIF(),
                    NameStyles.InitialInitial => com.GetNameII(),
                    _ => name,
                };
            }

            return name;
        }

        public static string GetNames(this Combatant com)
            => (string)ExtensionValues.GetOrCreateValue(com)["Names"];

        public static void SetName(
            this Combatant com,
            string fullName)
        {
            com.Name = fullName.Trim();

            if (com.GetActorType() != Actor.Type.PC)
            {
                return;
            }

            var name1 = NameToInitial(com.Name, NameStyles.FullName);
            var name2 = NameToInitial(com.Name, NameStyles.FullInitial);
            var name3 = NameToInitial(com.Name, NameStyles.InitialFull);
            var name4 = NameToInitial(com.Name, NameStyles.InitialInitial);

            com.Name = name1;
            ExtensionValues.GetOrCreateValue(com)["NameFI"] = name2;
            ExtensionValues.GetOrCreateValue(com)["NameIF"] = name3;
            ExtensionValues.GetOrCreateValue(com)["NameII"] = name4;
            ExtensionValues.GetOrCreateValue(com)["Names"] = string.Join(
                "|",
                new[] { name1, name2, name3, name4 });
        }

        public static string GetNamesRegex(this Combatant com)
            => com.GetNames()
                .Replace(@".", @"\.")
                .Replace(@"-", @"\-");

        public static readonly string UnknownName = "Unknown";

        private static string NameToInitial(
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

        #endregion Names
    }

    [AddINotifyPropertyChangedInterface]
    public class CombatantA :
        Combatant,
        ICloneable
    {
        public int DisplayOrder =>
            PCOrder.Instance.PCOrders.Any(x => x.Job == this.JobID) ?
            PCOrder.Instance.PCOrders.FirstOrDefault(x => x.Job == this.JobID).Order :
            int.MaxValue;

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

        public Job AsJob() => Jobs.Find(this.Job);

        public Roles Role => this.AsJob()?.Role ?? Roles.Unknown;

        public override string ToString() =>
            $"{this.Name}, {this.JobID}";

        public Combatant Clone()
        {
            var clone = (Combatant)this.MemberwiseClone();
            return clone;
        }

        object ICloneable.Clone()
        {
            var clone = this.MemberwiseClone() as Combatant;
            return clone;
        }
    }

    public static class NameEx
    {
        private static readonly Combatant combatant = new Combatant()
        {
            type = (byte)Actor.Type.PC
        };

        public static string[] GetNames(
            this string fullName)
        {
            combatant.SetName(fullName);
            return new[] { combatant.Name, combatant.GetNameFI(), combatant.GetNameIF(), combatant.GetNameII() };
        }
    }
}
