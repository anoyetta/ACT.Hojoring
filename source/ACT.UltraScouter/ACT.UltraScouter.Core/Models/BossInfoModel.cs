namespace ACT.UltraScouter.Models
{
    public class BossInfoModel :
        TargetInfoModel
    {
        #region Singleton

        private static BossInfoModel instance = new BossInfoModel();
        public new static BossInfoModel Instance => instance;

        #endregion Singleton
    }
}
