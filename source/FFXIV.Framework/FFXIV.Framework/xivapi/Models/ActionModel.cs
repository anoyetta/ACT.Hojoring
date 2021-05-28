using FFXIV.Framework.XIVHelper;

namespace FFXIV.Framework.xivapi.Models
{
    public class ActionModel
    {
        public readonly static string HandledColumns = $"{nameof(ID)},{nameof(Name)},{nameof(ActionCategory)}.ID,{nameof(ClassJobCategory)}.ID,{nameof(ClassJobCategory)}.Name_en,{nameof(ClassJob)}.ID,{nameof(Icon)}";

        public int? ID { get; set; }
        public string Name { get; set; }
        public ActionCategoryModel ActionCategory { get; set; }
        public ClassJobCategoryModel ClassJobCategory { get; set; }
        public ClassJobModel ClassJob { get; set; }
        public string Icon { get; set; }

        public bool ContainsJob(
            JobIDs jobID)
        {
            var fromJob = this.ClassJob.ToLocalJob().ID;
            if (fromJob == jobID)
            {
                return true;
            }

            if (string.IsNullOrEmpty(this.ClassJobCategory.Name_en))
            {
                return false;
            }

            return this.ClassJobCategory.Name_en.Contains(jobID.ToString());
        }

        public override string ToString()
            => $"ID={this.ID ?? 0} Name={this.Name} Job={this.ClassJob.ToLocalJob()}";
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

    public class ClassJobCategoryModel
    {
        public int? ID { get; set; }

        public string Name_en { get; set; }
    }

    public class ClassJobModel
    {
        public int? ID { get; set; }

        public Job ToLocalJob() => this.ID.HasValue ?
            Jobs.Find(this.ID.Value) :
            new Job();
    }

    /// <summary>
    /// xivapi.com/ActionCategory
    /// </summary>
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
}
