namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class AuditLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty; // e.g., "Match", "Deck", "Rating"
    public string ResourceId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty; // JSON details
    public string IpAddress { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
