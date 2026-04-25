namespace CardDuel.ServerApi.Infrastructure.Models;

/// <summary>
/// Represents a single upgrade applied to a player-owned card instance.
/// Each row is one upgrade "fact". Multiple rows per card are expected.
/// upgrade_kind examples: "attack_bonus", "health_bonus", "armor_bonus",
/// "added_ability", "level_up", "custom_tag", etc.
/// </summary>
public sealed class PlayerCardUpgrade
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PlayerCardId { get; set; } = string.Empty;
    public string UpgradeKind { get; set; } = string.Empty; // free-form key, extensible
    public int? IntValue { get; set; }         // numeric bonus (e.g. +3 attack, +5 hp)
    public string? StringValue { get; set; }   // string reference (e.g. ability_id added)
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
    public string AppliedBy { get; set; } = string.Empty; // "crafting", "admin", "event", "upgrade_system"
    public string? Note { get; set; }          // optional human-readable description

    public PlayerCard PlayerCard { get; set; } = null!;
}
