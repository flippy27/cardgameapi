using System.Security.Claims;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Controllers;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardDuel.ServerApi.Tests;

public sealed class MatchesControllerTests
{
    [Fact]
    public void PlayCard_WhenActionFails_ReturnsParseableGameActionError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AppDbContext(options);

        var controller = new MatchesController(new ThrowingMatchService(), dbContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, "player1") },
                            "TestAuth"))
                }
            }
        };

        var response = controller.PlayCard("match1", new PlayCardRequest("match1", "player1", "runtime-1", 0));

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        var payload = Assert.IsType<GameActionErrorDto>(badRequest.Value);
        Assert.Equal("not_enough_mana", payload.Code);
        Assert.Equal("Not enough mana.", payload.Message);
    }

    private sealed class ThrowingMatchService : IMatchService
    {
        public MatchReservationDto CreatePrivate(string playerId, string deckId, string? name, ResolvedGameRules resolvedRules) => throw new NotImplementedException();
        public MatchReservationDto JoinPrivate(string playerId, string deckId, string roomCode) => throw new NotImplementedException();
        public MatchReservationDto Queue(string playerId, string deckId, QueueMode mode, int rating, ResolvedGameRules resolvedRules) => throw new NotImplementedException();
        public bool TryReconnect(string matchId, string playerId, string reconnectToken, out int seatIndex) => throw new NotImplementedException();
        public MatchSnapshot Connect(string matchId, string playerId, string reconnectToken, string connectionId) => throw new NotImplementedException();
        public MatchSnapshot SetReady(string matchId, string playerId, bool ready) => throw new NotImplementedException();
        public MatchSnapshot PlayCard(string matchId, string playerId, string runtimeHandKey, int slotIndex) => throw GameActionException.NotEnoughMana();
        public MatchSnapshot EndTurn(string matchId, string playerId) => throw new NotImplementedException();
        public MatchSnapshot DestroyCard(string matchId, string playerId, string runtimeCardId) => throw new NotImplementedException();
        public MatchSnapshot Forfeit(string matchId, string playerId) => throw new NotImplementedException();
        public MatchCompletionResponse CompleteMatch(string matchId, string playerId, string opponentId, bool playerWon, int durationSeconds) => throw new NotImplementedException();
        public PostActionsResponse ProcessActions(string matchId, PostActionsRequest request) => throw new NotImplementedException();
        public MatchSnapshot MarkDisconnected(string matchId, string playerId) => throw new NotImplementedException();
        public MatchSnapshot GetSnapshot(string matchId, string playerId, bool spectator = false) => throw new NotImplementedException();
        public MatchSummaryDto GetSummary(string matchId) => throw new NotImplementedException();
        public IReadOnlyList<MatchSummaryDto> ListMatches() => throw new NotImplementedException();
        public IReadOnlyList<DispatchEnvelope> BuildDispatches(string matchId) => throw new NotImplementedException();
        public void AddSpectator(string matchId, string spectatorId, string connectionId) => throw new NotImplementedException();
    }
}
