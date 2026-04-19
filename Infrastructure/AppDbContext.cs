using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users { get; set; } = null!;
    public DbSet<PlayerDeck> Decks { get; set; } = null!;
    public DbSet<MatchRecord> Matches { get; set; } = null!;
    public DbSet<PlayerRating> Ratings { get; set; } = null!;
    public DbSet<ReplayLog> ReplayLogs { get; set; } = null!;
    public DbSet<CardDefinition> Cards { get; set; } = null!;
    public DbSet<AbilityDefinition> Abilities { get; set; } = null!;
    public DbSet<EffectDefinition> Effects { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<MatchAction> MatchActions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Username).HasMaxLength(64);
            e.Property(x => x.PasswordHash).HasMaxLength(255);
            e.HasOne(x => x.Rating).WithOne(r => r.User).HasForeignKey<PlayerRating>(r => r.UserId);
        });

        modelBuilder.Entity<PlayerDeck>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.DeckId }).IsUnique();
            e.Property(x => x.DeckId).HasMaxLength(128);
            e.Property(x => x.DisplayName).HasMaxLength(255);
            e.Property(x => x.CardIds)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
        });

        modelBuilder.Entity<MatchRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MatchId).IsUnique();
            e.HasIndex(x => new { x.Player1Id, x.CreatedAt });
            e.HasIndex(x => new { x.Player2Id, x.CreatedAt });
            e.Property(x => x.MatchId).HasMaxLength(64);
            e.Property(x => x.RoomCode).HasMaxLength(16);
        });

        modelBuilder.Entity<PlayerRating>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RatingValue).IsDescending();
            e.Property(x => x.Region).HasMaxLength(32).HasDefaultValue("global");
        });

        modelBuilder.Entity<CardDefinition>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CardId).IsUnique();
            e.Property(x => x.CardId).HasMaxLength(128);
            e.Property(x => x.DisplayName).HasMaxLength(255);
            e.Property(x => x.Description).HasMaxLength(1024);
            e.Property(x => x.AbilitiesJson).HasColumnType("jsonb");
            e.HasMany(x => x.Abilities).WithOne(a => a.CardDefinition).HasForeignKey(a => a.CardDefinitionId);
        });

        modelBuilder.Entity<AbilityDefinition>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CardDefinitionId, x.AbilityId }).IsUnique();
            e.Property(x => x.AbilityId).HasMaxLength(128);
            e.Property(x => x.DisplayName).HasMaxLength(255);
            e.Property(x => x.Description).HasMaxLength(512);
            e.HasMany(x => x.Effects).WithOne(ef => ef.AbilityDefinition).HasForeignKey(ef => ef.AbilityDefinitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EffectDefinition>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AbilityDefinitionId, x.Sequence }).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt).IsDescending();
            e.HasIndex(x => new { x.Resource, x.ResourceId });
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.Resource).HasMaxLength(64);
            e.Property(x => x.ResourceId).HasMaxLength(255);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.Details).HasColumnType("jsonb");
        });

        modelBuilder.Entity<MatchAction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MatchId);
            e.HasIndex(x => new { x.MatchId, x.ActionNumber }).IsUnique();
            e.Property(x => x.ActionType).HasMaxLength(64);
            e.Property(x => x.ActionData).HasColumnType("jsonb");
        });
    }
}
