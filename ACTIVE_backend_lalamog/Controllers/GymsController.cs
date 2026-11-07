using System.Security.Claims;
using Lalamog.Api.Authorization;
using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Lalamog.Api.Controllers;

[ApiController]
[Route("api/gyms")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class GymsController : ControllerBase
{
    private readonly IGymService _gyms;
    public GymsController(IGymService gyms) { _gyms = gyms; }
    private Guid ActorId { get { return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); } }
    private Guid? OptionalActor()
    {
        string? id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return id == null ? null : Guid.Parse(id);
    }
    [Authorize(Policy = ApplicationPermissions.Visitor_ViewGyms)]
    [HttpGet]
    public async Task<ActionResult<List<GymResponse>>> List(CancellationToken ct) { return Ok(await _gyms.ListAsync(ct)); }
    [Authorize(Policy = ApplicationPermissions.Visitor_ViewGyms)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<GymResponse>> Get(long id, CancellationToken ct) { return Ok(await _gyms.GetAsync(id, OptionalActor(), ct)); }
    [Authorize(Policy = ApplicationPermissions.Visitor_ViewGyms)]
    [HttpGet("{id:long}/cover")]
    public async Task<IActionResult> Cover(long id, CancellationToken ct)
    {
        StoredFile file = await _gyms.CoverAsync(id, OptionalActor(), ct);
        Response.Headers.CacheControl = "no-store"; Response.Headers.XContentTypeOptions = "nosniff";
        return File(file.Content, file.ContentType);
    }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageGyms)]
    [HttpGet("mine")]
    public async Task<ActionResult<List<GymResponse>>> Mine(CancellationToken ct) { return Ok(await _gyms.OwnedAsync(ActorId, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageGyms)]
    [HttpPost]
    public async Task<ActionResult<GymResponse>> Create(GymRequest request, CancellationToken ct) { return Ok(await _gyms.CreateAsync(ActorId, request, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageGyms)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<GymResponse>> Update(long id, GymRequest request, CancellationToken ct) { return Ok(await _gyms.UpdateAsync(ActorId, id, request, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageGyms)]
    [HttpPost("{id:long}/publish")]
    public async Task<ActionResult<GymResponse>> Publish(long id, CancellationToken ct) { return Ok(await _gyms.PublishAsync(ActorId, id, true, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageGyms)]
    [HttpPost("{id:long}/unpublish")]
    public async Task<ActionResult<GymResponse>> Unpublish(long id, CancellationToken ct) { return Ok(await _gyms.PublishAsync(ActorId, id, false, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageGyms)]
    [HttpPost("{id:long}/cover")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadCover(long id, IFormFile file, CancellationToken ct)
    { await _gyms.UploadCoverAsync(ActorId, id, file, ct); return NoContent(); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageBookings)]
    [HttpGet("bookings")]
    public async Task<ActionResult<List<GymBookingResponse>>> Bookings(CancellationToken ct) { return Ok(await _gyms.BookingsAsync(ActorId, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageBookings)]
    [HttpPost("bookings/{id:long}/approve")]
    public async Task<ActionResult<GymBookingResponse>> Approve(long id, CancellationToken ct) { return Ok(await _gyms.DecideAsync(ActorId, id, "approve", null, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageBookings)]
    [HttpPost("bookings/{id:long}/decline")]
    public async Task<ActionResult<GymBookingResponse>> Decline(long id, CancelRequest request, CancellationToken ct) { return Ok(await _gyms.DecideAsync(ActorId, id, "decline", request.Reason, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageBookings)]
    [HttpPost("bookings/{id:long}/cancel")]
    public async Task<ActionResult<GymBookingResponse>> Cancel(long id, CancelRequest request, CancellationToken ct) { return Ok(await _gyms.DecideAsync(ActorId, id, "cancel", request.Reason, ct)); }
    [Authorize(Policy = ApplicationPermissions.Owner_ManageBookings)]
    [HttpPost("bookings/{id:long}/complete")]
    public async Task<ActionResult<GymBookingResponse>> Complete(long id, CancellationToken ct) { return Ok(await _gyms.DecideAsync(ActorId, id, "complete", null, ct)); }
}
