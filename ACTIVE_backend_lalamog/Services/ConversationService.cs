using Lalamog.Api.DTOs;
using Lalamog.Api.Hubs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace Lalamog.Api.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMatchingService _matchingService;
        private readonly IHubContext<MessageHub> _hub;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IConversationRepository conversationRepository,
            IMatchingService matchingService,
            IHubContext<MessageHub> hub,
            ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _matchingService = matchingService;
            _hub = hub;
            _logger = logger;
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
            FighterMatch match = await _matchingService.RequireMemberAsync(matchId, actor, cancellationToken);
            string content = request.Content.Trim();
            if (content.Length < 1 || content.Length > 2000)
            {
                throw new ApiException(400, "Enter a message of 1 to 2000 characters.");
            }

            var message = new ChatMessage { MatchId = matchId, SenderId = actor, Content = content };
            await _conversationRepository.AddAsync(message, cancellationToken);
            MessageResponse response = DtoMapper.ToMessageResponse(message);
            try
            {
                await _hub.Clients.Users(match.User1Id.ToString(), match.User2Id.ToString())
                    .SendAsync("MessageReceived", response, CancellationToken.None);
            }
            catch (Exception exception)
            {
                // SQL persistence succeeded; clients recover the message through REST.
                _logger.LogWarning(exception, "Live delivery failed for message {MessageId}", message.Id);
            }
            return response;
        }
    }
}
