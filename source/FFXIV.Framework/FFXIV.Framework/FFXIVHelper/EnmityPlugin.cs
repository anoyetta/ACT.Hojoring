using System;
using System.Collections.Generic;
using System.Threading;
using FFXIV.Framework.Common;

namespace FFXIV.Framework.FFXIVHelper
{
    public class EnmityEntry
    {
        public uint ID;
        public uint OwnerID;
        public string Name;
        public uint Enmity;
        public bool isMe;
        public int HateRate;
        public byte Job;
        public JobIDs JobID => (JobIDs)Enum.ToObject(typeof(JobIDs), this.Job);
        public string JobName => this.JobID.ToString();
        public string EnmityString => Enmity.ToString("##,#");
        public bool IsPet => (OwnerID != 0);
    }

    public partial class EnmityPlugin :
        IDisposable
    {
        #region Singleton

        private static EnmityPlugin instance;

        public static EnmityPlugin Instance => instance ?? (instance = new EnmityPlugin());

        private EnmityPlugin()
        {
        }

        public static void Free()
        {
            instance?.Dispose();
            instance = null;
        }

        #endregion Singleton

        #region Logger

        private static NLog.Logger AppLogger => AppLog.DefaultLogger;

        #endregion Logger

        private volatile bool isInitialized = false;
        private ThreadWorker scanWorker;

        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;

            lock (this)
            {
                this.scanWorker = ThreadWorker.Run(
                    this.ScanEnmity,
                    100d,
                    "ScanEnmityWorker",
                    ThreadPriority.BelowNormal);
            }
        }

        public void Dispose()
        {
            this.isInitialized = false;

            lock (this)
            {
                this.scanWorker?.Abort();
                this.scanWorker = null;
            }
        }

        private void AttachProcess()
        {
            var process = FFXIVPlugin.Instance?.Process;

            if (this._process == null ||
                this._process.Id != process.Id)
            {
                this.enmityAddress = IntPtr.Zero;

                if (process != null)
                {
                    if (process.ProcessName == "ffxiv")
                    {
                        this._mode = FFXIVClientMode.FFXIV_32;
                        AppLogger.Error("[Enmity] DX9 is not supported.");
                    }
                    else if (process.ProcessName == "ffxiv_dx11")
                    {
                        this._mode = FFXIVClientMode.FFXIV_64;
                    }
                    else
                    {
                        this._mode = FFXIVClientMode.Unknown;
                    }
                }

                this._process = process;
            }

            if (this._process != null &&
                this.enmityAddress == IntPtr.Zero)
            {
                var result = this.GetPointerAddress();
                if (result)
                {
                    AppLogger.Trace("[Enmity] Attached enmity pointer.");
                }
            }
        }

        private List<EnmityEntry> enmityList;

        private void ScanEnmity()
        {
            lock (this)
            {
                this.AttachProcess();

                if (this._process != null &&
                    this.enmityAddress != IntPtr.Zero)
                {
                    this.enmityList = this.GetEnmityEntryList();
                }
                else
                {
                    this.enmityList = null;
                }
            }
        }

        public List<EnmityEntry> EnmityEntryList
        {
            get
            {
                lock (this)
                {
                    return this.enmityList?.Clone();
                }
            }
        }
    }
}
