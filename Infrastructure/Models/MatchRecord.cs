using CardDuel.ServerApi.Contracts;

namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class MatchRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MatchId { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string Player1Id { get; set; } = string.Empty;
    public string Player2Id { get; set; } = string.Empty;
    public string? WinnerId { get; set; }
    public QueueMode Mode { get; set; }
    public string GameRulesetId { get; set; } = string.Empty;
    public string GameRulesetName { get; set; } = string.Empty;
    public string GameRulesSnapshotJson { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int? Player1RatingBefore { get; set; }
    public int? Player1RatingAfter { get; set; }
    public int? Player2RatingBefore { get; set; }
    public int? Player2RatingAfter { get; set; }
    public DateTimeOffset? Player1DisconnectedAt { get; set; }
    public DateTimeOffset? Player2DisconnectedAt { get; set; }
    public string? Player1ReconnectToken { get; set; }
    public string? Player2ReconnectToken { get; set; }
}
