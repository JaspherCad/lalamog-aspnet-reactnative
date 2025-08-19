using MyApi.DTOs;

namespace MyApi.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string email, string name);
    }
}
