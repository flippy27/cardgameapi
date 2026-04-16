namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class ReplayLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MatchId { get; set; } = string.Empty;
    public int ActionNumber { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // PlayCard, EndTurn, Forfeit, etc
    public string ActionData { get; set; } = string.Empty; // JSON serialized
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
