using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcVersionService
    {
        Task<string> GetGameDataVersionAsync();
    }
}
