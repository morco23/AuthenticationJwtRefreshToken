using MorCohen.Helpers;
using MorCohen.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using MorCohen.Models.AuthenticateModels;

namespace MorCohen.Data
{
    public class UsersRepository : IUsersRepository
    {
        private readonly ApplicationDbContext _context;
        private bool _isSaveRequired;

        public UsersRepository(ApplicationDbContext context)
        {
            _context = context;
            _isSaveRequired = false;
        }

        public ApplicationUser GetById(string userId, bool includeRefreshTokens)
        {
            return _context.Users.IncludeIf(includeRefreshTokens, user => user.RefreshTokens).FirstOrDefault(user => user.Id == userId);
        }

        public ApplicationUser GetByEmail(string emailAddress, bool includeRefreshTokens)
        {
            return _context.Users.IncludeIf(includeRefreshTokens, user => user.RefreshTokens).FirstOrDefault(user => user.Email == emailAddress);
        }

        public async Task SaveChangesAsync()
        {
            if (_isSaveRequired)
            {
                await _context.SaveChangesAsync();
            }
        }

        public int Count()
        {
            return _context.Users.Count();
        }
    }
}
