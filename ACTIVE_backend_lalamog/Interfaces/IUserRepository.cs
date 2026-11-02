using Lalamog.Api.Models;

namespace Lalamog.Api.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string normalizedEmail);
    Task<bool> EmailExistsAsync(string normalizedEmail);
    Task AddAsync(AppUser user);
    Task UpdateAsync(AppUser user);
}
