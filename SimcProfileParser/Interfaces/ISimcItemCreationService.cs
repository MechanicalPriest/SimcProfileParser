using SimcProfileParser.Model;
using SimcProfileParser.Model.Profile;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcItemCreationService
    {
        SimcItem CreateItem(SimcParsedItem parsedItemData);
    }
}
