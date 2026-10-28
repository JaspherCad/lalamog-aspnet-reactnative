using Lalamog.Api.Contracts;
using Lalamog.Api.Models;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace Lalamog.Api.Controllers;
[Route("api/matches")]
public class MatchesController(MatchingService matches, ConversationService conversations) : ApiController
{
    [HttpPost("swipes")]
    public Task<SwipeResponse> Swipe(SwipeRequest request, CancellationToken ct) => matches.SwipeAsync(ActorId, request, ct);
    [HttpGet]
    public Task<List<MatchResponse>> List(CancellationToken ct) => matches.ListAsync(ActorId, ct);
    [HttpGet("{id:long}/messages")]
    public Task<List<ChatMessage>> Messages(long id, [FromQuery] long? before, [FromQuery] long? after, CancellationToken ct) =>
        conversations.ListAsync(ActorId, id, before, after, ct);
    [HttpPost("{id:long}/messages")]
    public Task<ChatMessage> Send(long id, MessageRequest request, CancellationToken ct) =>
        conversations.SendAsync(ActorId, id, request, ct);
}