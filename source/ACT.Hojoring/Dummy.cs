using ACT.XIVLog;

namespace ACT.Hojoring
{
    /// <summary>
    /// 依存関係を発生させるためのダミークラス
    /// </summary>
    public class Dummy
    {
        public static void Execute()
        {
            new XIVLogPlugin().InitPlugin(null, null);
        }
    }
}
