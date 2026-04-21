using System.Text.Json;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Game;

public sealed record GameRulesSeatOverride(
    int SeatIndex,
    int AdditionalHeroHealth,
    int AdditionalMaxHeroHealth,
    int AdditionalStartingMana,
    int AdditionalMaxMana,
    int AdditionalManaPerTurn,
    int AdditionalCardsDrawnOnTurnStart);

public sealed record EffectiveSeatRules(
    int HeroHealth,
    int MaxHeroHealth,
    int StartingMana,
    int MaxMana,
    int ManaGrantedPerTurn,
    int CardsDrawnOnTurnStart);

public sealed record GameRules(
    string RulesetId,
    string RulesetKey,
    string DisplayName,
    string? Description,
    bool IsActive,
    bool IsDefault,
    int StartingHeroHealth,
    int MaxHeroHealth,
    int StartingMana,
    int MaxMana,
    int ManaGrantedPerTurn,
    ManaGrantTiming ManaGrantTiming,
    int InitialDrawCount,
    int CardsDrawnOnTurnStart,
    int StartingSeatIndex,
    IReadOnlyList<GameRulesSeatOverride> SeatOverrides)
{
    public EffectiveSeatRules GetSeatRules(int seatIndex)
    {
        var seatOverride = SeatOverrides.FirstOrDefault(overrideRule => overrideRule.SeatIndex == seatIndex);
        return new EffectiveSeatRules(
            HeroHealth: StartingHeroHealth + (seatOverride?.AdditionalHeroHealth ?? 0),
            MaxHeroHealth: MaxHeroHealth + (seatOverride?.AdditionalMaxHeroHealth ?? 0),
            StartingMana: StartingMana + (seatOverride?.AdditionalStartingMana ?? 0),
            MaxMana: MaxMana + (seatOverride?.AdditionalMaxMana ?? 0),
            ManaGrantedPerTurn: ManaGrantedPerTurn + (seatOverride?.AdditionalManaPerTurn ?? 0),
            CardsDrawnOnTurnStart: CardsDrawnOnTurnStart + (seatOverride?.AdditionalCardsDrawnOnTurnStart ?? 0));
    }

    public GameRulesDto ToDto()
    {
        return new GameRulesDto(
            RulesetId,
            RulesetKey,
            DisplayName,
            Description,
            IsActive,
            IsDefault,
            StartingHeroHealth,
            MaxHeroHealth,
            StartingMana,
            MaxMana,
            ManaGrantedPerTurn,
            ManaGrantTiming,
            InitialDrawCount,
            CardsDrawnOnTurnStart,
            StartingSeatIndex,
            SeatOverrides.Select(overrideRule => new GameRulesSeatOverrideDto(
                overrideRule.SeatIndex,
                overrideRule.AdditionalHeroHealth,
                overrideRule.AdditionalMaxHeroHealth,
                overrideRule.AdditionalStartingMana,
                overrideRule.AdditionalMaxMana,
                overrideRule.AdditionalManaPerTurn,
                overrideRule.AdditionalCardsDrawnOnTurnStart)).ToArray());
    }

    public string ToSnapshotJson() => JsonSerializer.Serialize(ToDto());

    public static GameRules FromEntity(GameRuleset entity)
    {
        return new GameRules(
            entity.Id,
            entity.RulesetKey,
            entity.DisplayName,
            entity.Description,
            entity.IsActive,
            entity.IsDefault,
            entity.StartingHeroHealth,
            entity.MaxHeroHealth,
            entity.StartingMana,
            entity.MaxMana,
            entity.ManaGrantedPerTurn,
            entity.ManaGrantTiming,
            entity.InitialDrawCount,
            entity.CardsDrawnOnTurnStart,
            entity.StartingSeatIndex,
            entity.SeatOverrides
                .OrderBy(overrideRule => overrideRule.SeatIndex)
                .Select(overrideRule => new GameRulesSeatOverride(
                    overrideRule.SeatIndex,
                    overrideRule.AdditionalHeroHealth,
                    overrideRule.AdditionalMaxHeroHealth,
                    overrideRule.AdditionalStartingMana,
                    overrideRule.AdditionalMaxMana,
                    overrideRule.AdditionalManaPerTurn,
                    overrideRule.AdditionalCardsDrawnOnTurnStart))
                .ToArray());
    }
}

public sealed record ResolvedGameRules(
    string RulesetId,
    string DisplayName,
    GameRules Rules,
    string SnapshotJson);
