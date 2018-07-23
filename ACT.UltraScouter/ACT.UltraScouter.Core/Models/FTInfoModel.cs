namespace ACT.UltraScouter.Models
{
    public class FTInfoModel :
        TargetInfoModel
    {
        #region Singleton

        private static FTInfoModel instance = new FTInfoModel();
        public new static FTInfoModel Instance => instance;

        #endregion Singleton
    }
}
