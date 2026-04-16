namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class Tournament
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public int MaxPlayers { get; set; }
    public TournamentStatus Status { get; set; } = TournamentStatus.Open;
    public List<string> ParticipantIds { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum TournamentStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
