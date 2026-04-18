using Xunit;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Tests;

public class RatingServiceTests
{
    private readonly IRatingService _ratingService = new EloRatingService();

    [Fact]
    public void CalculateEloChange_WinnerGainsPoints()
    {
        var (newRating1, newRating2) = _ratingService.CalculateEloChange(1000, 1000, player1Won: true);
        Assert.True(newRating1 > 1000, "Winner should gain rating");
        Assert.True(newRating2 < 1000, "Loser should lose rating");
    }

    [Fact]
    public void CalculateEloChange_SumRemainsConstant()
    {
        var initial = 1000 + 1200;
        var (newRating1, newRating2) = _ratingService.CalculateEloChange(1000, 1200, player1Won: true);
        var after = newRating1 + newRating2;
        Assert.Equal(initial, after);
    }

    [Fact]
    public void CalculateEloChange_FavoriteLosingLosesMore()
    {
        var (_, loserRating) = _ratingService.CalculateEloChange(1200, 1000, player1Won: false);
        Assert.True(loserRating < 1200);
    }
}
