using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using MyApi.Interfaces;
using System.Security.Claims;

namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FightStatsController : ControllerBase
    {
        private readonly IFightStatsService _fightStatsService;

        public FightStatsController(IFightStatsService fightStatsService)
        {
            _fightStatsService = fightStatsService;
        }

        //#info
        // Record the result of a completed fight
        // How it works:
        // 1. Fighter completes a fight (fight status = "completed") 
        // 2. Either fighter can record the result using this endpoint
        // 3. System validates the user is a participant
        // 4. Updates both fighters' statistics and ratings
        //#end-info






        [HttpPost("record-result")]
        public async Task<IActionResult> RecordFightResult([FromBody] FightResultDto resultDto)
        {
            //{
            //   "FightScheduleId": 10,
            //   "WinnerId": "f2603802-deb3-4377-8651-1134db7837a7",
            //   "FightResult": "user1_win",
            //   "WinMethod": "KO",
            //   "FightDurationMinutes": 15,
            //   "ResultNotes": "Clean knockout in round 2",
            //   "User1Rating": 4,
            //   "User2Rating": 3,
            //   "User1Feedback": "Great fight, good sportsmanship",
            //   "User2Feedback": "Tough opponent, learned a lot"
            // }
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid user credentials");
                }

                // Validate fight result can be recorded
                var canRecord = await _fightStatsService.ValidateFightResultAsync(resultDto.FightScheduleId, userId);
                if (!canRecord)
                {
                    return BadRequest(new
                    {
                        message = "Cannot record result for this fight. Either fight is not completed, you're not a participant, or result already recorded."
                    });
                }

                var success = await _fightStatsService.RecordFightResultAsync(resultDto, userId);

                if (success)
                {
                    return Ok(new
                    {
                        message = "Fight result recorded successfully",
                        fightScheduleId = resultDto.FightScheduleId
                    });
                }

                return BadRequest(new { message = "Failed to record fight result" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }






        //     {
        //   "Confirmed": true,
        //   "Notes": "I confirm this result is accurate"
        // }

        // [HttpPost("confirm-result/{fightScheduleId}")]
        // public async Task<IActionResult> ConfirmFightResult(long fightScheduleId, [FromBody] FightConfirmationDto confirmationDto)
        // {
        //     try
        //     {
        //         var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //         if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        //         {
        //             return Unauthorized("Invalid user credentials");
        //         }


        //         return Ok(new { message = "Result confirmation feature coming soon" });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        //     }
        // }


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserStats(Guid userId)
        {
            try
            {
                var stats = await _fightStatsService.GetUserStatsAsync(userId);

                if (stats == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }


        [HttpGet("my-stats")]
        public async Task<IActionResult> GetMyStats()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid user credentials");
                }

                var stats = await _fightStatsService.GetUserStatsAsync(userId);

                if (stats == null)
                {
                    return NotFound(new { message = "User profile not found" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }



        //AI SAID THIS GOOD
        [HttpGet("head-to-head/{opponentId}")]
        public async Task<IActionResult> GetHeadToHeadStats(Guid opponentId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid user credentials");
                }

                var stats = await _fightStatsService.GetHeadToHeadStatsAsync(userId, opponentId, userId);

                if (stats == null)
                {
                    return NotFound(new { message = "No fight data found between these users" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }










        [HttpGet("fight-history")]
        public async Task<IActionResult> GetMyFightHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid user credentials");
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 20;

                var history = await _fightStatsService.GetUserFightHistoryAsync(userId, page, pageSize);

                return Ok(new
                {
                    data = history,
                    page,
                    pageSize,
                    hasMore = history.Count == pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }


        [HttpGet("fight-history/{userId}")]
        public async Task<IActionResult> GetUserFightHistory(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 20;

                var history = await _fightStatsService.GetUserFightHistoryAsync(userId, page, pageSize);

                return Ok(new
                {
                    data = history,
                    page,
                    pageSize,
                    hasMore = history.Count == pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }



    }
}
