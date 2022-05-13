using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MorCohen.Helpers;
using MorCohen.Interfaces;
using MorCohen.Models.AuthenticateModels;

namespace MorCohen.Data
{
    public class RefreshTokenRepository: IRefreshTokensRepository
    {
        private readonly ApplicationDbContext _context;
        private bool _isSaveRequired;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
            _isSaveRequired = false;
        }

        public RefreshToken GetByTokenValue(string token, bool includeApplicationUser = false)
        {
            return _context.RefreshTokens   .IncludeIf(includeApplicationUser, token => token.ApplicationUser)
                                            .FirstOrDefault(rt => rt.Token == token);

        }

        public List<RefreshToken> GetByApplicationUserId(string userId, bool isExpired = false, bool isRevoked = false)
        {
            var queryable = _context.RefreshTokens.Where(token => token.ApplicationUserId == userId);
            queryable = isExpired ? queryable.Where(token => token.Expires <= DateTime.Now) : queryable.Where(token => token.Expires > DateTime.Now);
            queryable = isRevoked ? queryable.Where(token => token.Revoked != null) : queryable.Where(token => token.Revoked == null);
            return queryable.ToList();
        }

        public void Add(RefreshToken item)
        {
            _context.Add(item);
            _isSaveRequired = true;
        }


        public async Task SaveChangesAsync()
        {
            if (_isSaveRequired)
            {
                await _context.SaveChangesAsync();
            }
            _isSaveRequired = false;
        }
    }
}
