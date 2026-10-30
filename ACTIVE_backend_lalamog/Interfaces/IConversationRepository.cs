using Lalamog.Api.Models;

namespace Lalamog.Api.Interfaces
{
    public interface IConversationRepository
    {
        Task<List<ChatMessage>> ListAsync(long matchId, long? before, long? after, CancellationToken cancellationToken);

        Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    }
}
