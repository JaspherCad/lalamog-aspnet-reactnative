using Lalamog.Api.Contracts;
using Lalamog.Api.Hubs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.AspNetCore.SignalR;
namespace Lalamog.Api.Services;
public class ConversationService(IConversationRepository messages, MatchingService matches,
    IHubContext<MessageHub> hub, ILogger<ConversationService> logger)
{
    public async Task<List<ChatMessage>> ListAsync(Guid actor, long matchId, long? before, long? after, CancellationToken ct)
    {
        await matches.RequireMemberAsync(matchId, actor, ct);
        var result = await messages.ListAsync(matchId, before, after, ct);
        if (!after.HasValue) result.Reverse();
        return result;
    }
    public async Task<ChatMessage> SendAsync(Guid actor, long matchId, MessageRequest request, CancellationToken ct)
    {
        var match = await matches.RequireMemberAsync(matchId, actor, ct);
        var content = request.Content.Trim();
        if (content.Length is < 1 or > 2000) throw new ApiException(400, "Enter a message of 1 to 2000 characters.");
        var message = new ChatMessage { MatchId = matchId, SenderId = actor, Content = content };
        await messages.AddAsync(message, ct);
        try
        {
            await hub.Clients.Users(match.User1Id.ToString(), match.User2Id.ToString())
                .SendAsync("MessageReceived", message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Persistence succeeded. Clients can recover this message from HTTP history.
            logger.LogWarning(ex, "Live delivery failed for message {MessageId}", message.Id);
        }
        return message;
    }
}