using GeekApp.Shared.DTOs;

namespace GeekApp.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(LoginRequest request);
        Task<bool> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
    }
}
