using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace Lalamog.Api.Hubs;

// Receive-only. The framework routes notifications by authenticated NameIdentifier.
// Messages can only be persisted through the authorized HTTP controller.
[Authorize]
public class MessageHub : Hub;