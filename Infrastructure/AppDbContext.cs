using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users { get; set; } = null!;
    public DbSet<PlayerDeck> Decks { get; set; } = null!;
    public DbSet<MatchRecord> Matches { get; set; } = null!;
    public DbSet<GameRuleset> GameRulesets { get; set; } = null!;
    public DbSet<GameRulesetSeatOverride> GameRulesetSeatOverrides { get; set; } = null!;
    public DbSet<PlayerRating> Ratings { get; set; } = null!;
    public DbSet<ReplayLog> ReplayLogs { get; set; } = null!;
    public DbSet<CardDefinition> Cards { get; set; } = null!;
    public DbSet<AbilityDefinition> Abilities { get; set; } = null!;
    public DbSet<CardAbilityDefinition> CardAbilities { get; set; } = null!;
    public DbSet<EffectDefinition> Effects { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<MatchAction> MatchActions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(64);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.Rating).WithOne(r => r.User).HasForeignKey<PlayerRating>(r => r.UserId);
        });

        modelBuilder.Entity<PlayerDeck>(e =>
        {
            e.ToTable("decks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.DeckId).HasColumnName("deck_id").HasMaxLength(128);
            e.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(255);
            e.Property(x => x.CardIds).HasColumnName("card_ids")
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => new { x.UserId, x.DeckId }).IsUnique();
        });

        modelBuilder.Entity<MatchRecord>(e =>
        {
            e.ToTable("matches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.MatchId).HasColumnName("match_id").HasMaxLength(64);
            e.Property(x => x.RoomCode).HasColumnName("room_code").HasMaxLength(16);
            e.Property(x => x.Player1Id).HasColumnName("player1_id");
            e.Property(x => x.Player2Id).HasColumnName("player2_id");
            e.Property(x => x.WinnerId).HasColumnName("winner_id");
            e.Property(x => x.Mode).HasColumnName("mode");
            e.Property(x => x.GameRulesetId).HasColumnName("game_ruleset_id").HasMaxLength(64);
            e.Property(x => x.GameRulesetName).HasColumnName("game_ruleset_name").HasMaxLength(128);
            e.Property(x => x.GameRulesSnapshotJson).HasColumnName("game_rules_snapshot_json").HasColumnType("jsonb");
            e.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at");
            e.Property(x => x.Player1RatingBefore).HasColumnName("player1_rating_before");
            e.Property(x => x.Player1RatingAfter).HasColumnName("player1_rating_after");
            e.Property(x => x.Player2RatingBefore).HasColumnName("player2_rating_before");
            e.Property(x => x.Player2RatingAfter).HasColumnName("player2_rating_after");
            e.Property(x => x.Player1DisconnectedAt).HasColumnName("player1_disconnected_at");
            e.Property(x => x.Player1ReconnectToken).HasColumnName("player1_reconnect_token");
            e.Property(x => x.Player2DisconnectedAt).HasColumnName("player2_disconnected_at");
            e.Property(x => x.Player2ReconnectToken).HasColumnName("player2_reconnect_token");
            e.HasIndex(x => x.MatchId).IsUnique();
            e.HasIndex(x => new { x.Player1Id, x.CreatedAt });
            e.HasIndex(x => new { x.Player2Id, x.CreatedAt });
            e.HasIndex(x => x.GameRulesetId);
        });

        modelBuilder.Entity<GameRuleset>(e =>
        {
            e.ToTable("game_rulesets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RulesetKey).HasColumnName("ruleset_key").HasMaxLength(64);
            e.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(128);
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(1024);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.IsDefault).HasColumnName("is_default");
            e.Property(x => x.StartingHeroHealth).HasColumnName("starting_hero_health");
            e.Property(x => x.MaxHeroHealth).HasColumnName("max_hero_health");
            e.Property(x => x.StartingMana).HasColumnName("starting_mana");
            e.Property(x => x.MaxMana).HasColumnName("max_mana");
            e.Property(x => x.ManaGrantedPerTurn).HasColumnName("mana_granted_per_turn");
            e.Property(x => x.ManaGrantTiming).HasColumnName("mana_grant_timing");
            e.Property(x => x.InitialDrawCount).HasColumnName("initial_draw_count");
            e.Property(x => x.CardsDrawnOnTurnStart).HasColumnName("cards_drawn_on_turn_start");
            e.Property(x => x.StartingSeatIndex).HasColumnName("starting_seat_index");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.RulesetKey).IsUnique();
            e.HasIndex(x => x.IsDefault);
            e.HasMany(x => x.SeatOverrides)
                .WithOne(x => x.GameRuleset)
                .HasForeignKey(x => x.GameRulesetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GameRulesetSeatOverride>(e =>
        {
            e.ToTable("game_ruleset_seat_overrides");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.GameRulesetId).HasColumnName("game_ruleset_id");
            e.Property(x => x.SeatIndex).HasColumnName("seat_index");
            e.Property(x => x.AdditionalHeroHealth).HasColumnName("additional_hero_health");
            e.Property(x => x.AdditionalMaxHeroHealth).HasColumnName("additional_max_hero_health");
            e.Property(x => x.AdditionalStartingMana).HasColumnName("additional_starting_mana");
            e.Property(x => x.AdditionalMaxMana).HasColumnName("additional_max_mana");
            e.Property(x => x.AdditionalManaPerTurn).HasColumnName("additional_mana_per_turn");
            e.Property(x => x.AdditionalCardsDrawnOnTurnStart).HasColumnName("additional_cards_drawn_on_turn_start");
            e.HasIndex(x => new { x.GameRulesetId, x.SeatIndex }).IsUnique();
        });

        modelBuilder.Entity<PlayerRating>(e =>
        {
            e.ToTable("ratings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.RatingValue).HasColumnName("rating_value");
            e.Property(x => x.Wins).HasColumnName("wins");
            e.Property(x => x.Losses).HasColumnName("losses");
            e.Property(x => x.Region).HasColumnName("region").HasMaxLength(32).HasDefaultValue("global");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.RatingValue).IsDescending();
        });

        modelBuilder.Entity<CardDefinition>(e =>
        {
            e.ToTable("cards");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CardId).HasColumnName("card_id").HasMaxLength(128);
            e.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(255);
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(1024);
            e.Property(x => x.ManaCost).HasColumnName("mana_cost");
            e.Property(x => x.Attack).HasColumnName("attack");
            e.Property(x => x.Health).HasColumnName("health");
            e.Property(x => x.Armor).HasColumnName("armor");
            e.Property(x => x.CardType).HasColumnName("card_type");
            e.Property(x => x.CardRarity).HasColumnName("card_rarity");
            e.Property(x => x.CardFaction).HasColumnName("card_faction");
            e.Property(x => x.UnitType).HasColumnName("unit_type");
            e.Property(x => x.AllowedRow).HasColumnName("allowed_row");
            e.Property(x => x.DefaultAttackSelector).HasColumnName("default_attack_selector");
            e.Property(x => x.TurnsUntilCanAttack).HasColumnName("turns_until_can_attack");
            e.Property(x => x.IsLimited).HasColumnName("is_limited");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.CardId).IsUnique();
            e.HasMany(x => x.CardAbilities).WithOne(ca => ca.CardDefinition).HasForeignKey(ca => ca.CardDefinitionId);
        });

        modelBuilder.Entity<AbilityDefinition>(e =>
        {
            e.ToTable("abilities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AbilityId).HasColumnName("ability_id").HasMaxLength(128);
            e.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(255);
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(512);
            e.Property(x => x.TriggerKind).HasColumnName("trigger_kind");
            e.Property(x => x.TargetSelectorKind).HasColumnName("target_selector_kind");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.AbilityId).IsUnique();
            e.HasMany(x => x.CardAbilities).WithOne(ca => ca.AbilityDefinition).HasForeignKey(ca => ca.AbilityDefinitionId);
            e.HasMany(x => x.Effects).WithOne(ef => ef.AbilityDefinition).HasForeignKey(ef => ef.AbilityDefinitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardAbilityDefinition>(e =>
        {
            e.ToTable("card_abilities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CardDefinitionId).HasColumnName("card_definition_id");
            e.Property(x => x.AbilityDefinitionId).HasColumnName("ability_definition_id");
            e.Property(x => x.Sequence).HasColumnName("sequence");
            e.HasIndex(x => new { x.CardDefinitionId, x.Sequence }).IsUnique();
        });

        modelBuilder.Entity<EffectDefinition>(e =>
        {
            e.ToTable("effects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EffectKind).HasColumnName("effect_kind");
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.Sequence).HasColumnName("sequence");
            e.Property(x => x.AbilityDefinitionId).HasColumnName("ability_definition_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => new { x.AbilityDefinitionId, x.Sequence }).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Action).HasColumnName("action").HasMaxLength(64);
            e.Property(x => x.Resource).HasColumnName("resource").HasMaxLength(64);
            e.Property(x => x.ResourceId).HasColumnName("resource_id").HasMaxLength(255);
            e.Property(x => x.Details).HasColumnName("details").HasColumnType("jsonb");
            e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            e.Property(x => x.StatusCode).HasColumnName("status_code");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt).IsDescending();
            e.HasIndex(x => new { x.Resource, x.ResourceId });
        });

        modelBuilder.Entity<MatchAction>(e =>
        {
            e.ToTable("match_actions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.MatchId).HasColumnName("match_id");
            e.Property(x => x.ActionNumber).HasColumnName("action_number");
            e.Property(x => x.PlayerId).HasColumnName("player_id");
            e.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(64);
            e.Property(x => x.ActionData).HasColumnName("action_data").HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.MatchId);
            e.HasIndex(x => new { x.MatchId, x.ActionNumber }).IsUnique();
        });

        modelBuilder.Entity<ReplayLog>(e =>
        {
            e.ToTable("replay_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.MatchId).HasColumnName("match_id");
            e.Property(x => x.ActionNumber).HasColumnName("action_number");
            e.Property(x => x.PlayerId).HasColumnName("player_id");
            e.Property(x => x.ActionType).HasColumnName("action_type");
            e.Property(x => x.ActionData).HasColumnName("action_data").HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
