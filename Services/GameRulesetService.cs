using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Services;

public interface IGameRulesetService
{
    Task<IReadOnlyList<GameRulesetSummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MatchmakingModeRulesetDto>> ListModeAssignmentsAsync(CancellationToken cancellationToken = default);
    Task<GameRulesDto?> GetAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<GameRulesDto> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<ResolvedGameRules> ResolveAsync(string? requestedRulesetId, CancellationToken cancellationToken = default);
    Task<ResolvedGameRules> ResolveForModeAsync(QueueMode mode, CancellationToken cancellationToken = default);
    Task<GameRulesDto> CreateAsync(UpsertGameRulesetRequest request, CancellationToken cancellationToken = default);
    Task<GameRulesDto?> UpdateAsync(string rulesetId, UpsertGameRulesetRequest request, CancellationToken cancellationToken = default);
    Task<GameRulesDto?> ActivateAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<MatchmakingModeRulesetDto?> AssignModeAsync(QueueMode mode, string rulesetId, CancellationToken cancellationToken = default);
}

public sealed class GameRulesetService(AppDbContext dbContext) : IGameRulesetService
{
    public async Task<IReadOnlyList<GameRulesetSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.GameRulesets
            .AsNoTracking()
            .OrderByDescending(ruleset => ruleset.IsDefault)
            .ThenBy(ruleset => ruleset.DisplayName)
            .Select(ruleset => new GameRulesetSummaryDto(
                ruleset.Id,
                ruleset.RulesetKey,
                ruleset.DisplayName,
                ruleset.IsActive,
                ruleset.IsDefault,
                ruleset.CreatedAt,
                ruleset.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MatchmakingModeRulesetDto>> ListModeAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MatchmakingModeRulesetAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Ruleset)
            .OrderBy(assignment => assignment.Mode)
            .Select(assignment => new MatchmakingModeRulesetDto(
                assignment.Mode,
                assignment.RulesetId,
                assignment.Ruleset!.RulesetKey,
                assignment.Ruleset.DisplayName,
                assignment.Ruleset.IsActive,
                assignment.CreatedAt,
                assignment.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<GameRulesDto?> GetAsync(string rulesetId, CancellationToken cancellationToken = default)
    {
        var entity = await LoadRulesetQuery()
            .FirstOrDefaultAsync(ruleset => ruleset.Id == rulesetId, cancellationToken);

        return entity == null ? null : GameRules.FromEntity(entity).ToDto();
    }

    public async Task<GameRulesDto> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        var entity = await LoadRulesetQuery()
            .FirstOrDefaultAsync(ruleset => ruleset.IsDefault && ruleset.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("No active default ruleset is configured.");

        return GameRules.FromEntity(entity).ToDto();
    }

    public async Task<ResolvedGameRules> ResolveAsync(string? requestedRulesetId, CancellationToken cancellationToken = default)
    {
        var entity = requestedRulesetId == null
            ? await LoadRulesetQuery()
                .FirstOrDefaultAsync(ruleset => ruleset.IsDefault && ruleset.IsActive, cancellationToken)
            : await LoadRulesetQuery()
                .FirstOrDefaultAsync(ruleset => ruleset.Id == requestedRulesetId && ruleset.IsActive, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException(requestedRulesetId == null
                ? "No active default ruleset is configured."
                : $"Ruleset '{requestedRulesetId}' was not found or is not active.");
        }

        var rules = GameRules.FromEntity(entity);
        return new ResolvedGameRules(entity.Id, entity.DisplayName, rules, rules.ToSnapshotJson());
    }

    public async Task<ResolvedGameRules> ResolveForModeAsync(QueueMode mode, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.MatchmakingModeRulesetAssignments
            .AsNoTracking()
            .Include(current => current.Ruleset)
            .FirstOrDefaultAsync(current => current.Mode == mode, cancellationToken);

        if (assignment?.Ruleset == null || !assignment.Ruleset.IsActive)
        {
            throw new InvalidOperationException($"No active ruleset is assigned for matchmaking mode '{mode}'.");
        }

        var rules = GameRules.FromEntity(assignment.Ruleset);
        return new ResolvedGameRules(assignment.Ruleset.Id, assignment.Ruleset.DisplayName, rules, rules.ToSnapshotJson());
    }

    public async Task<GameRulesDto> CreateAsync(UpsertGameRulesetRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        if (await dbContext.GameRulesets.AnyAsync(ruleset => ruleset.RulesetKey == request.RulesetKey, cancellationToken))
        {
            throw new InvalidOperationException($"A ruleset with key '{request.RulesetKey}' already exists.");
        }

        var entity = new GameRuleset();
        Apply(entity, request);
        dbContext.GameRulesets.Add(entity);
        await NormalizeDefaultStateAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return GameRules.FromEntity(entity).ToDto();
    }

    public async Task<GameRulesDto?> UpdateAsync(string rulesetId, UpsertGameRulesetRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var entity = await dbContext.GameRulesets
            .Include(ruleset => ruleset.SeatOverrides)
            .FirstOrDefaultAsync(ruleset => ruleset.Id == rulesetId, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        if (!string.Equals(entity.RulesetKey, request.RulesetKey, StringComparison.OrdinalIgnoreCase)
            && await dbContext.GameRulesets.AnyAsync(ruleset => ruleset.RulesetKey == request.RulesetKey && ruleset.Id != rulesetId, cancellationToken))
        {
            throw new InvalidOperationException($"A ruleset with key '{request.RulesetKey}' already exists.");
        }

        Apply(entity, request);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await NormalizeDefaultStateAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return GameRules.FromEntity(entity).ToDto();
    }

    public async Task<GameRulesDto?> ActivateAsync(string rulesetId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.GameRulesets
            .Include(ruleset => ruleset.SeatOverrides)
            .FirstOrDefaultAsync(ruleset => ruleset.Id == rulesetId, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        entity.IsActive = true;
        entity.IsDefault = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await NormalizeDefaultStateAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return GameRules.FromEntity(entity).ToDto();
    }

    public async Task<MatchmakingModeRulesetDto?> AssignModeAsync(QueueMode mode, string rulesetId, CancellationToken cancellationToken = default)
    {
        var ruleset = await dbContext.GameRulesets
            .FirstOrDefaultAsync(current => current.Id == rulesetId, cancellationToken);

        if (ruleset == null)
        {
            return null;
        }

        if (!ruleset.IsActive)
        {
            throw new InvalidOperationException("Cannot assign an inactive ruleset to a matchmaking mode.");
        }

        var assignment = await dbContext.MatchmakingModeRulesetAssignments
            .Include(current => current.Ruleset)
            .FirstOrDefaultAsync(current => current.Mode == mode, cancellationToken);

        if (assignment == null)
        {
            assignment = new MatchmakingModeRulesetAssignment
            {
                Mode = mode,
                RulesetId = ruleset.Id
            };
            dbContext.MatchmakingModeRulesetAssignments.Add(assignment);
        }
        else
        {
            assignment.RulesetId = ruleset.Id;
            assignment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MatchmakingModeRulesetDto(
            mode,
            ruleset.Id,
            ruleset.RulesetKey,
            ruleset.DisplayName,
            ruleset.IsActive,
            assignment.CreatedAt,
            assignment.UpdatedAt);
    }

    private IQueryable<GameRuleset> LoadRulesetQuery()
    {
        return dbContext.GameRulesets
            .AsNoTracking()
            .Include(ruleset => ruleset.SeatOverrides);
    }

    private async Task NormalizeDefaultStateAsync(GameRuleset targetRuleset, CancellationToken cancellationToken)
    {
        if (targetRuleset.IsDefault)
        {
            var otherDefaults = await dbContext.GameRulesets
                .Where(ruleset => ruleset.Id != targetRuleset.Id && ruleset.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var otherDefault in otherDefaults)
            {
                otherDefault.IsDefault = false;
                otherDefault.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    private static void Apply(GameRuleset entity, UpsertGameRulesetRequest request)
    {
        entity.RulesetKey = request.RulesetKey.Trim();
        entity.DisplayName = request.DisplayName.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.IsActive = request.IsActive;
        entity.IsDefault = request.IsDefault;
        entity.StartingHeroHealth = request.StartingHeroHealth;
        entity.MaxHeroHealth = request.MaxHeroHealth;
        entity.StartingMana = request.StartingMana;
        entity.MaxMana = request.MaxMana;
        entity.ManaGrantedPerTurn = request.ManaGrantedPerTurn;
        entity.ManaGrantTiming = request.ManaGrantTiming;
        entity.InitialDrawCount = request.InitialDrawCount;
        entity.CardsDrawnOnTurnStart = request.CardsDrawnOnTurnStart;
        entity.StartingSeatIndex = request.StartingSeatIndex;

        entity.SeatOverrides.Clear();
        foreach (var overrideRequest in request.SeatOverrides?.OrderBy(overrideRule => overrideRule.SeatIndex).DistinctBy(overrideRule => overrideRule.SeatIndex)
                     ?? Enumerable.Empty<UpsertGameRulesSeatOverrideRequest>())
        {
            entity.SeatOverrides.Add(new GameRulesetSeatOverride
            {
                SeatIndex = overrideRequest.SeatIndex,
                AdditionalHeroHealth = overrideRequest.AdditionalHeroHealth,
                AdditionalMaxHeroHealth = overrideRequest.AdditionalMaxHeroHealth,
                AdditionalStartingMana = overrideRequest.AdditionalStartingMana,
                AdditionalMaxMana = overrideRequest.AdditionalMaxMana,
                AdditionalManaPerTurn = overrideRequest.AdditionalManaPerTurn,
                AdditionalCardsDrawnOnTurnStart = overrideRequest.AdditionalCardsDrawnOnTurnStart
            });
        }
    }

    private static void ValidateRequest(UpsertGameRulesetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RulesetKey))
        {
            throw new InvalidOperationException("RulesetKey is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new InvalidOperationException("DisplayName is required.");
        }

        if (request.IsDefault && !request.IsActive)
        {
            throw new InvalidOperationException("A default ruleset must also be active.");
        }

        if (request.MaxHeroHealth < request.StartingHeroHealth)
        {
            throw new InvalidOperationException("MaxHeroHealth must be greater than or equal to StartingHeroHealth.");
        }

        if (request.MaxMana < request.StartingMana)
        {
            throw new InvalidOperationException("MaxMana must be greater than or equal to StartingMana.");
        }

        var overrides = request.SeatOverrides?.ToArray() ?? Array.Empty<UpsertGameRulesSeatOverrideRequest>();
        var duplicatedSeat = overrides
            .GroupBy(overrideRule => overrideRule.SeatIndex)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicatedSeat != null)
        {
            throw new InvalidOperationException($"Seat override for seat {duplicatedSeat.Key} is duplicated.");
        }

        foreach (var seatOverride in overrides)
        {
            var effectiveHeroHealth = request.StartingHeroHealth + seatOverride.AdditionalHeroHealth;
            var effectiveMaxHeroHealth = request.MaxHeroHealth + seatOverride.AdditionalMaxHeroHealth;
            var effectiveStartingMana = request.StartingMana + seatOverride.AdditionalStartingMana;
            var effectiveMaxMana = request.MaxMana + seatOverride.AdditionalMaxMana;
            var effectiveManaPerTurn = request.ManaGrantedPerTurn + seatOverride.AdditionalManaPerTurn;
            var effectiveCardsDrawn = request.CardsDrawnOnTurnStart + seatOverride.AdditionalCardsDrawnOnTurnStart;

            if (effectiveHeroHealth < 1)
            {
                throw new InvalidOperationException($"Seat {seatOverride.SeatIndex} would start with invalid hero health ({effectiveHeroHealth}).");
            }

            if (effectiveMaxHeroHealth < effectiveHeroHealth)
            {
                throw new InvalidOperationException($"Seat {seatOverride.SeatIndex} would have MaxHeroHealth lower than StartingHeroHealth.");
            }

            if (effectiveStartingMana < 0)
            {
                throw new InvalidOperationException($"Seat {seatOverride.SeatIndex} would start with invalid mana ({effectiveStartingMana}).");
            }

            if (effectiveMaxMana < effectiveStartingMana)
            {
                throw new InvalidOperationException($"Seat {seatOverride.SeatIndex} would have MaxMana lower than StartingMana.");
            }

            if (effectiveManaPerTurn < 0)
            {
                throw new InvalidOperationException($"Seat {seatOverride.SeatIndex} would gain invalid mana per turn ({effectiveManaPerTurn}).");
            }

            if (effectiveCardsDrawn < 0)
            {
                throw new InvalidOperationException($"Seat {seatOverride.SeatIndex} would draw an invalid number of cards per turn ({effectiveCardsDrawn}).");
            }
        }
    }
}
