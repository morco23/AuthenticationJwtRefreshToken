using System.Threading.Tasks;
using MorCohen.Models.AuthenticateModels;

namespace MorCohen.Interfaces
{
    public interface IApplicationAccountManager
    {
        Task CreateAccountAsync(CreateUserRequest request);

        Task<LoginResponse> LoginAsync(LoginRequest request, AdditionalLoginInfo additionalLoginInfo);

        Task<LoginResponse> RefreshAsync(string userId, AdditionalLoginInfo additionalLoginInfo);
    }
}
