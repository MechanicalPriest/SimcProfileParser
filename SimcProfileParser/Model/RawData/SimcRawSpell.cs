using System.Collections.Generic;

namespace SimcProfileParser.Model.RawData
{
    /// <summary>
    /// 
    /// </summary>
    class SimcRawSpell
    {
        public string Name { get; set; }
        public uint Id { get; set; }
        public uint School { get; set; }
        public double ProjectileSpeed { get; set; }
        public double ProjectileDelay { get; set; }
        /// <summary>
        /// minimum duration for the projectile
        /// </summary>
        public double MinimumTravelTime { get; set; }
        /// <summary>
        /// Racial mask for the spell
        /// </summary>
        public ulong RaceMask { get; set; }
        /// <summary>
        /// Class mask for spell
        /// </summary>
        public uint ClassMask { get; set; }
        /// <summary>
        /// NO LONGER IN USE?
        /// </summary>
        //public int ScalingType { get; set; }
        /// <summary>
        /// Max scaling level(?), 0 == no restrictions, otherwise min( player_level, max_scaling_level )
        /// </summary>
        public int MaxScalingLevel { get; set; }

        public uint SpellLevel { get; set; }
        public uint MaxLevel { get; set; }
        public uint RequireMaxLevel { get; set; }

        public double MinRange { get; set; }
        public double MaxRange { get; set; }

        public uint Cooldown { get; set; }
        public uint Gcd { get; set; }
        public uint CategoryCooldown { get; set; }

        public uint Charges { get; set; }
        public uint ChargeCooldown { get; set; }

        public uint Category { get; set; }
        public uint DamageClass { get; set; }
        public int MaxTargets { get; set; }

        public double Duration { get; set; }

        public uint MaxStack { get; set; }
        public uint ProcChance { get; set; }
        public int ProcCharges { get; set; }
        public ulong ProcFlags { get; set; }
        public uint InternalCooldown { get; set; }
        public double Rppm { get; set; }

        public uint EquippedClass { get; set; }
        public uint EquippedInventoryTypeMask { get; set; }
        public uint EquippedSubclassMask { get; set; }

        public int CastTime { get; set; }

        /// <summary>
        /// Skip this one maybe? It's the SpellMisc.dbc "flags"
        /// </summary>
        public uint[] Attributes { get; set; }
        /// <summary>
        /// Skip as well, another flags value
        /// </summary>
        public uint[] ClassFlags { get; set; }
        public uint ClassFlagsFamily { get; set; }

        public uint StanceMask { get; set; }

        public uint Mechanic { get; set; }

        /// <summary>
        /// Azerite Power ID
        /// </summary>
        public uint PowerId { get; set; }
        /// <summary>
        /// Essence ID
        /// </summary>
        public uint EssenceId { get; set; }
        public List<SimcRawSpellEffect> Effects { get; set; }
        public List<SimcRawSpellPower> SpellPowers { get; set; }

        public SimcRawSpell()
        {
            Effects = new List<SimcRawSpellEffect>();
            SpellPowers = new List<SimcRawSpellPower>();
        }
    }
}
