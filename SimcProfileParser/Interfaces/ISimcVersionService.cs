using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    internal interface ISimcVersionService
    {
        Task<string> GetGameDataVersionAsync();
    }
}
