using System;

namespace FFXIV.Framework.XIVHelper
{
    public class EnmityEntry
    {
        public uint ID;
        public uint OwnerID;
        public string Name;
        public uint Enmity;
        public bool IsMe;
        public int HateRate;
        public byte Job;

        public JobIDs JobID => (JobIDs)Enum.ToObject(typeof(JobIDs), this.Job);

        public string JobName => this.JobID.ToString();

        public string EnmityString => this.Enmity.ToString("N0");

        public bool IsPet => (this.OwnerID != 0);
    }
}
