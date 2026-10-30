using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        Task<AuthResponse> LoginAsync(LoginRequest request);

        Task LogoutAsync();
    }
}
