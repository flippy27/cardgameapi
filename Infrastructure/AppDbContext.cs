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
            e.Property(x => x.CardIds).HasConversion(
                v => string.Join(",", v),
                v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList());
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
    }
}
