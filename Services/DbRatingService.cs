using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace CardDuel.ServerApi.Services;

public interface IRatingDbService
{
    (int newPlayer1Rating, int newPlayer2Rating) UpdateRatingsForMatch(
        string player1Id, string player2Id, int player1RatingBefore, int player2RatingBefore, bool player1Won);
}

public sealed class DbRatingService(AppDbContext dbContext, IRatingService eloService) : IRatingDbService
{
    public (int newPlayer1Rating, int newPlayer2Rating) UpdateRatingsForMatch(
        string player1Id, string player2Id, int player1RatingBefore, int player2RatingBefore, bool player1Won)
    {
        var (newRating1, newRating2) = eloService.CalculateEloChange(player1RatingBefore, player2RatingBefore, player1Won);

        // Get or create rating records
        var rating1 = dbContext.Ratings.FirstOrDefault(r => r.UserId == player1Id)
            ?? new PlayerRating { Id = Guid.NewGuid().ToString(), UserId = player1Id, RatingValue = 1000 };

        var rating2 = dbContext.Ratings.FirstOrDefault(r => r.UserId == player2Id)
            ?? new PlayerRating { Id = Guid.NewGuid().ToString(), UserId = player2Id, RatingValue = 1000 };

        // Update ratings
        rating1.RatingValue = newRating1;
        rating1.Wins += player1Won ? 1 : 0;
        rating1.Losses += player1Won ? 0 : 1;
        rating1.UpdatedAt = DateTimeOffset.UtcNow;

        rating2.RatingValue = newRating2;
        rating2.Wins += player1Won ? 0 : 1;
        rating2.Losses += player1Won ? 1 : 0;
        rating2.UpdatedAt = DateTimeOffset.UtcNow;

        if (dbContext.Ratings.Local.FirstOrDefault(r => r.Id == rating1.Id) == null)
            dbContext.Ratings.Add(rating1);
        else
            dbContext.Ratings.Update(rating1);

        if (dbContext.Ratings.Local.FirstOrDefault(r => r.Id == rating2.Id) == null)
            dbContext.Ratings.Add(rating2);
        else
            dbContext.Ratings.Update(rating2);

        dbContext.SaveChanges();

        return (newRating1, newRating2);
    }
}
