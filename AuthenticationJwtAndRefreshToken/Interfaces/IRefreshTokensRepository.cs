using System.Collections.Generic;
using System.Threading.Tasks;
using MorCohen.Models.AuthenticateModels;

namespace MorCohen.Interfaces
{
    public interface IRefreshTokensRepository
    {
        RefreshToken GetByTokenValue(string token, bool includeApplicationUsers = false);

        List<RefreshToken> GetByApplicationUserId(string userId, bool isExpired = false, bool isRevoked = false);

        void Add(RefreshToken item);

        Task SaveChangesAsync();

    }
}
