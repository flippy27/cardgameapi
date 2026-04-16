namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class PlayerRating
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public int RatingValue { get; set; } = 1000;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public string Region { get; set; } = "global";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public UserAccount? User { get; set; }

    public double WinRate => Wins + Losses == 0 ? 0 : (double)Wins / (Wins + Losses);
}
