using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly AppDbContext _db;

        public ConversationRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<List<ChatMessage>> ListAsync(
            long matchId, long? before, long? after, CancellationToken cancellationToken)
        {
            var query = from message in _db.Messages.AsNoTracking()
                        where message.MatchId == matchId
                        select message;

            if (after.HasValue)
            {
                var newerMessages = from message in query
                                    where message.Id > after.Value
                                    orderby message.Id
                                    select message;
                return newerMessages.Take(100).ToListAsync(cancellationToken);
            }
            if (before.HasValue)
            {
                query = from message in query
                        where message.Id < before.Value
                        select message;
            }

            var latestMessages = from message in query
                                 orderby message.Id descending
                                 select message;
            return latestMessages.Take(100).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
        {
            _db.Messages.Add(message);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
