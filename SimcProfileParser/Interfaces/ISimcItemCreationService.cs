using SimcProfileParser.Model.Generated;
using SimcProfileParser.Model.Profile;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    internal interface ISimcItemCreationService
    {
        /// <param name="parsedItemData">Item from a parsed profile</param>
        /// <param name="playerLevel">Player level to scale gems and enchants to, or 0 for the max player level</param>
        Task<SimcItem> CreateItemAsync(SimcParsedItem parsedItemData, int playerLevel = 0);
        Task<SimcItem> CreateItemAsync(SimcItemOptions itemOptions);
    }
}
