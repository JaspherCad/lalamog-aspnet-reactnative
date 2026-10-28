using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace Lalamog.Api.Repositories;
public class ConversationRepository(AppDbContext db) : IConversationRepository
{
    public Task<List<ChatMessage>> ListAsync(long matchId, long? before, long? after, CancellationToken ct)
    {
        var query = db.Messages.AsNoTracking().Where(x => x.MatchId == matchId);
        if (after.HasValue) return query.Where(x => x.Id > after.Value).OrderBy(x => x.Id).Take(100).ToListAsync(ct);
        if (before.HasValue) query = query.Where(x => x.Id < before.Value);
        return query.OrderByDescending(x => x.Id).Take(100).ToListAsync(ct);
    }
    public async Task AddAsync(ChatMessage message, CancellationToken ct)
    {
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);
    }
}