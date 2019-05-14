namespace ACT.Hojoring.Updater.Driver
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var checker = new UpdateChecker();

            checker.UsePreRelease = true;
            var version = checker.GetNewerVersion();
        }
    }
}
