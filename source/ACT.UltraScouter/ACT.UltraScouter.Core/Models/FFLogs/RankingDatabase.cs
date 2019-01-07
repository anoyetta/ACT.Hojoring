using System.Collections.Generic;
using System.Linq;
using ACT.UltraScouter.Config;

namespace ACT.UltraScouter.Models.FFLogs
{
    public class RankingDatabase
    {
        public Dictionary<int, BasicEntryModel> SpecDictionary { get; set; }

        public List<RankingModel> RankingList { get; } = new List<RankingModel>();

        public void AddRankings(
            string encounterName,
            IEnumerable<RankingModel> rankings)
        {
            var targets = rankings
                .Where(x => x.Region == Settings.Instance.FFLogs.ServerRegion.ToString());

            foreach (var item in targets)
            {
                item.EncounterName = encounterName;

                if (this.SpecDictionary != null &&
                    this.SpecDictionary.ContainsKey(item.SpecID))
                {
                    item.Spec = this.SpecDictionary[item.SpecID].Name;
                }
            }

            this.RankingList.AddRange(targets);
        }
    }
}
