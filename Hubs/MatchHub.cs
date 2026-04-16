using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using CardDuel.ServerApi.Services;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Game;

namespace CardDuel.ServerApi.Hubs;

[Authorize]
public sealed class MatchHub(IMatchService matchService) : Hub
{
    public async Task<MatchSnapshot> ConnectToMatch(ConnectMatchRequest request)
    {
        var playerId = ResolvePlayerId();
        if (!string.Equals(playerId, request.PlayerId, StringComparison.Ordinal))
        {
            throw new HubException("Authenticated player does not match payload.");
        }

        var snapshot = matchService.Connect(request.MatchId, request.PlayerId, request.ReconnectToken, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, request.MatchId);
        await BroadcastMatch(request.MatchId);
        return snapshot;
    }

    public async Task<MatchSnapshot> SetReady(SetReadyRequest request)
    {
        EnsurePlayer(request.PlayerId);
        var snapshot = matchService.SetReady(request.MatchId, request.PlayerId, request.IsReady);
        await BroadcastMatch(request.MatchId);
        return snapshot;
    }

    public async Task<MatchSnapshot> PlayCard(PlayCardRequest request)
    {
        EnsurePlayer(request.PlayerId);
        var snapshot = matchService.PlayCard(request.MatchId, request.PlayerId, request.RuntimeHandKey, request.SlotIndex);
        await BroadcastMatch(request.MatchId);
        return snapshot;
    }

    public async Task<MatchSnapshot> EndTurn(EndTurnRequest request)
    {
        EnsurePlayer(request.PlayerId);
        var snapshot = matchService.EndTurn(request.MatchId, request.PlayerId);
        await BroadcastMatch(request.MatchId);
        return snapshot;
    }

    public async Task<MatchSnapshot> Forfeit(ForfeitRequest request)
    {
        EnsurePlayer(request.PlayerId);
        var snapshot = matchService.Forfeit(request.MatchId, request.PlayerId);
        await BroadcastMatch(request.MatchId);
        return snapshot;
    }

    public async Task WatchMatch(string matchId)
    {
        matchService.AddSpectator(matchId, $"spectator:{ResolvePlayerId()}", Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
        await BroadcastMatch(matchId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var matchId = Context.GetHttpContext()?.Request.Query["matchId"].ToString();
        var playerId = ResolvePlayerId();
        if (!string.IsNullOrWhiteSpace(matchId) && !string.IsNullOrWhiteSpace(playerId))
        {
            try
            {
                matchService.MarkDisconnected(matchId, playerId);
                await BroadcastMatch(matchId);
            }
            catch
            {
                // swallow; disconnections are best-effort here
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task BroadcastMatch(string matchId)
    {
        var dispatches = matchService.BuildDispatches(matchId);
        foreach (var dispatch in dispatches)
        {
            await Clients.Client(dispatch.ConnectionId).SendAsync("MatchSnapshot", dispatch.Snapshot);
        }
    }

    private string ResolvePlayerId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new HubException("Missing authenticated player id.");
    }

    private void EnsurePlayer(string playerId)
    {
        if (!string.Equals(ResolvePlayerId(), playerId, StringComparison.Ordinal))
        {
            throw new HubException("Authenticated player does not match payload.");
        }
    }
}
