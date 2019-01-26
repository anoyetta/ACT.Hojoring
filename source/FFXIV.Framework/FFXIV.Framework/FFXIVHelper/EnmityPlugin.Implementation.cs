using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FFXIV.Framework.FFXIVHelper
{
    public partial class EnmityPlugin
    {
        public enum FFXIVClientMode
        {
            Unknown = 0,
            FFXIV_32 = 1,
            FFXIV_64 = 2,
        }

        private DummyOverlay _overlay = new DummyOverlay();
        private Process _process;
        private FFXIVClientMode _mode;

        private IntPtr charmapAddress = IntPtr.Zero;
        private IntPtr targetAddress = IntPtr.Zero;
        private IntPtr enmityAddress = IntPtr.Zero;
        private IntPtr aggroAddress = IntPtr.Zero;

        private Combatant GetSelfCombatant()
            => FFXIVPlugin.Instance.GetPlayer();

        private List<Combatant> Combatants
            => FFXIVPlugin.Instance.GetCombatantList() as List<Combatant>;

        private unsafe List<EnmityEntry> GetEnmityEntryList()
        {
            short num = 0;
            uint topEnmity = 0;
            List<EnmityEntry> result = new List<EnmityEntry>();
            List<Combatant> combatantList = Combatants;
            Combatant mychar = GetSelfCombatant();

            /// 一度に全部読む
            byte[] buffer = GetByteArray(enmityAddress, 0x900 + 2);
            fixed (byte* p = buffer) num = (short)p[2296];

            if (num <= 0)
            {
                return result;
            }
            if (num > 31) num = 31; // changed??? 32->31

            for (short i = 0; i < num; i++)
            {
                int p = i * 72;
                uint _id;
                uint _enmity;

                fixed (byte* bp = buffer)
                {
                    _id = *(uint*)&bp[p + 56];
                    _enmity = *(uint*)&bp[p + 60];
                }
                var entry = new EnmityEntry()
                {
                    ID = _id,
                    Enmity = _enmity,
                    isMe = false,
                    Name = "Unknown",
                    Job = 0x00
                };
                if (entry.ID > 0)
                {
                    Combatant c = combatantList.Find(x => x.ID == entry.ID);
                    if (c != null)
                    {
                        entry.Name = c.Name;
                        entry.Job = c.Job;
                        entry.OwnerID = c.OwnerID;
                    }
                    if (entry.ID == mychar.ID)
                    {
                        entry.isMe = true;
                    }
                    if (topEnmity <= entry.Enmity)
                    {
                        topEnmity = entry.Enmity;
                    }
                    entry.HateRate = (int)(((double)entry.Enmity / (double)topEnmity) * 100);
                    result.Add(entry);
                }
            }
            return result;
        }

        private const string charmapSignature = "488b420848c1e8033da701000077248bc0488d0d";
        private const string targetSignature = "41bc000000e041bd01000000493bc47555488d0d";
        private const string enmitySignature = "83f9ff7412448b048e8bd3488d0d";
        private const int charmapOffset = 0;
        private const int targetOffset = 192;
        private const int enmityOffset = -4648;

        /// <summary>
        /// 各ポインタのアドレスを取得
        /// </summary>
        private bool GetPointerAddress()
        {
            bool success = true;
            bool bRIP = true;

            List<string> fail = new List<string>();

            /// CHARMAP
            List<IntPtr> list = SigScan(charmapSignature, 0, bRIP);
            if (list != null && list.Count == 1)
            {
                charmapAddress = list[0] + charmapOffset;
            }
            else
            {
                charmapAddress = IntPtr.Zero;
                fail.Add(nameof(charmapAddress));
                success = false;
            }

            // ENMITY
            list = SigScan(enmitySignature, 0, bRIP);
            if (list != null && list.Count == 1)
            {
                enmityAddress = list[0] + enmityOffset;
                aggroAddress = IntPtr.Add(enmityAddress, 2312); // Offset 0x900 + 8
            }
            else
            {
                enmityAddress = IntPtr.Zero;
                aggroAddress = IntPtr.Zero;
                fail.Add(nameof(enmityAddress));
                fail.Add(nameof(aggroAddress));
                success = false;
            }

            if (!success)
            {
                AppLogger.Error("[Enmity] Failed to memory scan.");
            }

            return success;
        }

        private List<IntPtr> SigScan(string pattern, int offset = 0, bool bRIP = false)
        {
            if (pattern == null || pattern.Length % 2 != 0)
            {
                return new List<IntPtr>();
            }

            // 1byte = 2char
            byte?[] patternByteArray = new byte?[pattern.Length / 2];

            // Convert Pattern to "Array of Byte"
            for (int i = 0; i < pattern.Length / 2; i++)
            {
                string text = pattern.Substring(i * 2, 2);
                if (text == "??")
                {
                    patternByteArray[i] = null;
                }
                else
                {
                    patternByteArray[i] = new byte?(Convert.ToByte(text, 16));
                }
            }

            int moduleMemorySize = _process.MainModule.ModuleMemorySize;
            IntPtr baseAddress = _process.MainModule.BaseAddress;
            IntPtr intPtr_EndOfModuleMemory = IntPtr.Add(baseAddress, moduleMemorySize);
            IntPtr intPtr_Scannning = baseAddress;

            int splitSizeOfMemory = 65536;
            byte[] splitMemoryArray = new byte[splitSizeOfMemory];

            List<IntPtr> list = new List<IntPtr>();

            // while loop for scan all memory
            while (intPtr_Scannning.ToInt64() < intPtr_EndOfModuleMemory.ToInt64())
            {
                IntPtr nSize = new IntPtr(splitSizeOfMemory);

                // if remaining memory size is less than splitSize, change nSize to remaining size
                if (IntPtr.Add(intPtr_Scannning, splitSizeOfMemory).ToInt64() > intPtr_EndOfModuleMemory.ToInt64())
                {
                    nSize = (IntPtr)(intPtr_EndOfModuleMemory.ToInt64() - intPtr_Scannning.ToInt64());
                }

                IntPtr intPtr_NumberOfBytesRead = IntPtr.Zero;

                // read memory
                if (NativeMethods.ReadProcessMemory(_process.Handle, intPtr_Scannning, splitMemoryArray, nSize, ref intPtr_NumberOfBytesRead))
                {
                    int num = 0;

                    // slide start point byte bu byte, check with patternByteArray
                    while ((long)num < intPtr_NumberOfBytesRead.ToInt64() - (long)patternByteArray.Length - (long)offset)
                    {
                        int matchCount = 0;
                        for (int j = 0; j < patternByteArray.Length; j++)
                        {
                            // pattern "??" have a null value. in this case, skip the check.
                            if (!patternByteArray[j].HasValue)
                            {
                                matchCount++;
                            }
                            else
                            {
                                if (patternByteArray[j].Value != splitMemoryArray[num + j])
                                {
                                    break;
                                }
                                matchCount++;
                            }
                        }

                        // if all bytes are match, it means "the pattern found"
                        if (matchCount == patternByteArray.Length)
                        {
                            IntPtr item;
                            if (bRIP)
                            {
                                item = new IntPtr(BitConverter.ToInt32(splitMemoryArray, num + patternByteArray.Length + offset));
                                item = new IntPtr(intPtr_Scannning.ToInt64() + (long)num + (long)patternByteArray.Length + 4L + item.ToInt64());
                            }
                            else if (_mode == FFXIVClientMode.FFXIV_64)
                            {
                                item = new IntPtr(BitConverter.ToInt64(splitMemoryArray, num + patternByteArray.Length + offset));
                                item = new IntPtr(item.ToInt64());
                            }
                            else
                            {
                                item = new IntPtr(BitConverter.ToInt32(splitMemoryArray, num + patternByteArray.Length + offset));
                                item = new IntPtr(item.ToInt64());
                            }

                            // add the item if not contains already
                            if (item != IntPtr.Zero && !list.Contains(item))
                            {
                                list.Add(item);
                            }
                        }
                        num++;
                    }
                }
                intPtr_Scannning = IntPtr.Add(intPtr_Scannning, splitSizeOfMemory - patternByteArray.Length - offset);
            }
            return list;
        }

        private byte[] GetByteArray(IntPtr address, int length)
        {
            var data = new byte[length];
            Peek(address, data);
            return data;
        }

        private bool Peek(IntPtr address, byte[] buffer)
        {
            IntPtr zero = IntPtr.Zero;
            IntPtr nSize = new IntPtr(buffer.Length);
            return NativeMethods.ReadProcessMemory(_process.Handle, address, buffer, nSize, ref zero);
        }

        private static class NativeMethods
        {
            // ReadProcessMemory
            [DllImport("kernel32.dll")]
            public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize, ref IntPtr lpNumberOfBytesRead);
        }

        private class DummyOverlay
        {
            public void LogDebug(
                string format,
                params object[] args)
            {
                Debug.WriteLine(format, args);
            }
        }
    }
}
