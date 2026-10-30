using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lalamog.Api.Controllers
{
    [ApiController]
    [Authorize]
    public abstract class ApiController : ControllerBase
    {
        protected Guid ActorId
        {
            get
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                return Guid.Parse(userId);
            }
        }
    }
}
