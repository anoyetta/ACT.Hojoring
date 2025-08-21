using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant
{
    public enum ObjectType : byte
    {
        Unknown = 0x00,
        PC = 0x01,
        Monster = 0x02,
        NPC = 0x03,
        Aetheryte = 0x05,
        Gathering = 0x06,
        Minion = 0x09
    }

    /// <summary>A byte offset by 0x1980 from the combatant's address that further describes the combatant if their ObjectType is a Monster.</summary>
    // Basically, pets, combat NPCs (ones that attack), and monsters all share ObjectType 2 (Monster), this field distinguishes the monsters from those two.
    public enum MonsterType : byte
    {
        Friendly = 0,
        Hostile = 4
    }

    /// <summary>An unsigned byte offset by 0x94 from the combatant's address that describes its status.</summary>
    public enum ObjectStatus : byte
    {
        /// <summary>Indicates that a targetable PC, NPC, monster, minion, or object is at an effective distance of 97 or less from the player.</summary>
        // A dead PC keeps status 191.
        NormalActorStatus = 191,

        /// <summary>Indicates that a targetable pet, chocobo, or sometimes a monster belonging to another one (add) is in the vicinity.</summary>
        // It hasn't been tested, but it probably works off the 97 effective distance as well.
        NormalSubActorStatus = 190,

        /// <summary>Indicates that a previously targetable actor or sub-actor is now untargetable.</summary>
        // It's the same as log line 34 toggling the nameplate off.
        TemporarilyUntargetable = 189,

        /// <summary>Indicates a dead actor or sub-actor, or one that loads/spawns into the game while being untargetable.</summary>
        // This includes the invisible "helper" boss actors in instances.
        // When an enemy's HP reaches 0, their nameplates grays out and then disappears. The status is only updated to 188 after the nameplate's gone.
        LoadsUntargetable = 188

        /* More statuses do exist, and a lot of them are not fully tested or understood:
         * Status 47 indicates that an actor is updating from status 191 to 175.
         * Status 175 indicates that an actor is at an effective distance of between 98 and 109 (inclusive).
         * When a combatant just loads in and is 98 or more yalms away from the player, the status is set to 171, which could signify that
         * the combatant is pending an update on its status.
         * Watching an unskippable cutscene sets the status to 255. (needs confirmation still though)
         * Watching a skippable cuscene from the inn sets the status to 111. Fun fact: Manually setting the status to 111 creates a clone with a T-pose.
         * And more.
         */
    }

    /// <summary>An int that describes the 3D model visibility.</summary>
    // In a fight like O1N, where the main boss (Alte Roite) momentarily disappears and appears at the edge of the map to do his knockback,
    // the ModelStatus gets set to 16384 (Hidden), but the ObjectStatus remains at 191. The fact that the ObjectStatus doesn't change to 189 could mean that 
    // the boss is still accepting damage in this invisibility period, and that no ghosting will occur. Checking the logs shows that
    // there is indeed no presence of log line 34 that toggles the targetability.
    // Doing Titan (Extreme) confirms this behavior seen in O1N:
    // When Titan's model disappears from the jumps, the field gets set to 16384 (Hidden). When the model reappears, the field gets set back to 0 (Visible).
    // And when Titan's Heart spawns, Titan's ModelStatus remains at 0 (while his ObjectStatus remains at 189).
    // In TEA, all bosses load from the start with ModelStatus 16384 (except for Living Liquid of course), manually changing the values to 0 makes the bosses
    // appear on the map in an immune state, although it's finicky to make that happen consistently.
    public enum ModelStatus : int
    {
        Visible = 0,
        /// <summary>Indicates that the combatant's model has unloaded from the game's memory, be it from a death or a wipe.</summary>
        Unloaded = 2048,
        Hidden = 16384,

        // There are other statuses as well:
        // Manually setting the status 2306 (or 258) hides the model instantly.
        // Manually setting the status to 4 hides the model but still allows you to move.
        // And a few others, but monsters consistently use 0, 2048, and 16384.
    }

    /// <summary>A byte offset by 0x19A0 from the combatant's address that describes the combatant's aggression status.</summary>
    // It's not exactly only about the aggression status, it's that and other things embedded into it:
    // Sheathing the weapon adds 4 to the status.
    // Being mid-cast adds 128 to the status.
    public enum AggressionStatus : byte
    {
        /// <summary>Indicates a combatant that doesn't aggro on sight and is out of combat.</summary>
        Passive = 0,
        /// <summary>Indicates a combatant that aggros on sight and is out of combat.</summary>
        Aggressive = 1,
        /// <summary>Indicates a combatant that doesn't aggro on sight and is in combat.</summary>
        EngagedPassive = 2,
        /// <summary>Indicates a combatant that aggros on sight and is in combat.</summary>
        EngagedAggressive = 3
    }

    [Serializable]
    public class Combatant
    {
        public uint ID;
        public uint OwnerID;
        public ObjectType Type;
        public MonsterType MonsterType;
        public ObjectStatus Status;
        public ModelStatus ModelStatus;
        public AggressionStatus AggressionStatus;
        public uint TargetID;
        public bool IsTargetable;

        public byte Job;
        public string Name;

        public int CurrentHP;
        public int MaxHP;

        public Single PosX;
        public Single PosY;
        public Single PosZ;
        public Single Heading;
        public Single Radius;

        public string Distance;
        public string EffectiveDistance;
        [NonSerialized]
        public byte RawEffectiveDistance;

        public List<EffectEntry> Effects;

        public uint BNpcID;
        public int CurrentMP;
        public int MaxMP;
        public byte Level;

        public uint BNpcNameID;

        public ushort WorldID;
        public ushort CurrentWorldID;
        public uint NPCTargetID;
        public ushort CurrentGP;
        public ushort MaxGP;
        public ushort CurrentCP;
        public ushort MaxCP;
        public uint PCTargetID;
        public byte IsCasting1;
        public byte IsCasting2;
        public uint CastBuffID;
        public uint CastTargetID;
        public float CastGroundTargetX;
        public float CastGroundTargetY;
        public float CastGroundTargetZ;
        public float CastDurationCurrent;
        public float CastDurationMax;

        public short TransformationId;
        public byte WeaponId;

        private Single GetDistance(Combatant target)
        {
            var distanceX = (float)Math.Abs(PosX - target.PosX);
            var distanceY = (float)Math.Abs(PosY - target.PosY);
            var distanceZ = (float)Math.Abs(PosZ - target.PosZ);
            return (Single)Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY) + (distanceZ * distanceZ));
        }

        public string DistanceString(Combatant target)
        {
            return GetDistance(target).ToString("0.00");
        }

        public string EffectiveDistanceString(Combatant target)
        {
            // Mild hack for backwards compat for pre-6.x with no Radius memory location.
            if (target.Radius == 0)
                return this.RawEffectiveDistance.ToString("0.00");
            return (GetDistance(target) - target.Radius).ToString("0.00");
        }
    }

    [Serializable]
    public class EffectEntry
    {
        public ushort BuffID;
        public ushort Stack;
        public float Timer;
        public uint ActorID;
        public bool isOwner;
    }
}
