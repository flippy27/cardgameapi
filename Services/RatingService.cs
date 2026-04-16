namespace CardDuel.ServerApi.Services;

public interface IRatingService
{
    (int newRating1, int newRating2) CalculateEloChange(int rating1, int rating2, bool player1Won);
}

public sealed class EloRatingService : IRatingService
{
    private const int K = 32; // K-factor for rating change
    private const int RatingFloor = 100;
    private const int RatingCeiling = 4000;

    public (int newRating1, int newRating2) CalculateEloChange(int rating1, int rating2, bool player1Won)
    {
        var expectedScore1 = 1 / (1 + Math.Pow(10, (rating2 - rating1) / 400.0));
        var expectedScore2 = 1 - expectedScore1;

        var actualScore1 = player1Won ? 1 : 0;
        var actualScore2 = player1Won ? 0 : 1;

        var delta1 = (int)Math.Round(K * (actualScore1 - expectedScore1));
        var delta2 = (int)Math.Round(K * (actualScore2 - expectedScore2));

        var newRating1 = Math.Clamp(rating1 + delta1, RatingFloor, RatingCeiling);
        var newRating2 = Math.Clamp(rating2 + delta2, RatingFloor, RatingCeiling);

        return (newRating1, newRating2);
    }
}
