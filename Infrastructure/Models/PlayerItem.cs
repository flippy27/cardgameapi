namespace CardDuel.ServerApi.Infrastructure.Models;

/// <summary>
/// Player's item inventory. One row per (user_id, item_type_id) pair.
/// Quantity accumulates in place — no separate transaction log here.
/// </summary>
public sealed class PlayerItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public int ItemTypeId { get; set; }
    public long Quantity { get; set; } = 0;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserAccount User { get; set; } = null!;
    public ItemTypeDefinition ItemType { get; set; } = null!;
}
