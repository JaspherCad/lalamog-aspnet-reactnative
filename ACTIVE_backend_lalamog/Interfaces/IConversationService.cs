using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces
{
    public interface IConversationService
    {
        Task<List<MessageResponse>> ListAsync(Guid actor, long matchId, long? before, long? after, CancellationToken cancellationToken);

        Task<MessageResponse> SendAsync(Guid actor, long matchId, MessageRequest request, CancellationToken cancellationToken);
    }
}
