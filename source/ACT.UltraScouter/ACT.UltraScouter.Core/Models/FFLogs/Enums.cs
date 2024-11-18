using System.ComponentModel.DataAnnotations;

namespace ACT.UltraScouter.Models.FFLogs
{
    public enum FFLogsRegions
    {
        JP = 0,
        NA,
        EU,
        CN,
        KR,
        OC
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

    public enum FFLogsMetric
    {
        [Display(Name = "rDPS")]
        rdps = 0,
        [Display(Name = "aDPS")]
        adps = 1,
        [Display(Name = "nDPS")]
        ndps = 2,
        [Display(Name = "cDPS")]
        cdps = 3,
    }
}
