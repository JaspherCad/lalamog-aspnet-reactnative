using Lalamog.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lalamog.Api.Controllers
{
    [Authorize(Policy = ApplicationPermissions.User_Schedule)]
    [Route("api/fights")]
    public class FightsController : ApiController
    {
        private readonly IFightService _fightService;

        public FightsController(IFightService fightService)
        {
            _fightService = fightService;
        }

        [HttpGet]
        public async Task<ActionResult<List<FightResponse>>> List(CancellationToken cancellationToken)
        {
            List<FightResponse> response = await _fightService.ListAsync(ActorId, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<FightResponse>> Get(long id, CancellationToken cancellationToken)
        {
            FightResponse response = await _fightService.GetAsync(ActorId, id, cancellationToken);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<FightResponse>> Schedule(ScheduleRequest request, CancellationToken cancellationToken)
        {
            FightResponse response = await _fightService.ScheduleAsync(ActorId, request, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{id:long}/publication")]
        public async Task<ActionResult<FightResponse>> Publication(long id, PublicationRequest request, CancellationToken cancellationToken)
        { return Ok(await _fightService.PublicationAsync(ActorId, id, request, cancellationToken)); }

        [HttpPost("{id:long}/confirm")]
        public async Task<ActionResult<FightResponse>> Confirm(long id, ConfirmRequest request, CancellationToken cancellationToken)
        {
            FightResponse response = await _fightService.ConfirmAsync(ActorId, id, request, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{id:long}/cancel")]
        public async Task<ActionResult<FightResponse>> Cancel(long id, CancelRequest request, CancellationToken cancellationToken)
        {
            FightResponse response = await _fightService.CancelAsync(ActorId, id, request, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{id:long}/complete")]
        public async Task<ActionResult<FightResponse>> Complete(long id, CancellationToken cancellationToken)
        {
            FightResponse response = await _fightService.CompleteAsync(ActorId, id, cancellationToken);
            return Ok(response);
        }
    }
}
