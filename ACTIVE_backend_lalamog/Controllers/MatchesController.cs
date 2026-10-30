using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lalamog.Api.Controllers
{
    [Route("api/matches")]
    public class MatchesController : ApiController
    {
        private readonly IMatchingService _matchingService;
        private readonly IConversationService _conversationService;

        public MatchesController(IMatchingService matchingService, IConversationService conversationService)
        {
            _matchingService = matchingService;
            _conversationService = conversationService;
        }

        [HttpPost("swipes")]
        public async Task<ActionResult<SwipeResponse>> Swipe(SwipeRequest request, CancellationToken cancellationToken)
        {
            SwipeResponse response = await _matchingService.SwipeAsync(ActorId, request, cancellationToken);
            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<List<MatchResponse>>> List(CancellationToken cancellationToken)
        {
            List<MatchResponse> response = await _matchingService.ListAsync(ActorId, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:long}/messages")]
        public async Task<ActionResult<List<MessageResponse>>> Messages(
            long id, [FromQuery] long? before, [FromQuery] long? after, CancellationToken cancellationToken)
        {
            List<MessageResponse> response = await _conversationService.ListAsync(ActorId, id, before, after, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{id:long}/messages")]
        public async Task<ActionResult<MessageResponse>> Send(long id, MessageRequest request, CancellationToken cancellationToken)
        {
            MessageResponse response = await _conversationService.SendAsync(ActorId, id, request, cancellationToken);
            return Ok(response);
        }
    }
}
