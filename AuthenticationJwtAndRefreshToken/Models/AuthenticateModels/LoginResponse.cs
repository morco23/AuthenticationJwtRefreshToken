namespace MorCohen.Models.AuthenticateModels
{
    public class LoginResponse
    {
        public LoginResponse(bool success, string token = null, string refreshToken = null)
        {
            Success = success;
            Token = token;
            RefreshToken = refreshToken;
        }

        public bool Success { get; }
        public string Token { get; set;  }
        public string RefreshToken { get; set; }
    }
}
