using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MyApi.DTOs;
using MyApi.Models;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite;
using NpgsqlTypes;
using MyApi.Interfaces;
namespace MyApi.Services
{
    public class FightingScheduleService : IFightingScheduleService
    {
        private readonly ApplicationDbContext _context;

        public FightingScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }






        public async Task<bool> CanScheduleFight(Guid userId, long matchId)
        {
            // 1. Verify user is part of the match
            var match = await _context.Matches
                .Where(m => m.Id == matchId &&
                    (m.User1Id == userId || m.User2Id == userId))
                .FirstOrDefaultAsync();

            if (match == null)
            {
                Console.WriteLine("User is not part of the match");
                return false;
            }


            // 2. Verify no existing scheduled fight for this match
            var existingFight = await _context.FightSchedules
                .AnyAsync(fs => fs.MatchId == matchId &&
                    fs.Status != "canceled" && fs.Status != "completed");

            if (existingFight)
            {
                Console.WriteLine("User already has a scheduled fight for this match");
                return false;
            }

            // 3. Verify user's profile is complete
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null ||
                profile.ExperienceLevel == null ||
                string.IsNullOrEmpty(profile.FightingStyle))
            {

                Console.WriteLine("User profile is incomplete");
                return false;
            }

            return true;
        }








        public async Task<FightScheduleDto> ScheduleFight(
        Guid userId,
        ScheduleFightRequestDto dto)
        {


            // 1. Validate scheduling ability
            if (!await CanScheduleFight(userId, dto.MatchId))
            {
                Console.WriteLine($"MatchId: {dto.MatchId}");
                throw new ArgumentException("Cannot schedule fight");
            }



            // 2. Get match details
            var match = await _context.Matches
                .Include(m => m.User1)
                .Include(m => m.User2)
                .FirstOrDefaultAsync(m => m.Id == dto.MatchId);

            if (match == null) throw new ArgumentException("Match not found");

            //#info: since match is in lexicographically order, we can automate the user1/user2 assignment here


            // 3. Get profiles for skill levels
            var user1Profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == match.User1Id);

            var user2Profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == match.User2Id);

            if (user1Profile == null || user2Profile == null)
                throw new ArgumentException("Profile not found");

            // 4. Create fight schedule
            var fightSchedule = new FightSchedule
            {
                MatchId = dto.MatchId,
                ScheduledDateTime = dto.ScheduledDateTime,
                LocationName = dto.LocationName,
                LocationAddress = dto.LocationAddress,

                User1SkillLevelAtTimeOfScheduling = user1Profile.ExperienceLevel ?? 1,
                User2SkillLevelAtTimeOfScheduling = user2Profile.ExperienceLevel ?? 1,
                // #warning: Safety waiver and emergency contacts will be filled when confirmed
                Status = "scheduled"
            };

            //#note: location point
            if (dto.LocationCoordinates != null)
            {
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                fightSchedule.LocationCoordinates = geometryFactory.CreatePoint(new Coordinate(
                    dto.LocationCoordinates.X, // longitude
                    dto.LocationCoordinates.Y  // latitude
                ));
            }

            var entry = _context.FightSchedules.Add(fightSchedule);
            await _context.SaveChangesAsync();


            //return as DTO
            var fightScheduleDto = new FightScheduleDto
            {
                Id = entry.Entity.Id,
                MatchId = fightSchedule.MatchId,
                ScheduledDateTime = fightSchedule.ScheduledDateTime,
                LocationName = fightSchedule.LocationName,
                LocationAddress = fightSchedule.LocationAddress,

                //#note: 
                // Cannot implicitly convert type 'NetTopologySuite.Geometries.Point' to 'MyApi.DTOs.LocationDto'
                //#end-note:       

                LocationCoordinates = fightSchedule.LocationCoordinates != null
                    ? new LocationDto
                    {
                        X = fightSchedule.LocationCoordinates.X,
                        Y = fightSchedule.LocationCoordinates.Y
                    }
                    : null,
                User1SkillLevelAtTimeOfScheduling = fightSchedule.User1SkillLevelAtTimeOfScheduling,
                User2SkillLevelAtTimeOfScheduling = fightSchedule.User2SkillLevelAtTimeOfScheduling,
                Status = fightSchedule.Status
            };


            return fightScheduleDto;
        }


        public async Task<FightScheduleDto> ConfirmFight(
        Guid userId,
        long fightScheduleId,
        SafetyWaiverDto waiverDto)
        {
            var fight = await _context.FightSchedules
                .Include(fs => fs.Match) //#warning: Match is from different table so eager load.
                .FirstOrDefaultAsync(fs => fs.Id == fightScheduleId);

            //#sql: 
            // SELECT * FROM FightSchedules fs 
            // LEFT JOIN Matches m ON m.Id = fs.MatchId 
            // WHERE fs.Id = @fightScheduleId
            //#end-sql

            if (fight == null) throw new ArgumentException("Fight not found");
            //print fight
            Console.WriteLine($"Fight found: {fight}");

            if (fight.Match == null) throw new ArgumentException("Fight is not associated with a match");

            // Determine which user is confirming
            // if userId === fight.Match.User1Id
            if (fight.Match.User1Id == userId)
            {
                fight.IsSafetyWaiverAcceptedByUser1 = true;
                fight.User1EmergencyContactName = waiverDto.EmergencyContactName;
                fight.User1EmergencyContactPhone = waiverDto.EmergencyContactPhone;
            }
            else if (fight.Match.User2Id == userId)
            {
                fight.IsSafetyWaiverAcceptedByUser2 = true;
                fight.User2EmergencyContactName = waiverDto.EmergencyContactName;
                fight.User2EmergencyContactPhone = waiverDto.EmergencyContactPhone;
            }
            else
            {
                throw new UnauthorizedAccessException();
            }

            // #warning If both have accepted, move to confirmed state
            if (fight.IsSafetyWaiverAcceptedByUser1 &&
                fight.IsSafetyWaiverAcceptedByUser2)
            {
                fight.Status = "confirmed";
            }

            await _context.SaveChangesAsync();


            //#error: TO AVOID possible object cycle was detected 
            // because we also have FightSchedule inside match table (WHICH IS VERY OTPIONAL)
            //         public ICollection<FightSchedule> FightSchedules { get; set; } = new List<FightSchedule>();
            // #note: remember: always ONLY Many to One code, no need for One to Many that causes issue like this.. so return DTO to avoid error

            //#end-error: 

            //#info: fight here is already updated ---> idk why but not very SPRING BOOT.

            var fightDto = new FightScheduleDto
            {
                Id = fight.Id,
                MatchId = fight.MatchId,
                ScheduledDateTime = fight.ScheduledDateTime,
                LocationName = fight.LocationName,
                LocationAddress = fight.LocationAddress,
                LocationCoordinates = fight.LocationCoordinates != null
        ? new LocationDto { X = fight.LocationCoordinates.X, Y = fight.LocationCoordinates.Y }
        : null,
                IsSafetyWaiverAcceptedByUser1 = fight.IsSafetyWaiverAcceptedByUser1,
                IsSafetyWaiverAcceptedByUser2 = fight.IsSafetyWaiverAcceptedByUser2,


                User1EmergencyContactName = fight.User1EmergencyContactName ?? string.Empty,
                User1EmergencyContactPhone = fight.User1EmergencyContactPhone ?? string.Empty,
                User2EmergencyContactName = fight.User2EmergencyContactName ?? string.Empty,
                User2EmergencyContactPhone = fight.User2EmergencyContactPhone ?? string.Empty,


                User1SkillLevelAtTimeOfScheduling = fight.User1SkillLevelAtTimeOfScheduling,
                User2SkillLevelAtTimeOfScheduling = fight.User2SkillLevelAtTimeOfScheduling,
                Status = fight.Status,
                CreatedAt = fight.CreatedAt,
                UpdatedAt = fight.UpdatedAt
                // add whatever other safe fields you want
            };

            return fightDto;
        }









        public async Task<FightSchedule> CancelFight(
        Guid userId,
        long fightScheduleId,
        string reason)
        {
            var fight = await _context.FightSchedules
                .FirstOrDefaultAsync(fs => fs.Id == fightScheduleId);

            if (fight == null) throw new ArgumentException("Fight not found");

            // Verify user is part of this fight
            if (fight.Match.User1Id != userId && fight.Match.User2Id != userId)
            {
                throw new UnauthorizedAccessException();
            }

            fight.Status = "canceled";
            fight.CancellationReason = reason;
            fight.CanceledAt = DateTime.UtcNow;
            fight.CanceledByUserId = userId;

            await _context.SaveChangesAsync();
            return fight;
        }



    }
}