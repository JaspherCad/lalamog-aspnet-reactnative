using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;

namespace Lalamog.Api.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMatchingService _matchingService;

        public ConversationService(
            IConversationRepository conversationRepository,
            IMatchingService matchingService)
        {
            _conversationRepository = conversationRepository;
            _matchingService = matchingService;
        }

        public async Task<List<MessageResponse>> ListAsync(
            Guid actor, long matchId, long? before, long? after, CancellationToken cancellationToken)
        {
            await _matchingService.RequireMemberAsync(matchId, actor, cancellationToken);
            List<ChatMessage> messages = await _conversationRepository.ListAsync(matchId, before, after, cancellationToken);
            if (!after.HasValue)
            {
                messages.Reverse();
            }

            var responses = new List<MessageResponse>();
            foreach (ChatMessage message in messages)
            {
                responses.Add(DtoMapper.ToMessageResponse(message));
            }
            return responses;
        }

        public async Task<MessageResponse> SendAsync(
            Guid actor, long matchId, MessageRequest request, CancellationToken cancellationToken)
        {
            await _matchingService.RequireMemberAsync(matchId, actor, cancellationToken);
            string content = request.Content.Trim();
            if (content.Length < 1 || content.Length > 2000)
            {
                throw new ApiException(400, "Enter a message of 1 to 2000 characters.");
            }

            var message = new ChatMessage { MatchId = matchId, SenderId = actor, Content = content };
            await _conversationRepository.AddAsync(message, cancellationToken);
            return DtoMapper.ToMessageResponse(message);
        }
    }
}
