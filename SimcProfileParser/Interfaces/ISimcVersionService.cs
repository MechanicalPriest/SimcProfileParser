using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimcProfileParser.Interfaces
{
    public interface ISimcVersionService
    {
        Task<string> GetGameDataVersionAsync();
    }
}
