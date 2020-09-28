using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Model.Profile
{
    public class SimcParsedLine
    {
        public string RawLine { get; internal set; }
        public string CleanLine { get; internal set; }
        public string Identifier { get; internal set; }
        public string Value { get; internal set; }
        public override string ToString()
        {
            return RawLine;
        }
    }
}
