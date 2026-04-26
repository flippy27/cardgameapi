namespace CardDuel.ServerApi.Game;

public sealed class GameActionException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;

    public static GameActionException MatchNotPlayable() => new("match_not_playable", "Match is not currently playable.");
    public static GameActionException PlayerNotInMatch() => new("player_not_in_match", "Player is not part of this match.");
    public static GameActionException NotYourTurn(string activePlayerId, int activeSeatIndex) =>
        new("not_your_turn", $"It is not this player's turn. Active player is '{activePlayerId}' in seat {activeSeatIndex}.");
    public static GameActionException CardNotFoundInHand() => new("card_not_found", "Card not found in hand.");
    public static GameActionException NotEnoughMana() => new("not_enough_mana", "Not enough mana.");
    public static GameActionException FrontSlotRequired() => new("front_slot_required", "A front card must exist before playing into the back row.");
    public static GameActionException LeftSlotRequired() => new("left_slot_required", "A left slot card must exist before playing into the right slot.");
    public static GameActionException BoardLaneFull(BoardSlot slot) => new("board_lane_full", $"Cannot place card in {slot}; there is no room to shift cards.");
    public static GameActionException FrontOnlyCardRequired() => new("invalid_row_front_only", "This card can only be played in the front row.");
    public static GameActionException BackOnlyCardRequired() => new("invalid_row_back_only", "This card can only be played in the back row.");
    public static GameActionException RuntimeCardNotFound() => new("runtime_card_not_found", "Board card runtime id was not found.");
    public static GameActionException CannotDestroyOpponentsCard() => new("cannot_destroy_opponents_card", "Players may only destroy their own in-play cards.");
}
