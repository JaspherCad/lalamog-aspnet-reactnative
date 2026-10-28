using Lalamog.Api.Contracts;
using Lalamog.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace Lalamog.Api.Controllers;
[Route("api/fights")]
public class FightsController(FightService fights) : ApiController
{
    [HttpGet]
    public Task<List<FightResponse>> List(CancellationToken ct) => fights.ListAsync(ActorId, ct);
    [HttpGet("{id:long}")]
    public Task<FightResponse> Get(long id, CancellationToken ct) => fights.GetAsync(ActorId, id, ct);
    [HttpPost]
    public Task<FightResponse> Schedule(ScheduleRequest request, CancellationToken ct) => fights.ScheduleAsync(ActorId, request, ct);
    [HttpPost("{id:long}/confirm")]
    public Task<FightResponse> Confirm(long id, ConfirmRequest request, CancellationToken ct) => fights.ConfirmAsync(ActorId, id, request, ct);
    [HttpPost("{id:long}/cancel")]
    public Task<FightResponse> Cancel(long id, CancelRequest request, CancellationToken ct) => fights.CancelAsync(ActorId, id, request, ct);
    [HttpPost("{id:long}/complete")]
    public Task<FightResponse> Complete(long id, CancellationToken ct) => fights.CompleteAsync(ActorId, id, ct);
}