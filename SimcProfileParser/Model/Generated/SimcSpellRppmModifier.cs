using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace SimcProfileParser.Model.Generated
{
    public class SimcSpellRppmModifier
    {
        public uint SpellId { get; internal set; }
        public bool RppmIsHasted { get; internal set; }
        public bool RppmIsSpecModified { get; internal set; }
        public uint RppmSpec { get; internal set; }
        public double RppmCoefficient { get; internal set; }
    }
}
