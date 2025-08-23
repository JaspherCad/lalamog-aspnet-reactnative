using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MyApi.DTOs;
using MyApi.Services;
using MyApi.Interfaces;
using System.Security.Claims;


namespace MyApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class FightingScheduleController : ControllerBase
    {
        IFightingScheduleService _fightingScheduleService;

        public FightingScheduleController(IFightingScheduleService fightingScheduleService)
        {
            _fightingScheduleService = fightingScheduleService;
        }
        //eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJmMjYwMzgwMi1kZWIzLTQzNzctODY1MS0xMTM0ZGI3ODM3YTciLCJlbWFpbCI6ImphbmUuZG9lQGV4YW1wbGUuY29tIiwidW5pcXVlX25hbWUiOiJKYW5lIERvZSIsIm5iZiI6MTc1NDk3NTk2MiwiZXhwIjoxNzU1NTgwNzYyLCJpYXQiOjE3NTQ5NzU5NjIsImlzcyI6Ik15QXBpIiwiYXVkIjoiTXlBcGkifQ.ic2rgWsFYI1fH6aFf0vFbQ53itVR-0Wsfw7zykY0Ixg






        //#info:
        // in match user can schedule fight
        // match has fightSchedule1, 2, 3, ...n
        //#end-info:







        [HttpPost("schedule")]
        [Authorize]
        public async Task<IActionResult> ScheduleFight(ScheduleFightRequestDto dto)
        {

            //             {
            //   "matchId": 12,
            //   "scheduledDateTime": "2025-08-26T18:00:00Z",
            //   "locationName": "Downtown Boxing Gym",
            //   "locationAddress": "fetchedOnOpenMapApi",
            //   "locationCoordinates": {
            //     "x": -118.2437,
            //     "y": 34.0522
            //   }
            // }
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid user credentials");
                }

                Console.WriteLine($"User {userId} scheduling fight for MatchId: {dto.MatchId}");
                var result = await _fightingScheduleService.ScheduleFight(userId, dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }





        [HttpGet("all-my-schedule")]
        [Authorize]
        public async Task<IActionResult> GetAllSchedule()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user credentials");
            }

            var result = await _fightingScheduleService.GetAllSchedulesForUser(userId);
            return Ok(result);
        }


        [HttpGet("match/{matchId}/schedules/{fightScheduleId}")]
        [Authorize]
        public async Task<IActionResult> InfoOfSchedule(long matchId, long fightScheduleId)
        {
            

            var result = await _fightingScheduleService.GetScheduleInfo(matchId, fightScheduleId);
            return Ok(result);
        }

    

       
        [HttpGet("match/{matchId}/schedules")]
        [Authorize]
        public async Task<IActionResult> GetAllSchedulesForMatch(long matchId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid user credentials");
                }

                var result = await _fightingScheduleService.GetAllSchedulesForMatch(matchId, userId);
                return Ok(new
                {
                    matchId = matchId,
                    schedules = result,
                    totalCount = result.Count()
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }



        [HttpPost("confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmFight(ConfirmFightRequestDto confirmFightRequestDto)
        {
            //             {
            //   "fightScheduleId": 25,
            //   "safetyWaiver": {
            //     "emergencyContactName": "John Smith",
            //     "emergencyContactPhone": "+1-555-123-4567"
            //   }
            // }
            try
            {
                // Extract userId from JWT token automatically
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid user credentials");
                }

                var result = await _fightingScheduleService.ConfirmFight(
                    userId,
                    confirmFightRequestDto.FightScheduleId,
                    confirmFightRequestDto.SafetyWaiver
                );
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelFight(Guid userId, long fightScheduleId, string reason)
        {
            var result = await _fightingScheduleService.CancelFight(userId, fightScheduleId, reason);
            if (result == null) return BadRequest("Failed to cancel fight.");
            return Ok(result);
        }


        [HttpPost("complete/{fightScheduleId}")]
        [Authorize]
        public async Task<IActionResult> CompleteFight(long fightScheduleId, [FromBody] CompleteFightDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid user credentials");
                }

                var result = await _fightingScheduleService.CompleteFight(userId, fightScheduleId, dto);
                return Ok(new
                {
                    message = "Fight marked as completed successfully",
                    fight = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

    }


}