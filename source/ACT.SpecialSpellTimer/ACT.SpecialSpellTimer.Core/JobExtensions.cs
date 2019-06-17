using FFXIV.Framework.XIVHelper;

namespace ACT.SpecialSpellTimer
{
    /// <summary>
    /// ジョブ
    /// </summary>
    public static class JobExtensions
    {
        /// <summary>
        /// 当該ジョブがサモナーか？
        /// </summary>
        /// <returns>bool</returns>
        public static bool IsSummoner(
            this Job job) =>
            job.ID == JobIDs.ACN ||
            job.ID == JobIDs.SMN ||
            job.ID == JobIDs.SCH;
    }
}
