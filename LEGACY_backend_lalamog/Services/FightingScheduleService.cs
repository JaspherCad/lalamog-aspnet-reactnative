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
            // 1. Verify user is part of the match and get match details with includes
            var match = await _context.Matches
                .Include(m => m.User1)
                .Include(m => m.User2)
                .Where(m => m.Id == dto.MatchId &&
                    (m.User1Id == userId || m.User2Id == userId))
                .FirstOrDefaultAsync();

            if (match == null)
            {
                throw new ArgumentException("You are not part of this match or match not found");
            }

            // 2. Check for existing active fight schedules
            var existingFight = await _context.FightSchedules
                .AnyAsync(fs => fs.MatchId == dto.MatchId &&
                    fs.Status != "canceled" && fs.Status != "completed");

            if (existingFight)
            {
                throw new ArgumentException("This match already has an active fight schedule. Please cancel or complete the existing fight before scheduling a new one.");
            }

            // 3. Verify user's profile is complete
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null ||
                profile.ExperienceLevel == null ||
                string.IsNullOrEmpty(profile.FightingStyle))
            {
                throw new ArgumentException("Please complete your profile (experience level and fighting style) before scheduling a fight");
            }

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




        public async Task<FightScheduleDto> GetScheduleInfo(long matchId, long fightScheduleId)
        {

            //keep simple no need extra validations just str8 to the point.
            var fightSchedule = await _context.FightSchedules
                .Include(fs => fs.Match)
                .FirstOrDefaultAsync(fs => fs.Id == fightScheduleId && fs.MatchId == matchId);

            if (fightSchedule == null) throw new ArgumentException("Fight schedule not found");

            

            return new FightScheduleDto
            {
                Id = fightSchedule.Id,
                MatchId = fightSchedule.MatchId,
                ScheduledDateTime = fightSchedule.ScheduledDateTime,
                LocationName = fightSchedule.LocationName,
                LocationAddress = fightSchedule.LocationAddress,
                LocationCoordinates = fightSchedule.LocationCoordinates != null
                    ? new LocationDto
                    {
                        X = fightSchedule.LocationCoordinates.X,
                        Y = fightSchedule.LocationCoordinates.Y
                    }
                    : null,
                User1SkillLevelAtTimeOfScheduling = fightSchedule.User1SkillLevelAtTimeOfScheduling,
                User2SkillLevelAtTimeOfScheduling = fightSchedule.User2SkillLevelAtTimeOfScheduling,
                Status = fightSchedule.Status,
                FightGifUrl = fightSchedule.FightGifUrl
            };
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

            //#end-error: nvm, an error occured here haha.

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
                UpdatedAt = fight.UpdatedAt,
                FightGifUrl = fight.FightGifUrl
                // add whatever other safe fields you want
            };

            return fightDto;
        }




        public async Task<IEnumerable<FightScheduleDto>> GetAllSchedulesForUser(Guid userId)
        {
            var fights = await _context.FightSchedules
                .Include(fs => fs.Match)
                .Where(fs => fs.Match.User1Id == userId || fs.Match.User2Id == userId)
                .ToListAsync();

            //returns list then .map the list  to get DTOs
            
            return fights.Select(fight => new FightScheduleDto
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
                UpdatedAt = fight.UpdatedAt,
                FightGifUrl = fight.FightGifUrl
            });
        }

       
        public async Task<FightScheduleDto> CompleteFight(
            Guid userId,
            long fightScheduleId,
            CompleteFightDto dto)
        {
            var fight = await _context.FightSchedules
                .Include(fs => fs.Match)
                .FirstOrDefaultAsync(fs => fs.Id == fightScheduleId);

            if (fight == null) throw new ArgumentException("Fight not found");
            if (fight.Match == null) throw new ArgumentException("Fight is not associated with a match");

            // Verify user is part of this fight
            if (fight.Match.User1Id != userId && fight.Match.User2Id != userId)
            {
                throw new UnauthorizedAccessException("You are not a participant in this fight");
            }

            // Verify fight is in confirmed status
            if (fight.Status != "confirmed")
            {
                throw new ArgumentException("Fight must be confirmed before it can be completed");
            }

            // Update fight status to completed
            fight.Status = "completed";
            fight.UpdatedAt = DateTime.UtcNow;

           

            await _context.SaveChangesAsync();

            // Return updated fight as DTO
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
                UpdatedAt = fight.UpdatedAt,
                FightGifUrl = fight.FightGifUrl
            };

            return fightDto;
        }

        public async Task<FightScheduleDto> CancelFight(
        Guid userId,
        long fightScheduleId,
        string reason)
        {
            var fight = await _context.FightSchedules
                .Include(fs => fs.Match) // #warning: Match is from different table so eager load.
                .FirstOrDefaultAsync(fs => fs.Id == fightScheduleId);

            if (fight == null) throw new ArgumentException("Fight not found");
            if (fight.Match == null) throw new ArgumentException("Fight is not associated with a match");

            // Verify user is part of this fight
            if (fight.Match.User1Id != userId && fight.Match.User2Id != userId)
            {
                throw new UnauthorizedAccessException("You are not a participant in this fight");
            }

            fight.Status = "canceled";
            fight.CancellationReason = reason;
            fight.CanceledAt = DateTime.UtcNow;
            fight.CanceledByUserId = userId;

            await _context.SaveChangesAsync();

            // Return as DTO to avoid circular reference
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
                CancellationReason = fight.CancellationReason ?? string.Empty,
                CanceledAt = fight.CanceledAt,
                CanceledByUserId = fight.CanceledByUserId,
                CreatedAt = fight.CreatedAt,
                UpdatedAt = fight.UpdatedAt,
                FightGifUrl = fight.FightGifUrl
            };

            return fightDto;
        }


        public async Task<IEnumerable<FightScheduleDto>> GetAllPublicFightSchedules()
        {
            var fights = await _context.FightSchedules
                .Include(fs => fs.Match)
                .OrderByDescending(fs => fs.ScheduledDateTime)
                .ToListAsync();

            return fights.Select(fight => new FightScheduleDto
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
                CancellationReason = fight.CancellationReason ?? string.Empty,
                CanceledAt = fight.CanceledAt,
                CanceledByUserId = fight.CanceledByUserId,
                CreatedAt = fight.CreatedAt,
                UpdatedAt = fight.UpdatedAt,
                FightGifUrl = fight.FightGifUrl
            });
        }

        /// <summary>
        /// Get all fight schedules for a specific match (both users can see their fight history together)
        /// </summary>
        public async Task<IEnumerable<FightScheduleDto>> GetAllSchedulesForMatch(long matchId, Guid requestingUserId)
        {
            // First verify the requesting user is part of this match
            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.Id == matchId &&
                    (m.User1Id == requestingUserId || m.User2Id == requestingUserId));

            if (match == null)
            {
                throw new UnauthorizedAccessException("You are not a participant in this match");
            }

            // Get all fight schedules for this specific match
            var fights = await _context.FightSchedules
                .Include(fs => fs.Match)
                .Where(fs => fs.MatchId == matchId)
                .OrderByDescending(fs => fs.ScheduledDateTime)
                .ToListAsync();

            return fights.Select(fight => new FightScheduleDto
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
                CancellationReason = fight.CancellationReason ?? string.Empty,
                CanceledAt = fight.CanceledAt,
                CanceledByUserId = fight.CanceledByUserId,
                CreatedAt = fight.CreatedAt,
                UpdatedAt = fight.UpdatedAt,
                FightGifUrl = fight.FightGifUrl
            });
        }

        public async Task<ImageDto?> UploadFightGif(Guid userId, long fightScheduleId, IFormFile file)
        {
            try
            {
                // 1. Validate file size (max 10MB for GIFs)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                    throw new ArgumentException("File size cannot exceed 10MB");

                // 2. Validate file type (only GIF)
                var allowedExtensions = new[] { ".gif" };
                var allowedMimeTypes = new[] { "image/gif" };

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    throw new ArgumentException("Invalid file type. Only GIF files are allowed");

                if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                    throw new ArgumentException("Invalid file content type");

                // 3. Get fight schedule and validate
                var fight = await _context.FightSchedules
                    .Include(fs => fs.Match)
                    .FirstOrDefaultAsync(fs => fs.Id == fightScheduleId);

                if (fight == null)
                    throw new ArgumentException("Fight schedule not found");

                if (fight.Match == null)
                    throw new ArgumentException("Fight is not associated with a match");

                // 4. Verify user is part of the match
                if (fight.Match.User1Id != userId && fight.Match.User2Id != userId)
                    throw new UnauthorizedAccessException("You are not part of this match");

                // 5. Verify fight is completed
                if (fight.Status != "completed")
                    throw new ArgumentException("Can only upload GIF for completed fights");

                // 6. Create upload directory
                var uploadsPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "fight-gifs",
                    fightScheduleId.ToString()
                );

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // 7. Generate unique filename
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var uniqueFileName = $"fight_{timestamp}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // 8. Get old GIF path if exists
                string oldGifPath = null;
                if (!string.IsNullOrEmpty(fight.FightGifUrl) && fight.FightGifUrl.Contains("/uploads/fight-gifs/"))
                {
                    var oldFileName = Path.GetFileName(fight.FightGifUrl);
                    oldGifPath = Path.Combine(uploadsPath, oldFileName);
                }

                // 9. Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 10. Generate URL
                var fileUrl = $"/uploads/fight-gifs/{fightScheduleId}/{uniqueFileName}";

                // 11. Update fight schedule
                fight.FightGifUrl = fileUrl;
                fight.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // 12. Delete old GIF if exists
                if (!string.IsNullOrEmpty(oldGifPath) && System.IO.File.Exists(oldGifPath))
                {
                    try
                    {
                        System.IO.File.Delete(oldGifPath);
                    }
                    catch (Exception deleteEx)
                    {
                        Console.WriteLine($"Warning: Could not delete old fight GIF: {deleteEx.Message}");
                    }
                }

                return new ImageDto
                {
                    Message = "Fight GIF uploaded successfully",
                    ImageUrl = fileUrl,
                    FileName = uniqueFileName,
                    FileSize = file.Length
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading fight GIF: {ex.Message}");
                return new ImageDto
                {
                    Message = $"Error uploading fight GIF: {ex.Message}",
                    ImageUrl = string.Empty,
                    FileName = string.Empty,
                    FileSize = 0
                };
            }
        }

    }
}