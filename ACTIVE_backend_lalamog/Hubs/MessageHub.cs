using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lalamog.Api.Hubs
{
    // Receive-only: clients persist messages through the authorized REST controller.
    [Authorize]
    public class MessageHub : Hub
    {
    }
}
