using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Core.Enums;
using Sharlayan.Models;
using Sharlayan.Models.ReadResults;
using Sharlayan.Models.Structures;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ActorItem = Sharlayan.Core.ActorItem;
using EnmityItem = Sharlayan.Core.EnmityItem;

namespace FFXIV.Framework.XIVHelper
{
    public static class ReaderEx
    {
        public static ActorItem CurrentPlayer { get; private set; }

        public static CombatantEx CurrentPlayerCombatant { get; private set; }

        public static ProcessModel ProcessModel { get; private set; }

        public static Func<uint, ActorItem> GetActorCallback { get; set; }

        public static Func<uint, ActorItem> GetNPCActorCallback { get; set; }

        private static Func<StructuresContainer> getStructuresDelegate;

        private static Func<StructuresContainer> GetStructuresDelegate => getStructuresDelegate ??= CreateGetStructuresDelegate();

        private static Func<StructuresContainer> CreateGetStructuresDelegate()
        {
            var property = MemoryHandler.Instance.GetType().GetProperty(
                "Structures",
                BindingFlags.NonPublic | BindingFlags.Instance);

            return (Func<StructuresContainer>)Delegate.CreateDelegate(
                typeof(Func<StructuresContainer>),
                MemoryHandler.Instance,
                property.GetMethod);
        }

#if false
        private static readonly Lazy<Func<byte[], bool, ActorItem, ActorItem>> LazyResolveActorFromBytesDelegate = new Lazy<Func<byte[], bool, ActorItem, ActorItem>>(() =>
        {
            var asm = MemoryHandler.Instance.GetType().Assembly;
            var type = asm.GetType("Sharlayan.Utilities.ActorItemResolver");

            var method = type.GetMethod(
                "ResolveActorFromBytes",
                BindingFlags.Static | BindingFlags.Public);

            return (Func<byte[], bool, ActorItem, ActorItem>)Delegate.CreateDelegate(
                typeof(Func<byte[], bool, ActorItem, ActorItem>),
                null,
                method);
        });

        private static ActorItem InvokeActorItemResolver(
            byte[] source,
            bool isCurrentUser = false,
            ActorItem entry = null)
            => LazyResolveActorFromBytesDelegate.Value.Invoke(
                source,
                isCurrentUser,
                entry);

        private static readonly Lazy<Func<byte[], ActorItem, PartyMember>> LazyResolvePartyMemberFromBytesDelegate = new Lazy<Func<byte[], ActorItem, PartyMember>>(() =>
        {
            var asm = MemoryHandler.Instance.GetType().Assembly;
            var type = asm.GetType("Sharlayan.Utilities.PartyMemberResolver");

            var method = type.GetMethod(
                "ResolvePartyMemberFromBytes",
                BindingFlags.Static | BindingFlags.Public);

            return (Func<byte[], ActorItem, PartyMember>)Delegate.CreateDelegate(
                typeof(Func<byte[], ActorItem, PartyMember>),
                null,
                method);
        });

        private static PartyMember InvokePartyMemberResolver(
            byte[] source,
            ActorItem entry = null)
            => LazyResolvePartyMemberFromBytesDelegate.Value.Invoke(
                source,
                entry);
#endif

        public static void SetProcessModel(
            ProcessModel model)
        {
            ProcessModel = model;
            getStructuresDelegate = null;
        }

        public static List<ActorItem> GetActorSimple(
            bool isScanNPC = false,
            bool isPlayerOnly = false)
        {
            var result = new List<ActorItem>(256);

            if (!Reader.CanGetActors() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            var isWin64 = ProcessModel?.IsWin64 ?? true;

            var targetAddress = IntPtr.Zero;
            var endianSize = isWin64 ? 8 : 4;

            var structures = GetStructuresDelegate.Invoke();
            var sourceSize = structures.ActorItem.SourceSize;
            var limit = structures.ActorItem.EntityCount;
            var characterAddressMap = MemoryHandler.Instance.GetByteArray(Scanner.Instance.Locations[Signatures.CharacterMapKey], endianSize * limit);
            var uniqueAddresses = new Dictionary<IntPtr, IntPtr>();
            var firstAddress = IntPtr.Zero;

            var firstTime = true;
            for (var i = 0; i < limit; i++)
            {
                Thread.Yield();

                var characterAddress = isWin64 ?
                    new IntPtr(BitConverter.TryToInt64(characterAddressMap, i * endianSize)) :
                    new IntPtr(BitConverter.TryToInt32(characterAddressMap, i * endianSize));

                if (characterAddress == IntPtr.Zero)
                {
                    continue;
                }

                if (firstTime)
                {
                    firstAddress = characterAddress;
                    firstTime = false;
                }

                uniqueAddresses[characterAddress] = characterAddress;
            }

            foreach (var kvp in uniqueAddresses)
            {
                Thread.Yield();

                var characterAddress = new IntPtr(kvp.Value.ToInt64());
                var source = MemoryHandler.Instance.GetByteArray(characterAddress, sourceSize);

                var ID = BitConverter.TryToUInt32(source, structures.ActorItem.ID);
                var NPCID2 = BitConverter.TryToUInt32(source, structures.ActorItem.NPCID2);
                var Type = (Actor.Type)source[structures.ActorItem.Type];

                var existing = default(ActorItem);
                switch (Type)
                {
                    case Actor.Type.Aetheryte:
                    case Actor.Type.EventObject:
                    case Actor.Type.NPC:
                        if (!isScanNPC)
                        {
                            continue;
                        }

                        existing = GetNPCActorCallback?.Invoke(NPCID2);
                        break;

                    default:
                        existing = GetActorCallback?.Invoke(ID);
                        break;
                }

                var isFirstEntry = kvp.Value.ToInt64() == firstAddress.ToInt64();
                var entry = ResolveActorFromBytes(structures, source, isFirstEntry, existing);

                if (isFirstEntry)
                {
                    CurrentPlayer = entry;
                    CurrentPlayerCombatant = SharlayanHelper.Instance.ToCombatant(entry);

                    if (targetAddress.ToInt64() > 0)
                    {
                        var targetInfoSource = MemoryHandler.Instance.GetByteArray(targetAddress, 128);
                        entry.TargetID = (int)BitConverter.TryToInt32(targetInfoSource, structures.ActorItem.ID);
                    }

                    if (isPlayerOnly)
                    {
                        result.Add(entry);
                        break;
                    }
                }

                result.Add(entry);
            }

            return result;
        }

        public static uint[] GetPartyMemberIDs()
        {
            var result = new List<uint>(8);

            if (!Reader.CanGetPartyMembers() || !MemoryHandler.Instance.IsAttached)
            {
                return result.ToArray();
            }

            var structures = GetStructuresDelegate.Invoke();

            var PartyInfoMap = (IntPtr)Scanner.Instance.Locations[Signatures.PartyMapKey];
            var PartyCountMap = Scanner.Instance.Locations[Signatures.PartyCountKey];

            var partyCount = MemoryHandler.Instance.GetByte(PartyCountMap);
            var sourceSize = structures.PartyMember.SourceSize;

            if (partyCount > 1 && partyCount < 9)
            {
                for (uint i = 0; i < partyCount; i++)
                {
                    var address = PartyInfoMap.ToInt64() + i * (uint)sourceSize;
                    var source = MemoryHandler.Instance.GetByteArray(new IntPtr(address), sourceSize);
                    var ID = BitConverter.TryToUInt32(source, structures.PartyMember.ID);

                    result.Add(ID);
                    Thread.Yield();
                }
            }

            return result.ToArray();
        }

#if false
        public static List<PartyMember> GetPartyMemberSimple()
        {
            var result = new List<PartyMember>(8);

            if (!Reader.CanGetPartyMembers() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            var structures = LazyGetStructuresDelegate.Value.Invoke();

            var PartyInfoMap = (IntPtr)Scanner.Instance.Locations[Signatures.PartyMapKey];
            var PartyCountMap = Scanner.Instance.Locations[Signatures.PartyCountKey];

            var partyCount = MemoryHandler.Instance.GetByte(PartyCountMap);
            var sourceSize = structures.PartyMember.SourceSize;

            if (partyCount > 1 && partyCount < 9)
            {
                for (uint i = 0; i < partyCount; i++)
                {
                    var address = PartyInfoMap.ToInt64() + i * (uint)sourceSize;
                    var source = MemoryHandler.Instance.GetByteArray(new IntPtr(address), sourceSize);

                    var ID = BitConverter.TryToUInt32(source, structures.ActorItem.ID);
                    var NPCID2 = BitConverter.TryToUInt32(source, structures.ActorItem.NPCID2);
                    var Type = (Actor.Type)source[structures.ActorItem.Type];

                    var existing = default(ActorItem);
                    switch (Type)
                    {
                        case Actor.Type.Aetheryte:
                        case Actor.Type.EventObject:
                        case Actor.Type.NPC:
                            existing = GetNPCActorCallback?.Invoke(NPCID2);
                            break;

                        default:
                            existing = GetActorCallback?.Invoke(ID);
                            break;
                    }

                    var entry = InvokePartyMemberResolver(source, existing);

                    result.Add(entry);
                    Thread.Yield();
                }
            }

            if (partyCount <= 1)
            {
                if (CurrentPlayer != null)
                {
                    result.Add(InvokePartyMemberResolver(Array.Empty<byte>(), CurrentPlayer));
                }
            }

            return result;
        }
#endif

        public static ActorItem ResolveActorFromBytes(StructuresContainer structures, byte[] source, bool isCurrentUser = false, ActorItem entry = null)
        {
            entry = entry ?? new ActorItem();

            var defaultBaseOffset = structures.ActorItem.DefaultBaseOffset;
            var defaultStatOffset = structures.ActorItem.DefaultStatOffset;
            var defaultStatusEffectOffset = structures.ActorItem.DefaultStatusEffectOffset;

            entry.MapTerritory = 0;
            entry.MapIndex = 0;
            entry.MapID = 0;
            entry.TargetID = 0;
            entry.Name = MemoryHandler.Instance.GetStringFromBytes(source, structures.ActorItem.Name);
            entry.ID = BitConverter.TryToUInt32(source, structures.ActorItem.ID);
            entry.UUID = string.IsNullOrEmpty(entry.UUID)
                             ? Guid.NewGuid().ToString()
                             : entry.UUID;
            entry.NPCID1 = BitConverter.TryToUInt32(source, structures.ActorItem.NPCID1);
            entry.NPCID2 = BitConverter.TryToUInt32(source, structures.ActorItem.NPCID2);
            entry.OwnerID = BitConverter.TryToUInt32(source, structures.ActorItem.OwnerID);
            entry.TypeID = source[structures.ActorItem.Type];
            entry.Type = (Actor.Type)entry.TypeID;

            entry.TargetTypeID = source[structures.ActorItem.TargetType];
            entry.TargetType = (Actor.TargetType)entry.TargetTypeID;

            entry.GatheringStatus = source[structures.ActorItem.GatheringStatus];
            entry.Distance = source[structures.ActorItem.Distance];

            entry.X = BitConverter.TryToSingle(source, structures.ActorItem.X + defaultBaseOffset);
            entry.Z = BitConverter.TryToSingle(source, structures.ActorItem.Z + defaultBaseOffset);
            entry.Y = BitConverter.TryToSingle(source, structures.ActorItem.Y + defaultBaseOffset);
            entry.Heading = BitConverter.TryToSingle(source, structures.ActorItem.Heading + defaultBaseOffset);
            entry.HitBoxRadius = BitConverter.TryToSingle(source, structures.ActorItem.HitBoxRadius + defaultBaseOffset);
            entry.Fate = BitConverter.TryToUInt32(source, structures.ActorItem.Fate + defaultBaseOffset); // ??
            entry.TargetFlags = source[structures.ActorItem.TargetFlags]; // ??
            entry.GatheringInvisible = source[structures.ActorItem.GatheringInvisible]; // ??
            entry.ModelID = BitConverter.TryToUInt32(source, structures.ActorItem.ModelID);
            entry.ActionStatusID = source[structures.ActorItem.ActionStatus];
            entry.ActionStatus = (Actor.ActionStatus)entry.ActionStatusID;

            // 0x17D - 0 = Green name, 4 = non-agro (yellow name)
            entry.IsGM = BitConverter.TryToBoolean(source, structures.ActorItem.IsGM); // ?
            entry.IconID = source[structures.ActorItem.Icon];
            entry.Icon = (Actor.Icon)entry.IconID;

            entry.StatusID = source[structures.ActorItem.Status];
            entry.Status = (Actor.Status)entry.StatusID;

            entry.ClaimedByID = BitConverter.TryToUInt32(source, structures.ActorItem.ClaimedByID);
            var targetID = BitConverter.TryToUInt32(source, structures.ActorItem.TargetID);
            var pcTargetID = targetID;

            entry.JobID = source[structures.ActorItem.Job + defaultStatOffset];
            entry.Job = (Actor.Job)entry.JobID;

            entry.Level = source[structures.ActorItem.Level + defaultStatOffset];
            entry.GrandCompany = source[structures.ActorItem.GrandCompany + defaultStatOffset];
            entry.GrandCompanyRank = source[structures.ActorItem.GrandCompanyRank + defaultStatOffset];
            entry.Title = source[structures.ActorItem.Title + defaultStatOffset];
            entry.HPCurrent = BitConverter.TryToInt32(source, structures.ActorItem.HPCurrent + defaultStatOffset);
            entry.HPMax = BitConverter.TryToInt32(source, structures.ActorItem.HPMax + defaultStatOffset);
            entry.MPCurrent = BitConverter.TryToInt32(source, structures.ActorItem.MPCurrent + defaultStatOffset);
            entry.MPMax = BitConverter.TryToInt32(source, structures.ActorItem.MPMax + defaultStatOffset);
            entry.TPCurrent = BitConverter.TryToInt16(source, structures.ActorItem.TPCurrent + defaultStatOffset);
            entry.TPMax = 1000;
            entry.GPCurrent = BitConverter.TryToInt16(source, structures.ActorItem.GPCurrent + defaultStatOffset);
            entry.GPMax = BitConverter.TryToInt16(source, structures.ActorItem.GPMax + defaultStatOffset);
            entry.CPCurrent = BitConverter.TryToInt16(source, structures.ActorItem.CPCurrent + defaultStatOffset);
            entry.CPMax = BitConverter.TryToInt16(source, structures.ActorItem.CPMax + defaultStatOffset);

            // entry.Race = source[0x2578]; // ??
            // entry.Sex = (Actor.Sex) source[0x2579]; //?
            entry.AgroFlags = source[structures.ActorItem.AgroFlags];
            entry.CombatFlags = source[structures.ActorItem.CombatFlags];
            entry.DifficultyRank = source[structures.ActorItem.DifficultyRank];
            entry.CastingID = BitConverter.TryToInt16(source, structures.ActorItem.CastingID); // 0x2C94);
            entry.CastingTargetID = BitConverter.TryToUInt32(source, structures.ActorItem.CastingTargetID); // 0x2CA0);
            entry.CastingProgress = BitConverter.TryToSingle(source, structures.ActorItem.CastingProgress); // 0x2CC4);
            entry.CastingTime = BitConverter.TryToSingle(source, structures.ActorItem.CastingTime); // 0x2DA8);
            entry.Coordinate = new Coordinate(entry.X, entry.Z, entry.Y);
            if (targetID > 0)
            {
                entry.TargetID = (int)targetID;
            }
            else
            {
                if (pcTargetID > 0)
                {
                    entry.TargetID = (int)pcTargetID;
                }
            }

            if (entry.CastingTargetID == 3758096384)
            {
                entry.CastingTargetID = 0;
            }

            entry.MapIndex = 0;

            // handle empty names
            if (string.IsNullOrEmpty(entry.Name))
            {
                if (entry.Type == Actor.Type.EventObject)
                {
                    entry.Name = $"{nameof(entry.EventObjectTypeID)}: {entry.EventObjectTypeID}";
                }
                else
                {
                    entry.Name = $"{nameof(entry.TypeID)}: {entry.TypeID}";
                }
            }

            CleanXPValue(ref entry);

            return entry;
        }

        private static void CleanXPValue(ref ActorItem entity)
        {
            if (entity.HPCurrent < 0 || entity.HPMax < 0)
            {
                entity.HPCurrent = 1;
                entity.HPMax = 1;
            }

            if (entity.HPCurrent > entity.HPMax)
            {
                if (entity.HPMax == 0)
                {
                    entity.HPCurrent = 1;
                    entity.HPMax = 1;
                }
                else
                {
                    entity.HPCurrent = entity.HPMax;
                }
            }

            if (entity.MPCurrent < 0 || entity.MPMax < 0)
            {
                entity.MPCurrent = 1;
                entity.MPMax = 1;
            }

            if (entity.MPCurrent > entity.MPMax)
            {
                if (entity.MPMax == 0)
                {
                    entity.MPCurrent = 1;
                    entity.MPMax = 1;
                }
                else
                {
                    entity.MPCurrent = entity.MPMax;
                }
            }

            if (entity.GPCurrent < 0 || entity.GPMax < 0)
            {
                entity.GPCurrent = 1;
                entity.GPMax = 1;
            }

            if (entity.GPCurrent > entity.GPMax)
            {
                if (entity.GPMax == 0)
                {
                    entity.GPCurrent = 1;
                    entity.GPMax = 1;
                }
                else
                {
                    entity.GPCurrent = entity.GPMax;
                }
            }

            if (entity.CPCurrent < 0 || entity.CPMax < 0)
            {
                entity.CPCurrent = 1;
                entity.CPMax = 1;
            }

            if (entity.CPCurrent > entity.CPMax)
            {
                if (entity.CPMax == 0)
                {
                    entity.CPCurrent = 1;
                    entity.CPMax = 1;
                }
                else
                {
                    entity.CPCurrent = entity.CPMax;
                }
            }
        }

        public static TargetResult GetTargetInfoSimple(
            bool isSkipEnmity = false)
        {
            var result = new TargetResult();

            if (!Reader.CanGetTargetInfo() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            var structures = GetStructuresDelegate.Invoke();
            var targetAddress = (IntPtr)Scanner.Instance.Locations[Signatures.TargetKey];

            if (targetAddress.ToInt64() > 0)
            {
                byte[] targetInfoSource = MemoryHandler.Instance.GetByteArray(targetAddress, structures.TargetInfo.SourceSize);

                var currentTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, structures.TargetInfo.Current);
                var mouseOverTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, structures.TargetInfo.MouseOver);
                var focusTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, structures.TargetInfo.Focus);
                var previousTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, structures.TargetInfo.Previous);

                var currentTargetID = BitConverter.TryToUInt32(targetInfoSource, structures.TargetInfo.CurrentID);

                if (currentTarget > 0)
                {
                    ActorItem entry = GetTargetActorItemFromSource(structures, currentTarget);
                    currentTargetID = entry.ID;
                    if (entry.IsValid)
                    {
                        result.TargetsFound = true;
                        result.TargetInfo.CurrentTarget = entry;
                    }
                }

                if (mouseOverTarget > 0)
                {
                    ActorItem entry = GetTargetActorItemFromSource(structures, mouseOverTarget);
                    if (entry.IsValid)
                    {
                        result.TargetsFound = true;
                        result.TargetInfo.MouseOverTarget = entry;
                    }
                }

                if (focusTarget > 0)
                {
                    ActorItem entry = GetTargetActorItemFromSource(structures, focusTarget);
                    if (entry.IsValid)
                    {
                        result.TargetsFound = true;
                        result.TargetInfo.FocusTarget = entry;
                    }
                }

                if (previousTarget > 0)
                {
                    ActorItem entry = GetTargetActorItemFromSource(structures, previousTarget);
                    if (entry.IsValid)
                    {
                        result.TargetsFound = true;
                        result.TargetInfo.PreviousTarget = entry;
                    }
                }

                if (currentTargetID > 0)
                {
                    result.TargetsFound = true;
                    result.TargetInfo.CurrentTargetID = currentTargetID;
                }
            }

            if (isSkipEnmity)
            {
                return result;
            }

            if (result.TargetInfo.CurrentTargetID > 0)
            {
                if (Reader.CanGetEnmityEntities())
                {
                    const int EnmityLimit = 16;

                    var enmityCount = MemoryHandler.Instance.GetInt16(Scanner.Instance.Locations[Signatures.EnmityCountKey]);
                    var enmityStructure = (IntPtr)Scanner.Instance.Locations[Signatures.EnmityMapKey];

                    if (enmityCount > EnmityLimit)
                    {
                        enmityCount = EnmityLimit;
                    }

                    if (enmityCount > 0 && enmityStructure.ToInt64() > 0)
                    {
                        var enmitySourceSize = structures.EnmityItem.SourceSize;
                        for (uint i = 0; i < enmityCount; i++)
                        {
                            Thread.Yield();

                            var address = new IntPtr(enmityStructure.ToInt64() + (i * enmitySourceSize));
                            var enmityEntry = new EnmityItem
                            {
                                ID = (uint)MemoryHandler.Instance.GetPlatformInt(address, structures.EnmityItem.ID),
                                Name = MemoryHandler.Instance.GetString(address + structures.EnmityItem.Name),
                                Enmity = MemoryHandler.Instance.GetUInt32(address + structures.EnmityItem.Enmity)
                            };

                            if (enmityEntry.ID <= 0)
                            {
                                continue;
                            }

                            result.TargetInfo.EnmityItems.Add(enmityEntry);
                        }
                    }
                }
            }

            return result;
        }

        private static ActorItem GetTargetActorItemFromSource(StructuresContainer structures, long address)
        {
            var targetAddress = new IntPtr(address);

            byte[] source = MemoryHandler.Instance.GetByteArray(targetAddress, structures.TargetInfo.Size);
            ActorItem entry = ResolveActorFromBytes(structures, source);

            return entry;
        }
    }

    internal static class BitConverter
    {
        public static bool TryToBoolean(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToBoolean(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static char TryToChar(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToChar(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static double TryToDouble(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToDouble(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static long TryToDoubleToInt64Bits(double value)
        {
            try
            {
                return System.BitConverter.DoubleToInt64Bits(value);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static short TryToInt16(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToInt16(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static int TryToInt32(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToInt32(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static long TryToInt64(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToInt64(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static double TryToInt64BitsToDouble(long value)
        {
            try
            {
                return System.BitConverter.Int64BitsToDouble(value);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static float TryToSingle(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToSingle(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static string TryToString(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToString(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static ushort TryToUInt16(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToUInt16(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static uint TryToUInt32(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToUInt32(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static ulong TryToUInt64(byte[] value, int index)
        {
            try
            {
                return System.BitConverter.ToUInt64(value, index);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
