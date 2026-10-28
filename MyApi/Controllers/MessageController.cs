using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using MyApi.Services;
using MyApi.Interfaces;
using System.Security.Claims;

//#error: WE ARE NO LONGER USING THIS!
//#error: WE ARE NOW USING MessageHub.cs (SignalR WEBSOCKET)


//#error: WE ARE NO LONGER USING THIS!
//#error: WE ARE NOW USING MessageHub.cs (SignalR WEBSOCKET)


//#error: WE ARE NO LONGER USING THIS!
//#error: WE ARE NOW USING MessageHub.cs (SignalR WEBSOCKET)

namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;

        public MessageController(IMessageService messageService, IUserService userService)
        {
            _messageService = messageService;
            _userService = userService;
        }



        // {
        //         SenderId = senderId,
        //         ReceiverId = receiverId,
        //         Content = content,
        //         CreatedAt = DateTime.UtcNow
        //     };

        [HttpPost("sendMessage")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto messageDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Guid.TryParse(userId, out Guid senderId))
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            try
            {
                var result = await _messageService.SendMessageAsync(senderId, messageDto.ReceiverId, messageDto.Content, messageDto.MatchId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Failed to send message" });
            }
        }


        //get msg by match (more unique than userIDS)
        [HttpGet("match/{matchId}")]
        [Authorize]
        public async Task<IActionResult> GetMessageHistory(long matchId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!Guid.TryParse(userId, out Guid senderId))
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            var messages = await _messageService.GetMessagesByMatchIdAsync(matchId);
            return Ok(messages);
        }

        
    }
}