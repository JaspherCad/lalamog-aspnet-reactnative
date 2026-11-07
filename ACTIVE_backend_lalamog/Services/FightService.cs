using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;

namespace Lalamog.Api.Services
{
    public class FightService : IFightService
    {
        private readonly IFightRepository _fightRepository;
        private readonly IMatchingRepository _transactions;
        private readonly IMatchingService _matchingService;
        private readonly TimeProvider _clock;
        private readonly IGymRepository _gyms;
        private readonly IAreaService _areas;

        public FightService(
            IFightRepository fightRepository,
            IMatchingRepository transactions,
            IMatchingService matchingService,
            TimeProvider clock,
            IAreaService areas, IGymRepository gyms)
        {
            _fightRepository = fightRepository;
            _transactions = transactions;
            _matchingService = matchingService;
            _clock = clock;
            _areas = areas;
            _gyms = gyms;
        }

        public async Task<List<FightResponse>> ListAsync(Guid actor, CancellationToken cancellationToken)
        {
            List<Fight> fights = await _fightRepository.ListAsync(actor, cancellationToken);
            var responses = new List<FightResponse>();
            foreach (Fight fight in fights)
            {
                responses.Add(DtoMapper.ToFightResponse(fight));
            }
            return responses;
        }

        public async Task<FightResponse> GetAsync(Guid actor, long id, CancellationToken cancellationToken)
        {
            Fight fight = await RequireFightAsync(actor, id, cancellationToken);
            return DtoMapper.ToFightResponse(fight);
        }

        public async Task<FightResponse> ScheduleAsync(Guid actor, ScheduleRequest request, CancellationToken cancellationToken)
        {
            async Task<FightResponse> ScheduleInTransactionAsync()
            {
                FighterMatch match = await _matchingService.RequireMemberAsync(request.MatchId, actor, cancellationToken);
                if (request.ScheduledAt <= _clock.GetUtcNow())
                {
                    throw new ApiException(400, "Choose a future date and time.");
                }
                if (request.GymId == null && (string.IsNullOrWhiteSpace(request.Location) || string.IsNullOrWhiteSpace(request.Address)))
                {
                    throw new ApiException(400, "Enter the venue name and address.");
                }
                if (await _fightRepository.HasUpcomingAsync(match.Id, cancellationToken))
                {
                    throw new ApiException(409, "This match already has an unfinished session.");
                }

                var fight = new Fight
                {
                    MatchId = match.Id,
                    Match = match,
                    ScheduledAt = request.ScheduledAt.ToUniversalTime(),
                    Location = request.Location?.Trim() ?? "",
                    Address = request.Address?.Trim() ?? "",
                    CreatedAt = _clock.GetUtcNow(),
                    UpdatedAt = _clock.GetUtcNow()
                };
                if (request.GymId != null)
                {
                    Gym gym = await _gyms.GetAsync(request.GymId.Value, cancellationToken) ?? throw new ApiException(404, "Gym not found.");
                    if (!gym.IsPublished || gym.OwnerUserId == null || !await _gyms.CanManageGymsAsync(gym.OwnerUserId.Value, cancellationToken)) { throw new ApiException(409, "This gym is not accepting new bookings."); }
                    if (request.GymVersion != Convert.ToBase64String(gym.Version)) { throw new ApiException(409, "The gym quote changed. Refresh its details before booking."); }
                    if (!string.IsNullOrWhiteSpace(request.Location) || !string.IsNullOrWhiteSpace(request.Address) || request.MapLocation != null)
                    { throw new ApiException(400, "Select a gym or enter a private location, not both."); }
                    fight.GymId = gym.Id; fight.Gym = gym; fight.QuotedPrice = gym.SessionPrice;
                    fight.SessionDurationMinutes = gym.SessionDurationMinutes; fight.Status = "pending_approval";
                }
                else if (request.MapLocation != null)
                {
                    _areas.Validate(request.MapLocation);
                    fight.RegionCode = request.MapLocation.RegionCode;
                    fight.ProvinceCode = request.MapLocation.ProvinceCode;
                    fight.CityCode = request.MapLocation.CityCode;
                    fight.BarangayCode = request.MapLocation.BarangayCode;
                    fight.Latitude = request.MapLocation.Latitude;
                    fight.Longitude = request.MapLocation.Longitude;
                    fight.PsgcVersion = fight.RegionCode == null ? null : _areas.Version;
                }
                _fightRepository.Add(fight);
                await _fightRepository.SaveAsync(cancellationToken);
                return DtoMapper.ToFightResponse(fight);
            }

            return await _transactions.InTransactionAsync(ScheduleInTransactionAsync, cancellationToken);
        }

        public async Task<FightResponse> ConfirmAsync(Guid actor, long id, ConfirmRequest request, CancellationToken cancellationToken)
        {
            async Task<FightResponse> ConfirmInTransactionAsync()
            {
                Fight fight = await RequireFightAsync(actor, id, cancellationToken);
                if (fight.Status != "scheduled" && fight.Status != "confirmed")
                {
                    throw new ApiException(409, "This session cannot be confirmed.");
                }
                if (fight.ScheduledAt <= _clock.GetUtcNow())
                {
                    throw new ApiException(409, "This session has already started.");
                }
                if (!request.Accepted || string.IsNullOrWhiteSpace(request.EmergencyContactName)
                    || string.IsNullOrWhiteSpace(request.EmergencyContactPhone))
                {
                    throw new ApiException(400, "Accept the waiver and provide your emergency contact name and phone.");
                }

                var ownConfirmations = from candidate in fight.Confirmations
                                       where candidate.UserId == actor
                                       select candidate;
                FightConfirmation? confirmation = ownConfirmations.SingleOrDefault();
                if (confirmation == null)
                {
                    confirmation = new FightConfirmation { FightId = fight.Id, UserId = actor };
                    fight.Confirmations.Add(confirmation);
                }
                confirmation.EmergencyContactName = request.EmergencyContactName.Trim();
                confirmation.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
                confirmation.AcceptedAt = _clock.GetUtcNow();
                confirmation.AllowPublicIdentity = fight.GymId != null && request.AllowPublicIdentity;
                if (fight.Confirmations.Count == 2)
                {
                    fight.Status = "confirmed";
                }
                fight.UpdatedAt = _clock.GetUtcNow();
                return DtoMapper.ToFightResponse(fight);
            }

            return await _transactions.InTransactionAsync(ConfirmInTransactionAsync, cancellationToken);
        }

        public async Task<FightResponse> CancelAsync(Guid actor, long id, CancelRequest request, CancellationToken cancellationToken)
        {
            async Task<FightResponse> CancelInTransactionAsync()
            {
                Fight fight = await RequireFightAsync(actor, id, cancellationToken);
                if (fight.Status != "pending_approval" && fight.Status != "scheduled" && fight.Status != "confirmed")
                {
                    throw new ApiException(409, "This session cannot be canceled.");
                }
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    throw new ApiException(400, "Enter a cancellation reason.");
                }
                fight.Status = "canceled";
                fight.CancellationReason = request.Reason.Trim();
                fight.UpdatedAt = _clock.GetUtcNow();
                return DtoMapper.ToFightResponse(fight);
            }

            return await _transactions.InTransactionAsync(CancelInTransactionAsync, cancellationToken);
        }

        public async Task<FightResponse> CompleteAsync(Guid actor, long id, CancellationToken cancellationToken)
        {
            async Task<FightResponse> CompleteInTransactionAsync()
            {
                Fight fight = await RequireFightAsync(actor, id, cancellationToken);
                if (fight.GymId != null) { throw new ApiException(403, "The gym owner marks this booking completed after the session ends."); }
                if (fight.Status != "confirmed")
                {
                    throw new ApiException(409, "Confirm the session before completing it.");
                }
                if (fight.ScheduledAt > _clock.GetUtcNow())
                {
                    throw new ApiException(409, "Wait until the scheduled time.");
                }
                fight.Status = "completed";
                fight.UpdatedAt = _clock.GetUtcNow();
                return DtoMapper.ToFightResponse(fight);
            }

            return await _transactions.InTransactionAsync(CompleteInTransactionAsync, cancellationToken);
        }

        public async Task<FightResponse> PublicationAsync(Guid actor, long id, PublicationRequest request, CancellationToken cancellationToken)
        {
            async Task<FightResponse> SaveConsentAsync()
            {
                Fight fight = await RequireFightAsync(actor, id, cancellationToken);
                if (fight.GymId == null) { throw new ApiException(400, "Private arrangements are never published."); }
                FightConfirmation? own = (from item in fight.Confirmations where item.UserId == actor select item).SingleOrDefault();
                if (own == null) { throw new ApiException(409, "Submit your waiver before changing publication consent."); }
                own.AllowPublicIdentity = request.AllowPublicIdentity;
                fight.UpdatedAt = _clock.GetUtcNow();
                await _fightRepository.SaveAsync(cancellationToken);
                return DtoMapper.ToFightResponse(fight);
            }
            return await _transactions.InTransactionAsync(SaveConsentAsync, cancellationToken);
        }

        private async Task<Fight> RequireFightAsync(Guid actor, long id, CancellationToken cancellationToken)
        {
            Fight? fight = await _fightRepository.GetAsync(id, cancellationToken);
            if (fight == null || !fight.Match.HasMember(actor))
            {
                throw new ApiException(404, "Session not found.");
            }
            return fight;
        }
    }
}
