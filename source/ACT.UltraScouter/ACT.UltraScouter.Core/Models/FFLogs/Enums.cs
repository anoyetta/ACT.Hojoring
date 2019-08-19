using System.ComponentModel.DataAnnotations;

namespace ACT.UltraScouter.Models.FFLogs
{
    public enum FFLogsRegions
    {
        JP = 0,
        NA,
        EU,
        CN,
        KR
    }

    public enum FFLogsPartitions
    {
        [Display(Name = "Current")]
        Current = 0,

        [Display(Name = "Standard")]
        Standard = 1,

        [Display(Name = "Non-Standard")]
        NonStandard = 2,

        [Display(Name = "Standard (Echo)")]
        StandardEcho = 7,

        [Display(Name = "Non-Standard (Echo)")]
        NonStandardEcho = 8,
    }

    public enum FFLogsDifficulty
    {
        Normal = 100,
        Savage = 101,
    }
}
