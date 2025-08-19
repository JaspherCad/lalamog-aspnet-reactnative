using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyApi.DTOs;
using MyApi.Models;

namespace MyApi.Interfaces
{
    public interface IFightingScheduleService
    {
        Task<bool> CanScheduleFight(Guid userId, long matchId);
        Task<FightScheduleDto> ScheduleFight(Guid userId, ScheduleFightRequestDto dto);
        Task<FightScheduleDto> ConfirmFight(Guid userId, long fightScheduleId, SafetyWaiverDto waiverDto);
        Task<FightSchedule> CancelFight(Guid userId, long fightScheduleId, string reason);
    }
}