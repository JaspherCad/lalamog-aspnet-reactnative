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













        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleFight(ScheduleFightRequestDto dto)
        {
            Console.WriteLine($"MatchId: {dto.MatchId}");
            var result = await _fightingScheduleService.ScheduleFight(dto.UserId, dto);
            if (result == null) return BadRequest("Failed to schedule fight.");
            return Ok(result);
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmFight(ConfirmFightRequestDto confirmFightRequestDto)
        {
            var result = await _fightingScheduleService.ConfirmFight(
                confirmFightRequestDto.UserId,
                confirmFightRequestDto.FightScheduleId,
                confirmFightRequestDto.SafetyWaiver
            );
            if (result == null) return BadRequest("Failed to confirm fight.");
            return Ok(result);
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelFight(Guid userId, long fightScheduleId, string reason)
        {
            var result = await _fightingScheduleService.CancelFight(userId, fightScheduleId, reason);
            if (result == null) return BadRequest("Failed to cancel fight.");
            return Ok(result);
        }

    }


}