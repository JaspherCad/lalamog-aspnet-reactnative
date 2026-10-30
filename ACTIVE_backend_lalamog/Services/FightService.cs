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

        public FightService(
            IFightRepository fightRepository,
            IMatchingRepository transactions,
            IMatchingService matchingService,
            TimeProvider clock)
        {
            _fightRepository = fightRepository;
            _transactions = transactions;
            _matchingService = matchingService;
            _clock = clock;
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
                if (string.IsNullOrWhiteSpace(request.Location) || string.IsNullOrWhiteSpace(request.Address))
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
                    Location = request.Location.Trim(),
                    Address = request.Address.Trim(),
                    CreatedAt = _clock.GetUtcNow(),
                    UpdatedAt = _clock.GetUtcNow()
                };
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
                if (fight.Status != "scheduled" && fight.Status != "confirmed")
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
