using Lalamog.Api.Contracts;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
namespace Lalamog.Api.Services;

public class FightService(IFightRepository fights, IMatchingRepository transactions,
    MatchingService matches, TimeProvider clock)
{
    public static FightResponse ToResponse(Fight f) => new(f.Id, f.MatchId,
        f.Match.User1Id, f.Match.User2Id, f.ScheduledAt, f.Location, f.Address, f.Status,
        f.Confirmations.Any(c => c.UserId == f.Match.User1Id),
        f.Confirmations.Any(c => c.UserId == f.Match.User2Id), f.CancellationReason,
        f.Confirmations.Select(c => new ConfirmationResponse(c.UserId,
            c.EmergencyContactName, c.EmergencyContactPhone, c.AcceptedAt)).ToList());

    private async Task<Fight> RequireAsync(Guid actor, long id, CancellationToken ct)
    {
        var fight = await fights.GetAsync(id, ct);
        if (fight is null || !fight.Match.HasMember(actor)) throw new ApiException(404, "Session not found.");
        return fight;
    }
    public async Task<List<FightResponse>> ListAsync(Guid actor, CancellationToken ct) =>
        (await fights.ListAsync(actor, ct)).Select(ToResponse).ToList();
    public async Task<FightResponse> GetAsync(Guid actor, long id, CancellationToken ct) =>
        ToResponse(await RequireAsync(actor, id, ct));
    public Task<FightResponse> ScheduleAsync(Guid actor, ScheduleRequest request, CancellationToken ct) =>
        transactions.InTransactionAsync(async () =>
        {
            var match = await matches.RequireMemberAsync(request.MatchId, actor, ct);
            if (request.ScheduledAt <= clock.GetUtcNow()) throw new ApiException(400, "Choose a future date and time.");
            if (string.IsNullOrWhiteSpace(request.Location) || string.IsNullOrWhiteSpace(request.Address))
                throw new ApiException(400, "Enter the venue name and address.");
            if (await fights.HasUpcomingAsync(match.Id, ct))
                throw new ApiException(409, "This match already has an unfinished session.");
            var fight = new Fight
            {
                MatchId = match.Id, Match = match, ScheduledAt = request.ScheduledAt.ToUniversalTime(),
                Location = request.Location.Trim(), Address = request.Address.Trim(),
                CreatedAt = clock.GetUtcNow(), UpdatedAt = clock.GetUtcNow()
            };
            fights.Add(fight);
            await fights.SaveAsync(ct);
            return ToResponse(fight);
        }, ct);
    public Task<FightResponse> ConfirmAsync(Guid actor, long id, ConfirmRequest request, CancellationToken ct) =>
        transactions.InTransactionAsync(async () =>
        {
            var fight = await RequireAsync(actor, id, ct);
            if (fight.Status is not ("scheduled" or "confirmed")) throw new ApiException(409, "This session cannot be confirmed.");
            if (fight.ScheduledAt <= clock.GetUtcNow()) throw new ApiException(409, "This session has already started.");
            if (!request.Accepted || string.IsNullOrWhiteSpace(request.EmergencyContactName)
                || string.IsNullOrWhiteSpace(request.EmergencyContactPhone))
                throw new ApiException(400, "Accept the waiver and provide your emergency contact name and phone.");
            var confirmation = fight.Confirmations.SingleOrDefault(c => c.UserId == actor);
            if (confirmation is null)
            {
                confirmation = new FightConfirmation { FightId = fight.Id, UserId = actor };
                fight.Confirmations.Add(confirmation);
            }
            confirmation.EmergencyContactName = request.EmergencyContactName.Trim();
            confirmation.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
            confirmation.AcceptedAt = clock.GetUtcNow();
            if (fight.Confirmations.Count == 2) fight.Status = "confirmed";
            fight.UpdatedAt = clock.GetUtcNow();
            return ToResponse(fight);
        }, ct);
    public Task<FightResponse> CancelAsync(Guid actor, long id, CancelRequest request, CancellationToken ct) =>
        transactions.InTransactionAsync(async () =>
        {
            var fight = await RequireAsync(actor, id, ct);
            if (fight.Status is not ("scheduled" or "confirmed")) throw new ApiException(409, "This session cannot be canceled.");
            if (string.IsNullOrWhiteSpace(request.Reason)) throw new ApiException(400, "Enter a cancellation reason.");
            fight.Status = "canceled";
            fight.CancellationReason = request.Reason.Trim();
            fight.UpdatedAt = clock.GetUtcNow();
            return ToResponse(fight);
        }, ct);
    public Task<FightResponse> CompleteAsync(Guid actor, long id, CancellationToken ct) =>
        transactions.InTransactionAsync(async () =>
        {
            var fight = await RequireAsync(actor, id, ct);
            if (fight.Status != "confirmed") throw new ApiException(409, "Confirm the session before completing it.");
            if (fight.ScheduledAt > clock.GetUtcNow()) throw new ApiException(409, "Wait until the scheduled time.");
            fight.Status = "completed";
            fight.UpdatedAt = clock.GetUtcNow();
            return ToResponse(fight);
        }, ct);
}