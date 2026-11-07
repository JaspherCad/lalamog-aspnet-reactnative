using Lalamog.Api.Authorization;
using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Lalamog.Api.Controllers;
[Route("api/admin/gyms")]
[Authorize(Policy = ApplicationPermissions.Admin_ManageMaps)]
public class AdminGymsController : ApiController
{
    private readonly IGymService _gyms;
    public AdminGymsController(IGymService gyms) { _gyms = gyms; }
    [HttpGet]
    public async Task<ActionResult<List<AdminGymResponse>>> List(CancellationToken ct) { return Ok(await _gyms.AdminListAsync(ct)); }
    [HttpPost("{id:long}/unpublish")]
    public async Task<IActionResult> Unpublish(long id, CancellationToken ct) { await _gyms.AdminUnpublishAsync(id, ct); return NoContent(); }
    [HttpPost("{id:long}/owner")]
    public async Task<IActionResult> AssignOwner(long id, AssignGymOwnerRequest request, CancellationToken ct)
    { await _gyms.AssignOwnerAsync(id, request.OwnerUserId, ct); return NoContent(); }
}
