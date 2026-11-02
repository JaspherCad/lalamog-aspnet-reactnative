using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AppUser?> FindByEmailAsync(string normalizedEmail)
    {
        var query = from user in _db.Users.AsNoTracking()
                    where user.NormalizedEmail == normalizedEmail
                    select user;
        return query.SingleOrDefaultAsync();
    }

    public Task<bool> EmailExistsAsync(string normalizedEmail)
    {
        var query = from user in _db.Users
                    where user.NormalizedEmail == normalizedEmail
                    select user.Id;
        return query.AnyAsync();
    }

    public async Task AddAsync(AppUser user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppUser user)
    {
        _db.Users.Update(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            throw;
        }
    }
}
