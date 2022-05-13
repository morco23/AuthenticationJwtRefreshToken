using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MorCohen.Models.AuthenticateModels;

namespace MorCohen.Interfaces
{
    public interface IUsersRepository
    {
        ApplicationUser GetById(string userId, bool includeRefreshTokens);

        ApplicationUser GetByEmail(string emailAddress, bool includeRefreshTokens);

        Task SaveChangesAsync();

        int Count();
    }
}
