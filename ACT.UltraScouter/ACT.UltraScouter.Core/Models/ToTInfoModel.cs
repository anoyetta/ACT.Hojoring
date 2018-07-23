namespace ACT.UltraScouter.Models
{
    public class ToTInfoModel :
        TargetInfoModel
    {
        #region Singleton

        private static ToTInfoModel instance = new ToTInfoModel();
        public new static ToTInfoModel Instance => instance;

        #endregion Singleton
    }
}
