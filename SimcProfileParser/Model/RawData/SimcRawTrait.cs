using System.Collections.Generic;

namespace SimcProfileParser.Model.RawData
{
    // From trait_data.hpp (trait_data_t)
    class SimcRawTrait
    {
        /// <summary>
        /// Index of the tree. 1 for class, 2 for spec.
        /// </summary>
        public uint TreeIndex { get; set; }
        /// <summary>
        /// ID of the class from the SimcProfileParser.Model.RawData.Class enum
        /// </summary>
        public uint ClassId { get; set; }
        /// <summary>
        /// Unique identifier for the trait
        /// </summary>
        public uint TraitNodeEntryId { get; set; }
        /// <summary>
        /// Unique identifier for this node on the talent tree (shared by choice nodes)
        /// </summary>
        public uint NodeId { get; set; }
        /// <summary>
        /// Maximum potential ranks a trait can have
        /// </summary>
        public uint MaxRanks { get; set; }
        /// <summary>
        /// Points that must be spent in the tree before this can be chosen
        /// </summary>
        public uint RequiredPoints { get; set; }
        /// <summary>
        /// NYI
        /// </summary>
        public uint TraitDefinitionId { get; set; }
        /// <summary>
        /// The SpellId associated with the trait
        /// </summary>
        public uint SpellId { get; set; }
        /// <summary>
        /// NYI
        /// </summary>
        public uint SpellOverrideId { get; set; }
        /// <summary>
        /// Row the trait is located
        /// </summary>
        public int Row { get; set; }
        /// <summary>
        /// Column the trait is located
        /// </summary>
        public int Column { get; set; }
        /// <summary>
        /// Location of the trait in the choice node
        /// </summary>
        public int SelectionIndex { get; set; }
        /// <summary>
        /// Name of the trait
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Spec the trait entry is specific to, 
        /// from the SimcProfileParser.Model.RawData.Specialisation enum
        /// </summary>
        public uint[] SpecId { get; set; }
        /// <summary>
        /// Spec the trait entry is a starter trait for, 
        /// from the SimcProfileParser.Model.RawData.Specialisation enum
        /// </summary>
        public uint[] SpecStarterId { get; set; }

        public SimcRawTrait()
        {

        }
    }
}
