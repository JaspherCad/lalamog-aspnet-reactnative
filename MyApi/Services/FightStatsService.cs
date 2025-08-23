using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.DTOs;
using MyApi.Interfaces;

namespace MyApi.Services
{
    public class FightStatsService : IFightStatsService
    {
        private readonly ApplicationDbContext _context;

        public FightStatsService(ApplicationDbContext context)
        {
            _context = context;
        }

       
        public async Task<bool> RecordFightResultAsync(FightResultDto resultDto, Guid recordedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get the fight schedule with related data
                // (get profile, and match info)
                //https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager
                var fightSchedule = await _context.FightSchedules

                    .Include(fs => fs.Match)
                    //inside match get profile thru aspNetUser
                    .ThenInclude(m => m.User1)
                    .ThenInclude(u => u.Profile)



                    .Include(fs => fs.Match)

                    .ThenInclude(m => m.User2)
                    .ThenInclude(u => u.Profile)
                    .FirstOrDefaultAsync(fs => fs.Id == resultDto.FightScheduleId);

                //#sql (note: we have no profile on match, only AspUser)
                //                 SELECT*
                // FROM "FightSchedules" fs
                // INNER JOIN "Matches" m ON fs."MatchId" = m."Id"
                // INNER JOIN "AspNetUsers" u1 ON m."User1Id" = u1."Id"
                // LEFT JOIN "Profiles" p1 ON u1."Id" = p1."UserId"
                // INNER JOIN "AspNetUsers" u2 ON m."User2Id" = u2."Id"
                // LEFT JOIN "Profiles" p2 ON u2."Id" = p2."UserId"
                // WHERE fs."Id" = @fightScheduleId;
                //#end-sql

                if (fightSchedule == null)
                    return false;

                // Validate fight can have result recorded
                if (fightSchedule.Status != "completed")
                    return false;

                // Check if result already recorded
                if (fightSchedule.ResultRecordedAt != null)
                    return false;

                // Validate user is participant
                var user1Id = fightSchedule.Match.User1Id;
                var user2Id = fightSchedule.Match.User2Id;

                if (recordedByUserId != user1Id && recordedByUserId != user2Id)
                    return false;

                // Validate winner ID matches result
                if (!ValidateWinnerAndResult(resultDto.WinnerId, resultDto.FightResult, user1Id, user2Id))
                    return false;

                // Store ratings before update
                var user1Profile = fightSchedule.Match.User1.Profile!;
                var user2Profile = fightSchedule.Match.User2.Profile!;

                fightSchedule.User1RatingBefore = user1Profile.Rating;
                fightSchedule.User2RatingBefore = user2Profile.Rating;

                // Update fight schedule with result
                fightSchedule.FightResult = resultDto.FightResult;
                fightSchedule.WinnerId = resultDto.WinnerId;
                fightSchedule.WinMethod = resultDto.WinMethod;
                fightSchedule.FightDurationMinutes = resultDto.FightDurationMinutes;
                fightSchedule.ResultNotes = resultDto.ResultNotes;
                fightSchedule.ResultRecordedAt = DateTime.UtcNow;
                fightSchedule.ResultRecordedByUserId = recordedByUserId;

                // Store feedback if provided
                fightSchedule.User1Rating = resultDto.User1Rating;
                fightSchedule.User2Rating = resultDto.User2Rating;
                fightSchedule.User1Feedback = resultDto.User1Feedback;
                fightSchedule.User2Feedback = resultDto.User2Feedback;

                await UpdateUserStats(user1Id, user2Id, resultDto.FightResult);

                // Calculate and update Elo ratings
                var (newUser1Rating, newUser2Rating) = CalculateEloRatings(
                    user1Profile.Rating, user2Profile.Rating, resultDto.FightResult);

                user1Profile.Rating = newUser1Rating;
                user2Profile.Rating = newUser2Rating;

                fightSchedule.User1RatingAfter = newUser1Rating;
                fightSchedule.User2RatingAfter = newUser2Rating;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

   
        private bool ValidateWinnerAndResult(Guid? winnerId, string fightResult, Guid user1Id, Guid user2Id)
        {
            return fightResult switch
            {
                "user1_win" => winnerId == user1Id,
                "user2_win" => winnerId == user2Id,
                "draw" or "no_contest" => winnerId == null,
                _ => false
            };
        }

        private async Task UpdateUserStats(Guid user1Id, Guid user2Id, string fightResult)
        {
            var profiles = await _context.Profiles
                .Where(p => p.UserId == user1Id || p.UserId == user2Id)
                .ToListAsync();

            var user1Profile = profiles.First(p => p.UserId == user1Id);
            var user2Profile = profiles.First(p => p.UserId == user2Id);

            // Update total fights
            user1Profile.TotalFights++;
            user2Profile.TotalFights++;

            // Update win/loss/draw counts and streaks
            switch (fightResult)
            {
                case "user1_win":
                    user1Profile.TotalWins++;
                    user2Profile.TotalLosses++;
                    UpdateStreaks(user1Profile, true);  // User1 won
                    UpdateStreaks(user2Profile, false); // User2 lost
                    break;

                case "user2_win":
                    user2Profile.TotalWins++;
                    user1Profile.TotalLosses++;
                    UpdateStreaks(user2Profile, true);  // User2 won
                    UpdateStreaks(user1Profile, false); // User1 lost
                    break;

                case "draw":
                    user1Profile.TotalDraws++;
                    user2Profile.TotalDraws++;
                    // Draws reset current streaks but don't start new ones
                    user1Profile.CurrentWinStreak = 0;
                    user1Profile.CurrentLossStreak = 0;
                    user2Profile.CurrentWinStreak = 0;
                    user2Profile.CurrentLossStreak = 0;
                    break;

                case "no_contest":
                    // No contest doesn't affect win/loss stats, only total fights
                    // Reset streaks
                    user1Profile.CurrentWinStreak = 0;
                    user1Profile.CurrentLossStreak = 0;
                    user2Profile.CurrentWinStreak = 0;
                    user2Profile.CurrentLossStreak = 0;
                    break;
            }

            user1Profile.UpdatedAt = DateTime.UtcNow;
            user2Profile.UpdatedAt = DateTime.UtcNow;
        }

        
        private void UpdateStreaks(Profile profile, bool won)
        {
            if (won)
            {
                profile.CurrentWinStreak++;
                profile.CurrentLossStreak = 0;

                if (profile.CurrentWinStreak > profile.BestWinStreak)
                {
                    profile.BestWinStreak = profile.CurrentWinStreak;
                }
            }
            else
            {
                profile.CurrentWinStreak = 0;
                profile.CurrentLossStreak++;
            }
        }

       
        private (int newUser1Rating, int newUser2Rating) CalculateEloRatings(int user1Rating, int user2Rating, string fightResult)
        {
            const int K_FACTOR = 32; // Standard Elo K-factor

            // Calculate expected scores
            var expectedUser1 = 1.0 / (1.0 + Math.Pow(10, (user2Rating - user1Rating) / 400.0));
            var expectedUser2 = 1.0 - expectedUser1;

            // Determine actual scores
            double actualUser1Score, actualUser2Score;
            switch (fightResult)
            {
                case "user1_win":
                    actualUser1Score = 1.0;
                    actualUser2Score = 0.0;
                    break;
                case "user2_win":
                    actualUser1Score = 0.0;
                    actualUser2Score = 1.0;
                    break;
                case "draw":
                    actualUser1Score = 0.5;
                    actualUser2Score = 0.5;
                    break;
                default: // no_contest
                    return (user1Rating, user2Rating); // No rating change
            }

            // Calculate new ratings
            var newUser1Rating = (int)Math.Round(user1Rating + K_FACTOR * (actualUser1Score - expectedUser1));
            var newUser2Rating = (int)Math.Round(user2Rating + K_FACTOR * (actualUser2Score - expectedUser2));

            // Ensure ratings don't go below minimum
            const int MIN_RATING = 100;
            newUser1Rating = Math.Max(newUser1Rating, MIN_RATING);
            newUser2Rating = Math.Max(newUser2Rating, MIN_RATING);

            return (newUser1Rating, newUser2Rating);
        }

      
        public async Task<UserStatsDto?> GetUserStatsAsync(Guid userId)
        {
            var profile = await _context.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return null;

            return new UserStatsDto
            {
                UserId = userId,
                Nickname = profile.Nickname ?? profile.User!.UserName ?? "Unknown",
                ProfilePictureUrl = profile.ProfilePictureUrl,
                TotalWins = profile.TotalWins,
                TotalLosses = profile.TotalLosses,
                TotalDraws = profile.TotalDraws,
                TotalFights = profile.TotalFights,
                WinPercentage = profile.WinPercentage,
                Rating = profile.Rating,
                CurrentWinStreak = profile.CurrentWinStreak,
                CurrentLossStreak = profile.CurrentLossStreak,
                BestWinStreak = profile.BestWinStreak,
                ExperienceLevel = profile.ExperienceLevel,
                FightingStyle = profile.FightingStyle
            };
        }

       







       
        public async Task<HeadToHeadStatsDto?> GetHeadToHeadStatsAsync(Guid userId1, Guid userId2, Guid requestingUserId)
        {
            // Get the match between these two users
            var match = await _context.Matches
                .Include(m => m.User1).ThenInclude(u => u.Profile)
                .Include(m => m.User2).ThenInclude(u => u.Profile)
                .Include(m => m.FightSchedules.Where(fs => fs.Status == "completed" && fs.ResultRecordedAt != null))
                .FirstOrDefaultAsync(m =>
                    (m.User1Id == userId1 && m.User2Id == userId2) ||
                    (m.User1Id == userId2 && m.User2Id == userId1));

            if (match == null)
            {
                // No fights between these users yet
                var users = await _context.Users
                    .Include(u => u.Profile)
                    .Where(u => u.Id == userId1 || u.Id == userId2)
                    .ToListAsync();

                var user1 = users.First(u => u.Id == userId1);
                var user2 = users.First(u => u.Id == userId2);

                return new HeadToHeadStatsDto
                {
                    User1Id = userId1,
                    User2Id = userId2,
                    User1Nickname = user1.Profile?.Nickname ?? user1.UserName ?? "Unknown",
                    User2Nickname = user2.Profile?.Nickname ?? user2.UserName ?? "Unknown",
                    User1ProfilePicture = user1.Profile?.ProfilePictureUrl,
                    User2ProfilePicture = user2.Profile?.ProfilePictureUrl,
                    TotalFights = 0,
                    MyWins = 0,
                    MyLosses = 0,
                    MyWinPercentage = 0,
                    LastFightResult = "No fights yet"
                };
            }

            // Calculate head-to-head stats
            var completedFights = match.FightSchedules.ToList();
            var user1Wins = completedFights.Count(f => f.WinnerId == match.User1Id);
            var user2Wins = completedFights.Count(f => f.WinnerId == match.User2Id);
            var draws = completedFights.Count(f => f.FightResult == "draw");

            // From requesting user's perspective
            var myWins = requestingUserId == match.User1Id ? user1Wins : user2Wins;
            var myLosses = requestingUserId == match.User1Id ? user2Wins : user1Wins;

            var latestFight = completedFights
                .OrderByDescending(f => f.ScheduledDateTime)
                .FirstOrDefault();

            string lastFightResult = "No fights yet";
            if (latestFight != null)
            {
                if (latestFight.WinnerId == requestingUserId)
                    lastFightResult = "Win";
                else if (latestFight.WinnerId != null)
                    lastFightResult = "Loss";
                else
                    lastFightResult = "Draw";
            }

            return new HeadToHeadStatsDto
            {
                User1Id = match.User1Id,
                User2Id = match.User2Id,
                User1Nickname = match.User1.Profile?.Nickname ?? match.User1.UserName ?? "Unknown",
                User2Nickname = match.User2.Profile?.Nickname ?? match.User2.UserName ?? "Unknown",
                User1ProfilePicture = match.User1.Profile?.ProfilePictureUrl,
                User2ProfilePicture = match.User2.Profile?.ProfilePictureUrl,
                User1Wins = user1Wins,
                User2Wins = user2Wins,
                Draws = draws,
                TotalFights = completedFights.Count,
                MyWins = myWins,
                MyLosses = myLosses,
                MyWinPercentage = completedFights.Count > 0 ? (double)myWins / completedFights.Count * 100 : 0,
                LastFightDate = latestFight?.ScheduledDateTime,
                LastWinnerId = latestFight?.WinnerId,
                LastWinMethod = latestFight?.WinMethod,
                LastFightResult = lastFightResult
            };
        }

        
        public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int page = 1, int pageSize = 50, string? fightingStyle = null, int? experienceLevel = null)
        {
            var query = _context.Profiles
                .Include(p => p.User)
                .Where(p => p.TotalFights > 0); // Only include users who have fought

            if (!string.IsNullOrEmpty(fightingStyle))
            {
                query = query.Where(p => p.FightingStyle == fightingStyle);
            }

            if (experienceLevel.HasValue)
            {
                query = query.Where(p => p.ExperienceLevel == experienceLevel.Value);
            }

            var profiles = await query
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.TotalWins)
                .ThenBy(p => p.TotalLosses)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return profiles.Select((profile, index) => new LeaderboardEntryDto
            {
                Rank = (page - 1) * pageSize + index + 1,
                UserId = profile.UserId,
                Nickname = profile.Nickname ?? profile.User!.UserName ?? "Unknown",
                ProfilePictureUrl = profile.ProfilePictureUrl,
                Rating = profile.Rating,
                TotalWins = profile.TotalWins,
                TotalLosses = profile.TotalLosses,
                TotalFights = profile.TotalFights,
                WinPercentage = profile.WinPercentage,
                CurrentWinStreak = profile.CurrentWinStreak,
                FightingStyle = profile.FightingStyle,
                ExperienceLevel = profile.ExperienceLevel
            }).ToList();
        }

       
        public async Task<List<FightHistoryDto>> GetUserFightHistoryAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var fightHistory = await _context.FightSchedules
                .Include(fs => fs.Match)
                .ThenInclude(m => m.User1)
                .ThenInclude(u => u.Profile)
                .Include(fs => fs.Match)
                .ThenInclude(m => m.User2)
                .ThenInclude(u => u.Profile)
                .Where(fs => (fs.Match.User1Id == userId || fs.Match.User2Id == userId)
                           && fs.Status == "completed"
                           && fs.ResultRecordedAt != null)
                .OrderByDescending(fs => fs.ScheduledDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return fightHistory.Select(fs =>
            {
                var isUser1 = fs.Match.User1Id == userId;
                var opponent = isUser1 ? fs.Match.User2 : fs.Match.User1;
                var myRatingBefore = isUser1 ? fs.User1RatingBefore : fs.User2RatingBefore;
                var myRatingAfter = isUser1 ? fs.User1RatingAfter : fs.User2RatingAfter;
                var myFeedback = isUser1 ? fs.User1Feedback : fs.User2Feedback;
                var opponentFeedback = isUser1 ? fs.User2Feedback : fs.User1Feedback;
                var myRating = isUser1 ? fs.User1Rating : fs.User2Rating;
                var opponentRating = isUser1 ? fs.User2Rating : fs.User1Rating;

                string result = "Draw";
                if (fs.FightResult == "no_contest")
                    result = "No Contest";
                else if (fs.WinnerId == userId)
                    result = "Win";
                else if (fs.WinnerId != null)
                    result = "Loss";

                return new FightHistoryDto
                {
                    FightScheduleId = fs.Id,
                    FightDate = fs.ScheduledDateTime,
                    OpponentId = opponent.Id,
                    OpponentNickname = opponent.Profile?.Nickname ?? opponent.UserName ?? "Unknown",
                    OpponentProfilePicture = opponent.Profile?.ProfilePictureUrl,
                    Result = result,
                    WinMethod = fs.WinMethod,
                    FightDurationMinutes = fs.FightDurationMinutes,
                    LocationName = fs.LocationName,
                    LocationAddress = fs.LocationAddress,
                    RatingBefore = myRatingBefore,
                    RatingAfter = myRatingAfter,
                    RatingChange = myRatingAfter - myRatingBefore,
                    MyFeedback = myFeedback,
                    OpponentFeedback = opponentFeedback,
                    MyRating = myRating,
                    OpponentRating = opponentRating
                };
            }).ToList();
        }

        //#note: Validates if a user can record a fight result
        public async Task<bool> ValidateFightResultAsync(long fightScheduleId, Guid userId)
        {
            // fight schedule innerjoin match
            var fightSchedule = await _context.FightSchedules
                .Include(fs => fs.Match)
                .FirstOrDefaultAsync(fs => fs.Id == fightScheduleId);

            return fightSchedule != null
                   && fightSchedule.Status == "completed"
                   && (fightSchedule.Match.User1Id == userId || fightSchedule.Match.User2Id == userId)
                   && fightSchedule.ResultRecordedAt == null; // Result not yet recorded
        }
    }
}
