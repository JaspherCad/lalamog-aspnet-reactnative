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
    public class SwipeService : ISwipeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public SwipeService(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        //jane doe: f2603802-deb3-4377-8651-1134db7837a7
        //jaspher: 5c344aee-e3c1-4faf-846b-73cb28574d76
        //postman sample 
        // {
        //     "SwipeeId":"5c344aee-e3c1-4faf-846b-73cb28574d76",
        //     "Direction": "right"
        // }



        public async Task<SwipeResultDto> ProcessSwipeAsync(Guid swiperId, Guid swipeeId, string direction)
        {
            //if user swipe themselves throw error
            if (swiperId == swipeeId)
            {
                throw new ArgumentException("You cannot swipe yourself.");
            }

            //if swipe is already existing: probably swiped left initially BUT wanted to swipe right after that day
            var existingSwipe = await _context.SwipeActions
                .FirstOrDefaultAsync(swipeActions => swipeActions.SwiperId == swiperId && swipeActions.SwipeeId == swipeeId);

            //if existing is true just update the input
            if (existingSwipe != null)
            {
                existingSwipe.Direction = direction;
                existingSwipe.CreatedAt = DateTime.UtcNow;
                _context.SwipeActions.Update(existingSwipe);
            }
            else
            {

                var swipeAction = new SwipeAction
                {
                    SwiperId = swiperId,
                    SwipeeId = swipeeId,
                    Direction = direction,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.SwipeActions.AddAsync(swipeAction);
            }

            await _context.SaveChangesAsync();
            //#info: after INSERT on "SwipeActions" table.
            //#note: simple pseudocode: this will check if both swiper liked each other then if confirmed: it will sort the swiperId / swipeeId lexicographically THEN INSERT into matches table so that MATCH TABLE HAVE ""ALPHABETICAL"" arrangement.

            // $ sql:
            // CREATE TRIGGER on_swipe_action 
            // AFTER INSERT ON public."SwipeActions" FOR EACH ROW EXECUTE FUNCTION check_mutual_swipe()


            //             CREATE OR REPLACE FUNCTION public.check_mutual_swipe()
            //  RETURNS trigger
            //  LANGUAGE plpgsql
            // AS $function$
            // DECLARE
            //     user1 uuid;
            //     user2 uuid;
            //         BEGIN
            //     -- Only act on right‐swipes
            //     IF NEW."Direction" <> 'right' THEN
            //         RETURN NEW;
            //     END IF;

            //     -- Sort the two UUIDs lexicographically via text
            //     IF NEW."SwiperId"::text<NEW."SwipeeId"::text THEN
            //         user1 := NEW."SwiperId";
            //         user2 := NEW."SwipeeId";
            //     ELSE
            //         user1 := NEW."SwipeeId";
            //         user2 := NEW."SwiperId";
            //     END IF;

            //     -- Check if the other party already swiped right
            //     IF EXISTS(
            //         SELECT 1

            //             FROM "SwipeActions"

            //             WHERE "SwiperId" = NEW."SwipeeId" (the person i like)

            //                AND "SwipeeId" = NEW."SwiperId" (me)

            //                AND "Direction" = 'right'
            //     ) 
            //      THEN
            //         INSERT INTO "Matches" ("User1Id", "User2Id", "Status", "CreatedAt")

            //         VALUES(user1, user2, 'active', NOW())  --lexicographed
            //         ON CONFLICT("User1Id", "User2Id") DO NOTHING;
            //         END IF;

            //         RETURN NEW;
            //         END;
            // $function$


            // #end-info: there is a trigger on the database that automatically creates a match if both users swipe right on each other lexicographically








            //check for match:
            if (direction == "right")
            {
                var a = swiperId.ToString();
                var b = swipeeId.ToString();
                var userId1 = string.CompareOrdinal(a, b) < 0 ? swiperId : swipeeId;
                var userId2 = string.CompareOrdinal(a, b) < 0 ? swipeeId : swiperId;

                //why lexicog? 
                // because we want to avoid duplicate matches and UNSEEN MATCHES TABLE, so we always store the smaller ID first then bigger

                // a -> b is not equals to b -> a;
                // but a -> b is equals to arranged(b -> a) 


                var match = await _context.Matches
                    .FirstOrDefaultAsync(m => m.User1Id == userId1 && m.User2Id == userId2);

                if (match != null)
                {
                

                    return new SwipeResultDto
                    {
                        Swiper = await _userService.GetProfileAsync(swiperId),
                        IsMatch = true,
                        MatchId = match.Id,
                        Message = "It's a match!",
                        MatchedUser = await _userService.GetProfileAsync(swipeeId)
                    };
                }
                else
                {
                    // no match found, just return the swiped profile and message
                    return new SwipeResultDto
                    {
                        Swiper = await _userService.GetProfileAsync(swiperId),
                        IsMatch = false,
                        MatchId = null,
                        Message = "Swipe processed successfully.",
                        MatchedUser = null
                    };
                }
            }
            // in frontend if matchId is null, it means no match was found
            // if match is not found, just return the swiped profile and message

            return new SwipeResultDto
            {
                Swiper = await _userService.GetProfileAsync(swiperId),
                IsMatch = false,
                MatchId = null,
                Message = "Swipe processed successfully.",
                MatchedUser = null
            };
        }


        public async Task<List<ProfileDto>> FetchAllAvailableProfilesAsync(Guid userId)
        {

            //simplyfy things: I WANT TO HIDE ALL THE USER THAT I HAVE SWIPED RIGHT OR MATCHED WITH

            // 1: get the IDS from SwipeActions where the user swiped right
            var swipedRightIds = _context.SwipeActions
                .Where(s => s.SwiperId == userId && s.Direction == "right")
                .Select(s => s.SwipeeId);
            //returns the ids of the users that the user has swiped right on

            //get all profiles from the match table that are matched with the user
            var matchedUserIds = _context.Matches
                .Where(m => m.Status == "active" &&
                    (m.User1Id == userId || m.User2Id == userId))
                .Select(m => m.User1Id == userId
                     ? m.User2Id
                     : m.User1Id);
            //returns the ids of the users that the user has swiped right on


            //combine the ids to exclude
            var excludeIds = swipedRightIds.Union(matchedUserIds).ToList();


            var availableProfiles = await _context.Profiles
                .Include(p => p.Availability) //Availability is different table (bruh!)
                .Where(p => p.UserId != userId
                    && !excludeIds.Contains(p.UserId))
                .ToListAsync(); //await requires here since this is actual DB work... not in memory work




            // Project (from real sql) to DTO (in memory) to avoid geography/geometry SQL issues
            var availables = availableProfiles.Select(p => new ProfileDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Nickname = p.Nickname,
                Bio = p.Bio,
                FightingStyle = p.FightingStyle,
                ExperienceLevel = p.ExperienceLevel,
                ProfilePictureUrl = p.ProfilePictureUrl,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Location = p.Location != null
                    ? new LocationDto { X = p.Location.X, Y = p.Location.Y }
                    : null,

                // Convert Model Availability to DTO Availability
                Availability = p.Availability != null
                    ? new DTOs.Availability
                    {
                        Days = p.Availability.Days,
                        Time = p.Availability.Time
                    }
                    : null
            }).ToList();



            return availables; //ProfileDto
        }


        //get all matches for user
        public async Task<List<MatchDto>> GetAllMatchesForUser(Guid userId)
        {
            var matchedResults = await _context.Matches
                .Where(m => m.Status == "active" &&
                    (m.User1Id == userId || m.User2Id == userId))
                .ToListAsync();

            var matchDtos = new List<MatchDto>();


            foreach (var match in matchedResults)
            {
                var user1Profile = await _userService.GetProfileAsync(match.User1Id);
                var user2Profile = await _userService.GetProfileAsync(match.User2Id);

                matchDtos.Add(new MatchDto
                {
                    User1 = user1Profile,
                    User2 = user2Profile,
                    CreatedAt = match.CreatedAt,
                    Id = match.Id,
                    Status = match.Status
                });
            }





            return matchDtos;
        }


        public async Task<List<ProfileDto>> GetMatchedUserOfUserWithID(Guid userId)
        {
            // Use the Match table directly since PostgreSQL triggers already handle match creation
            var matchedUserIds = await _context.Matches
                .Where(m => m.Status == "active" &&
                    (m.User1Id == userId || m.User2Id == userId))
                // if User1Id == myId then select User2Id else User1Id
                .Select(m => m.User1Id == userId ? m.User2Id : m.User1Id)
                .ToListAsync();

            if (matchedUserIds.Count == 0)
            {
                return new List<ProfileDto>();
            }

            // since matchedUserIds are just user IDs, we need to fetch the profiles to get actual PROFILE data
            // and not just the IDs


            var profileEntities = await _context.Profiles
                .Include(p => p.Availability)
                // get only the profiles that match the matchedUserIds
                .Where(p => matchedUserIds.Contains(p.UserId))
                .ToListAsync();

            // Project to DTO in memory to avoid geography/geometry SQL issues
            var profiles = profileEntities.Select(p => new ProfileDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Nickname = p.Nickname,
                Bio = p.Bio,
                FightingStyle = p.FightingStyle,
                ExperienceLevel = p.ExperienceLevel,
                ProfilePictureUrl = p.ProfilePictureUrl,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Location = p.Location != null
                ? new LocationDto { X = p.Location.X, Y = p.Location.Y }
                : null,

                // Convert Model Availability to DTO Availability
                Availability = p.Availability != null
                ? new DTOs.Availability
                {
                    Days = p.Availability.Days,
                    Time = p.Availability.Time
                }
                : null
            }).ToList();

            return profiles;
        }


        public async Task<SwipeProfilesDto> FetchAllProfilesAsync(Guid userId)
        {
            // Get matched profiles
            var matchedProfiles = await GetMatchedUserOfUserWithID(userId);

            // Get available profiles (excluding matched and swiped right)
            var availableProfiles = await FetchAllAvailableProfilesAsync(userId);

            return new SwipeProfilesDto
            {
                MatchedProfiles = matchedProfiles,
                AvailableProfiles = availableProfiles
            };
        }




    }
}

