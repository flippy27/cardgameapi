using System.ComponentModel.DataAnnotations;

namespace CardDuel.ServerApi.Contracts;

public enum QueueMode
{
    Casual = 0,
    Ranked = 1,
    Private = 2
}

public sealed record CreatePrivateMatchRequest(
    [property: Required] string PlayerId,
    [property: Required] string DeckId,
    string? MatchName);

public sealed record JoinPrivateMatchRequest(
    [property: Required] string PlayerId,
    [property: Required] string DeckId,
    [property: Required] string RoomCode);

public sealed record QueueForMatchRequest(
    [property: Required] string PlayerId,
    [property: Required] string DeckId,
    QueueMode Mode,
    int Rating = 1000);

public sealed record MatchReservationDto(
    string MatchId,
    string RoomCode,
    string ReconnectToken,
    int SeatIndex,
    QueueMode Mode,
    bool WaitingForOpponent,
    string Status);

public sealed record ConnectMatchRequest(
    [property: Required] string PlayerId,
    [property: Required] string MatchId,
    [property: Required] string ReconnectToken);

public sealed record SetReadyRequest(
    [property: Required] string MatchId,
    [property: Required] string PlayerId,
    bool IsReady);

public sealed record PlayCardRequest(
    [property: Required] string MatchId,
    [property: Required] string PlayerId,
    [property: Required] string RuntimeHandKey,
    [property: Range(0, 2)] int SlotIndex);

public sealed record EndTurnRequest(
    [property: Required] string MatchId,
    [property: Required] string PlayerId);

public sealed record ForfeitRequest(
    [property: Required] string MatchId,
    [property: Required] string PlayerId);

public sealed record MatchSummaryDto(
    string MatchId,
    string RoomCode,
    QueueMode Mode,
    int ConnectedPlayers,
    bool Started,
    bool Completed,
    int? WinnerSeatIndex);

public sealed record DeckUpsertRequest(
    [property: Required] string PlayerId,
    [property: Required] string DeckId,
    [property: Required] string DisplayName,
    [property: Required] IReadOnlyList<string> CardIds);

public sealed record MatchCompletionRequest(
    [property: Required] string PlayerId,
    [property: Required] string OpponentId,
    bool PlayerWon,
    int DurationSeconds,
    int? PlayerRatingBefore = null,
    int? OpponentRatingBefore = null);

public sealed record MatchCompletionResponse(
    string MatchId,
    bool Recorded,
    string Message);
