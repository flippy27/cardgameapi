using System.ComponentModel.DataAnnotations;

namespace CardDuel.ServerApi.Contracts;

public enum QueueMode
{
    Casual = 0,
    Ranked = 1,
    Private = 2
}

public enum ManaGrantTiming
{
    StartOfTurn = 0,
    EndOfTurn = 1
}

public sealed record GameRulesSeatOverrideDto(
    int SeatIndex,
    int AdditionalHeroHealth,
    int AdditionalMaxHeroHealth,
    int AdditionalStartingMana,
    int AdditionalMaxMana,
    int AdditionalManaPerTurn,
    int AdditionalCardsDrawnOnTurnStart);

public sealed record GameRulesDto(
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
    IReadOnlyList<GameRulesSeatOverrideDto> SeatOverrides);

public sealed record GameRulesetSummaryDto(
    string RulesetId,
    string RulesetKey,
    string DisplayName,
    bool IsActive,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record MatchmakingModeRulesetDto(
    QueueMode Mode,
    string RulesetId,
    string RulesetKey,
    string DisplayName,
    bool RulesetIsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AssignMatchmakingModeRulesetRequest(
    [Required] string RulesetId);

public sealed record UpsertGameRulesSeatOverrideRequest(
    [Range(0, 1)] int SeatIndex,
    int AdditionalHeroHealth = 0,
    int AdditionalMaxHeroHealth = 0,
    int AdditionalStartingMana = 0,
    int AdditionalMaxMana = 0,
    int AdditionalManaPerTurn = 0,
    int AdditionalCardsDrawnOnTurnStart = 0);

public sealed record UpsertGameRulesetRequest(
    [Required] string RulesetKey,
    [Required] string DisplayName,
    string? Description,
    bool IsActive,
    bool IsDefault,
    [Range(1, 200)] int StartingHeroHealth,
    [Range(1, 200)] int MaxHeroHealth,
    [Range(0, 20)] int StartingMana,
    [Range(0, 20)] int MaxMana,
    [Range(0, 10)] int ManaGrantedPerTurn,
    ManaGrantTiming ManaGrantTiming,
    [Range(0, 20)] int InitialDrawCount,
    [Range(0, 10)] int CardsDrawnOnTurnStart,
    [Range(0, 1)] int StartingSeatIndex,
    IReadOnlyList<UpsertGameRulesSeatOverrideRequest>? SeatOverrides = null);

public sealed record CreatePrivateMatchRequest(
    [Required] string PlayerId,
    [Required] string DeckId,
    string? MatchName);

public sealed record JoinPrivateMatchRequest(
    [Required] string PlayerId,
    [Required] string DeckId,
    [Required] string RoomCode);

public sealed record QueueForMatchRequest(
    [Required] string PlayerId,
    [Required] string DeckId,
    QueueMode Mode,
    int Rating = 1000);

public sealed record MatchReservationDto(
    string MatchId,
    string RoomCode,
    string ReconnectToken,
    int SeatIndex,
    QueueMode Mode,
    bool WaitingForOpponent,
    string Status,
    string RulesetId,
    GameRulesDto Rules);

public sealed record GameActionErrorDto(
    string Code,
    string Message);

public sealed record ConnectMatchRequest(
    [Required] string PlayerId,
    [Required] string MatchId,
    [Required] string ReconnectToken);

public sealed record SetReadyRequest(
    [Required] string MatchId,
    [Required] string PlayerId,
    bool IsReady);

public sealed record PlayCardRequest(
    [Required] string MatchId,
    [Required] string PlayerId,
    [Required] string RuntimeHandKey,
    [Range(0, 2)] int SlotIndex);

public sealed record EndTurnRequest(
    [Required] string MatchId,
    [Required] string PlayerId);

public sealed record ForfeitRequest(
    [Required] string MatchId,
    [Required] string PlayerId);

public sealed record MatchSummaryDto(
    string MatchId,
    string RoomCode,
    QueueMode Mode,
    int ConnectedPlayers,
    bool Started,
    bool Completed,
    int? WinnerSeatIndex,
    string RulesetId,
    string RulesetName);

public sealed record DeckUpsertRequest(
    [Required] string PlayerId,
    [Required] string DeckId,
    [Required] string DisplayName,
    [Required] IReadOnlyList<string> CardIds);

public sealed record DeckCardEntryDto(
    string EntryId,
    string CardId,
    int Position);

public sealed record DeckDetailsDto(
    string PlayerId,
    string DeckId,
    string DisplayName,
    IReadOnlyList<DeckCardEntryDto> Cards,
    IReadOnlyList<string> CardIds);

public sealed record AddDeckCardRequest(
    [Required] string CardId,
    int? Position = null);

public sealed record MatchCompletionRequest(
    [Required] string PlayerId,
    [Required] string OpponentId,
    bool PlayerWon,
    int DurationSeconds,
    int? PlayerRatingBefore = null,
    int? OpponentRatingBefore = null);

public sealed record MatchCompletionResponse(
    string MatchId,
    bool Recorded,
    string Message);

public sealed record MatchActionDto(
    int ActionNumber,
    int Sequence,
    DateTime Timestamp,
    [Required] string PlayerId,
    [Required] string ActionType,
    object? Data);

public sealed record PostActionsRequest(
    [Required] string MatchId,
    [Required] List<MatchActionDto> Actions,
    int GlobalSequence,
    DateTime Timestamp);

public sealed record PostActionsResponse(
    string MatchId,
    int ActionsReceived,
    bool Success,
    string Message);
