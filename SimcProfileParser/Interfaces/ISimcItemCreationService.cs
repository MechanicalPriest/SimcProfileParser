using SimcProfileParser.Model;
using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcItemCreationService
    {
        Task<SimcItem> CreateItemAsync(SimcParsedItem parsedItemData);
        Task<SimcItem> CreateItemAsync(SimcItemOptions itemOptions);
    }
}
