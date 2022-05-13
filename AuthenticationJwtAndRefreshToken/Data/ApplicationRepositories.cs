using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MorCohen.Interfaces;

namespace MorCohen.Data
{
    public class ApplicationRepositories
    {
        public IUsersRepository UsersRepository { get; set; }

        public IRefreshTokensRepository RefreshTokensRepository { get; set; }

        public ApplicationRepositories(IUsersRepository usersRepository, IRefreshTokensRepository refreshTokensRepository)
        {
            UsersRepository = usersRepository;
            RefreshTokensRepository = refreshTokensRepository;
        }

        public async Task SaveChangesAsync()
        {
            await UsersRepository.SaveChangesAsync();
            await RefreshTokensRepository.SaveChangesAsync();
        }
    }
}
