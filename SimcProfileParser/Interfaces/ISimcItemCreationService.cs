using SimcProfileParser.Model;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcItemCreationService
    {
        SimcItem CreateItem(SimcParsedItem parsedItemData);
    }
}
