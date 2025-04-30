using GeekApp.Shared.DTOs;

namespace GeekApp.Server.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> Register(RegisterRequest request);
        Task<AuthResponse> Login(LoginRequest request);
    }
}
