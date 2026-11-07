using Lalamog.Api.DTOs;
using Lalamog.Api.Models;

namespace Lalamog.Api.Services
{
    internal static class DtoMapper
    {
        public static ProfileResponse ToProfileResponse(FighterProfile profile, bool includePrivateLocation = false)
        {
            string avatarUrl = $"/api/media/avatars/{profile.UserId:D}";

            return new ProfileResponse
            {
                UserId = profile.UserId,
                MapLocation = includePrivateLocation ? new MapLocation
                {
                    RegionCode = profile.RegionCode,
                    ProvinceCode = profile.ProvinceCode,
                    CityCode = profile.CityCode,
                    BarangayCode = profile.BarangayCode,
                    PsgcVersion = profile.PsgcVersion,
                    Latitude = profile.Latitude,
                    Longitude = profile.Longitude,
                } : null,

                Nickname = profile.Nickname,
                FighterName = profile.FighterName,
                Bio = profile.Bio,
                FightingStyle = profile.FightingStyle,
                ExperienceLevel = profile.ExperienceLevel,
                Location = profile.Location,
                Availability = profile.Availability,
                AvatarUrl = avatarUrl
            };
        }

        public static MessageResponse ToMessageResponse(ChatMessage message)
        {
            return new MessageResponse
            {
                Id = message.Id,
                MatchId = message.MatchId,
                SenderId = message.SenderId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };
        }

        public static FightResponse ToFightResponse(Fight fight)
        {
            var confirmations = new List<ConfirmationResponse>();
            bool user1Confirmed = false;
            bool user2Confirmed = false;

            foreach (FightConfirmation confirmation in fight.Confirmations)
            {
                if (confirmation.UserId == fight.Match.User1Id)
                {
                    user1Confirmed = true;
                }
                if (confirmation.UserId == fight.Match.User2Id)
                {
                    user2Confirmed = true;
                }

                confirmations.Add(new ConfirmationResponse
                {
                    UserId = confirmation.UserId,
                    EmergencyContactName = confirmation.EmergencyContactName,
                    EmergencyContactPhone = confirmation.EmergencyContactPhone,
                    AcceptedAt = confirmation.AcceptedAt
                });
            }

            return new FightResponse
            {
                Id = fight.Id,
                MatchId = fight.MatchId,
                User1Id = fight.Match.User1Id,
                User2Id = fight.Match.User2Id,
                ScheduledAt = fight.ScheduledAt,
                MapLocation = new MapLocation
                {
                    RegionCode = fight.RegionCode,
                    ProvinceCode = fight.ProvinceCode,
                    CityCode = fight.CityCode,
                    BarangayCode = fight.BarangayCode,
                    PsgcVersion = fight.PsgcVersion,
                    Latitude = fight.Latitude,
                    Longitude = fight.Longitude,
                },

                Location = fight.Location,
                Address = fight.Address,
                Status = fight.Status,
                User1Confirmed = user1Confirmed,
                User2Confirmed = user2Confirmed,
                CancellationReason = fight.CancellationReason,
                Confirmations = confirmations
            };
        }
    }
}
