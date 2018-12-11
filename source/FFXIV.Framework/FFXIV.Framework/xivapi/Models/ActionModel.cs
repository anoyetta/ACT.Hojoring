using FFXIV.Framework.FFXIVHelper;

namespace FFXIV.Framework.xivapi.Models
{
    public class ActionModel
    {
        public readonly static string HandledColumns = $"{nameof(ID)},{nameof(Name)},{nameof(ActionCategory)}.ID,{nameof(ClassJob)}.ID,{nameof(Icon)}";

        public int ID { get; set; }
        public string Name { get; set; }
        public ActionCategoryModel ActionCategory { get; set; }
        public ClassJobModel ClassJob { get; set; }
        public string Icon { get; set; }

        public override string ToString()
            => $"ID={this.ID} Name={this.Name}";
    }

    public class ActionCategoryModel
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public string Name_de { get; set; }
        public string Name_en { get; set; }
        public string Name_fr { get; set; }
        public string Name_ja { get; set; }
    }

    public class ClassJobModel
    {
        public int? ID { get; set; }

        public Job ToLocalJob() => this.ID.HasValue ?
            Jobs.Find(this.ID.Value) :
            new Job();

        public ActionUserCategory ToLocalCategory()
        {
            switch (this.ToLocalJob().ID)
            {
                // タンク
                case JobIDs.PLD:
                case JobIDs.GLA:
                    return ActionUserCategory.Paladin;

                case JobIDs.WAR:
                case JobIDs.MRD:
                    return ActionUserCategory.Warrior;

                case JobIDs.DRK:
                    return ActionUserCategory.DarkKnight;

                // ヒーラー
                case JobIDs.WHM:
                case JobIDs.CNJ:
                    return ActionUserCategory.WhiteMage;

                case JobIDs.SCH:
                    return ActionUserCategory.Scholar;

                case JobIDs.AST:
                    return ActionUserCategory.Astrologian;

                // メレー
                case JobIDs.MNK:
                case JobIDs.PUG:
                    return ActionUserCategory.Monk;

                case JobIDs.DRG:
                case JobIDs.LNC:
                    return ActionUserCategory.Dragoon;

                case JobIDs.NIN:
                case JobIDs.ROG:
                    return ActionUserCategory.Ninja;

                case JobIDs.SAM:
                    return ActionUserCategory.Samurai;

                // レンジ
                case JobIDs.BRD:
                case JobIDs.ARC:
                    return ActionUserCategory.Bard;

                case JobIDs.MCH:
                    return ActionUserCategory.Machinist;

                // マジック
                case JobIDs.BLM:
                case JobIDs.THM:
                    return ActionUserCategory.BlackMage;

                case JobIDs.SMN:
                case JobIDs.ACN:
                    return ActionUserCategory.Summoner;

                case JobIDs.RDM:
                    return ActionUserCategory.RedMage;
            }

            return ActionUserCategory.Nothing;
        }
    }

    public enum ActionCategory
    {
        AutoAttack = 1,
        Spell,
        Weaponskill,
        Ability,
        Item,
        DoLAbility,
        DoHAbility,
        Event,
        System,
        Artillery,
        Mount,
        Glamour,
        AdrenalineRush
    }

    /// <summary>
    /// Hojoringとしてアクションの分類するためのカテゴリ分け
    /// </summary>
    public enum ActionUserCategory
    {
        Nothing = 0,

        TankRole = 10,
        Paladin = 11,
        Warrior = 12,
        DarkKnight = 13,

        HealerRole = 20,
        WhiteMage = 21,
        Scholar = 22,
        Astrologian = 23,

        MeleeDPSRole = 30,
        Monk = 31,
        Dragoon = 32,
        Ninja = 33,
        Samurai = 34,

        RangeDPSRole = 40,
        Bard = 41,
        Machinist = 42,

        MagicDPSRole = 50,
        BlackMage = 51,
        Summoner = 52,
        RedMage = 53,

        PetsEgi = 61,
        PetsFairy = 62,

        Others = 90,
    }
}
