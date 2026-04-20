using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardAbilities_Abilities_AbilityDefinitionId",
                table: "CardAbilities");

            migrationBuilder.DropForeignKey(
                name: "FK_CardAbilities_Cards_CardDefinitionId",
                table: "CardAbilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Effects_Abilities_AbilityDefinitionId",
                table: "Effects");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Users_UserId",
                table: "Ratings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ratings",
                table: "Ratings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Matches",
                table: "Matches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Effects",
                table: "Effects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Decks",
                table: "Decks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cards",
                table: "Cards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Abilities",
                table: "Abilities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReplayLogs",
                table: "ReplayLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MatchActions",
                table: "MatchActions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CardAbilities",
                table: "CardAbilities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SkillType",
                table: "Abilities");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Ratings",
                newName: "ratings");

            migrationBuilder.RenameTable(
                name: "Matches",
                newName: "matches");

            migrationBuilder.RenameTable(
                name: "Effects",
                newName: "effects");

            migrationBuilder.RenameTable(
                name: "Decks",
                newName: "decks");

            migrationBuilder.RenameTable(
                name: "Cards",
                newName: "cards");

            migrationBuilder.RenameTable(
                name: "Abilities",
                newName: "abilities");

            migrationBuilder.RenameTable(
                name: "ReplayLogs",
                newName: "replay_logs");

            migrationBuilder.RenameTable(
                name: "MatchActions",
                newName: "match_actions");

            migrationBuilder.RenameTable(
                name: "CardAbilities",
                newName: "card_abilities");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "users",
                newName: "last_login_at");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "users",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "users",
                newName: "IX_users_username");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "users",
                newName: "IX_users_email");

            migrationBuilder.RenameColumn(
                name: "Wins",
                table: "ratings",
                newName: "wins");

            migrationBuilder.RenameColumn(
                name: "Region",
                table: "ratings",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "Losses",
                table: "ratings",
                newName: "losses");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ratings",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ratings",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ratings",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RatingValue",
                table: "ratings",
                newName: "rating_value");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ratings",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Ratings_UserId",
                table: "ratings",
                newName: "IX_ratings_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_Ratings_RatingValue",
                table: "ratings",
                newName: "IX_ratings_rating_value");

            migrationBuilder.RenameColumn(
                name: "Mode",
                table: "matches",
                newName: "mode");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "matches",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WinnerId",
                table: "matches",
                newName: "winner_id");

            migrationBuilder.RenameColumn(
                name: "RoomCode",
                table: "matches",
                newName: "room_code");

            migrationBuilder.RenameColumn(
                name: "Player2ReconnectToken",
                table: "matches",
                newName: "player2_reconnect_token");

            migrationBuilder.RenameColumn(
                name: "Player2RatingBefore",
                table: "matches",
                newName: "player2_rating_before");

            migrationBuilder.RenameColumn(
                name: "Player2RatingAfter",
                table: "matches",
                newName: "player2_rating_after");

            migrationBuilder.RenameColumn(
                name: "Player2Id",
                table: "matches",
                newName: "player2_id");

            migrationBuilder.RenameColumn(
                name: "Player2DisconnectedAt",
                table: "matches",
                newName: "player2_disconnected_at");

            migrationBuilder.RenameColumn(
                name: "Player1ReconnectToken",
                table: "matches",
                newName: "player1_reconnect_token");

            migrationBuilder.RenameColumn(
                name: "Player1RatingBefore",
                table: "matches",
                newName: "player1_rating_before");

            migrationBuilder.RenameColumn(
                name: "Player1RatingAfter",
                table: "matches",
                newName: "player1_rating_after");

            migrationBuilder.RenameColumn(
                name: "Player1Id",
                table: "matches",
                newName: "player1_id");

            migrationBuilder.RenameColumn(
                name: "Player1DisconnectedAt",
                table: "matches",
                newName: "player1_disconnected_at");

            migrationBuilder.RenameColumn(
                name: "MatchId",
                table: "matches",
                newName: "match_id");

            migrationBuilder.RenameColumn(
                name: "DurationSeconds",
                table: "matches",
                newName: "duration_seconds");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "matches",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "matches",
                newName: "completed_at");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_Player2Id_CreatedAt",
                table: "matches",
                newName: "IX_matches_player2_id_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_Player1Id_CreatedAt",
                table: "matches",
                newName: "IX_matches_player1_id_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_MatchId",
                table: "matches",
                newName: "IX_matches_match_id");

            migrationBuilder.RenameColumn(
                name: "Sequence",
                table: "effects",
                newName: "sequence");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "effects",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "effects",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "EffectKind",
                table: "effects",
                newName: "effect_kind");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "effects",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AbilityDefinitionId",
                table: "effects",
                newName: "ability_definition_id");

            migrationBuilder.RenameIndex(
                name: "IX_Effects_AbilityDefinitionId_Sequence",
                table: "effects",
                newName: "IX_effects_ability_definition_id_sequence");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "decks",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "decks",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "decks",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "decks",
                newName: "display_name");

            migrationBuilder.RenameColumn(
                name: "DeckId",
                table: "decks",
                newName: "deck_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "decks",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CardIds",
                table: "decks",
                newName: "card_ids");

            migrationBuilder.RenameIndex(
                name: "IX_Decks_UserId_DeckId",
                table: "decks",
                newName: "IX_decks_user_id_deck_id");

            migrationBuilder.RenameColumn(
                name: "Health",
                table: "cards",
                newName: "health");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "cards",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Attack",
                table: "cards",
                newName: "attack");

            migrationBuilder.RenameColumn(
                name: "Armor",
                table: "cards",
                newName: "armor");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "cards",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "cards",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "UnitType",
                table: "cards",
                newName: "unit_type");

            migrationBuilder.RenameColumn(
                name: "TurnsUntilCanAttack",
                table: "cards",
                newName: "turns_until_can_attack");

            migrationBuilder.RenameColumn(
                name: "ManaCost",
                table: "cards",
                newName: "mana_cost");

            migrationBuilder.RenameColumn(
                name: "IsLimited",
                table: "cards",
                newName: "is_limited");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "cards",
                newName: "display_name");

            migrationBuilder.RenameColumn(
                name: "DefaultAttackSelector",
                table: "cards",
                newName: "default_attack_selector");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "cards",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CardType",
                table: "cards",
                newName: "card_type");

            migrationBuilder.RenameColumn(
                name: "CardRarity",
                table: "cards",
                newName: "card_rarity");

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "cards",
                newName: "card_id");

            migrationBuilder.RenameColumn(
                name: "CardFaction",
                table: "cards",
                newName: "card_faction");

            migrationBuilder.RenameColumn(
                name: "AllowedRow",
                table: "cards",
                newName: "allowed_row");

            migrationBuilder.RenameIndex(
                name: "IX_Cards_CardId",
                table: "cards",
                newName: "IX_cards_card_id");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "abilities",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "abilities",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "abilities",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TriggerKind",
                table: "abilities",
                newName: "trigger_kind");

            migrationBuilder.RenameColumn(
                name: "TargetSelectorKind",
                table: "abilities",
                newName: "target_selector_kind");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "abilities",
                newName: "display_name");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "abilities",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AbilityId",
                table: "abilities",
                newName: "ability_id");

            migrationBuilder.RenameIndex(
                name: "IX_Abilities_AbilityId",
                table: "abilities",
                newName: "IX_abilities_ability_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "replay_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "replay_logs",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "MatchId",
                table: "replay_logs",
                newName: "match_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "replay_logs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ActionType",
                table: "replay_logs",
                newName: "action_type");

            migrationBuilder.RenameColumn(
                name: "ActionNumber",
                table: "replay_logs",
                newName: "action_number");

            migrationBuilder.RenameColumn(
                name: "ActionData",
                table: "replay_logs",
                newName: "action_data");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "match_actions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "match_actions",
                newName: "player_id");

            migrationBuilder.RenameColumn(
                name: "MatchId",
                table: "match_actions",
                newName: "match_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "match_actions",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ActionType",
                table: "match_actions",
                newName: "action_type");

            migrationBuilder.RenameColumn(
                name: "ActionNumber",
                table: "match_actions",
                newName: "action_number");

            migrationBuilder.RenameColumn(
                name: "ActionData",
                table: "match_actions",
                newName: "action_data");

            migrationBuilder.RenameIndex(
                name: "IX_MatchActions_MatchId_ActionNumber",
                table: "match_actions",
                newName: "IX_match_actions_match_id_action_number");

            migrationBuilder.RenameIndex(
                name: "IX_MatchActions_MatchId",
                table: "match_actions",
                newName: "IX_match_actions_match_id");

            migrationBuilder.RenameColumn(
                name: "Sequence",
                table: "card_abilities",
                newName: "sequence");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "card_abilities",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CardDefinitionId",
                table: "card_abilities",
                newName: "card_definition_id");

            migrationBuilder.RenameColumn(
                name: "AbilityDefinitionId",
                table: "card_abilities",
                newName: "ability_definition_id");

            migrationBuilder.RenameIndex(
                name: "IX_CardAbilities_CardDefinitionId_Sequence",
                table: "card_abilities",
                newName: "IX_card_abilities_card_definition_id_sequence");

            migrationBuilder.RenameIndex(
                name: "IX_CardAbilities_AbilityDefinitionId",
                table: "card_abilities",
                newName: "IX_card_abilities_ability_definition_id");

            migrationBuilder.RenameColumn(
                name: "Resource",
                table: "audit_logs",
                newName: "resource");

            migrationBuilder.RenameColumn(
                name: "Details",
                table: "audit_logs",
                newName: "details");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "audit_logs",
                newName: "action");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "audit_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "audit_logs",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "StatusCode",
                table: "audit_logs",
                newName: "status_code");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "audit_logs",
                newName: "resource_id");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "audit_logs",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "audit_logs",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_UserId",
                table: "audit_logs",
                newName: "IX_audit_logs_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_Resource_ResourceId",
                table: "audit_logs",
                newName: "IX_audit_logs_resource_resource_id");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "audit_logs",
                newName: "IX_audit_logs_created_at");

            migrationBuilder.AlterColumn<string>(
                name: "action_data",
                table: "replay_logs",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ratings",
                table: "ratings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_matches",
                table: "matches",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_effects",
                table: "effects",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_decks",
                table: "decks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cards",
                table: "cards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_abilities",
                table: "abilities",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_replay_logs",
                table: "replay_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_match_actions",
                table: "match_actions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_card_abilities",
                table: "card_abilities",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_card_abilities_abilities_ability_definition_id",
                table: "card_abilities",
                column: "ability_definition_id",
                principalTable: "abilities",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_card_abilities_cards_card_definition_id",
                table: "card_abilities",
                column: "card_definition_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_effects_abilities_ability_definition_id",
                table: "effects",
                column: "ability_definition_id",
                principalTable: "abilities",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ratings_users_user_id",
                table: "ratings",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_card_abilities_abilities_ability_definition_id",
                table: "card_abilities");

            migrationBuilder.DropForeignKey(
                name: "FK_card_abilities_cards_card_definition_id",
                table: "card_abilities");

            migrationBuilder.DropForeignKey(
                name: "FK_effects_abilities_ability_definition_id",
                table: "effects");

            migrationBuilder.DropForeignKey(
                name: "FK_ratings_users_user_id",
                table: "ratings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ratings",
                table: "ratings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_matches",
                table: "matches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_effects",
                table: "effects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_decks",
                table: "decks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cards",
                table: "cards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_abilities",
                table: "abilities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_replay_logs",
                table: "replay_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_match_actions",
                table: "match_actions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_card_abilities",
                table: "card_abilities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "ratings",
                newName: "Ratings");

            migrationBuilder.RenameTable(
                name: "matches",
                newName: "Matches");

            migrationBuilder.RenameTable(
                name: "effects",
                newName: "Effects");

            migrationBuilder.RenameTable(
                name: "decks",
                newName: "Decks");

            migrationBuilder.RenameTable(
                name: "cards",
                newName: "Cards");

            migrationBuilder.RenameTable(
                name: "abilities",
                newName: "Abilities");

            migrationBuilder.RenameTable(
                name: "replay_logs",
                newName: "ReplayLogs");

            migrationBuilder.RenameTable(
                name: "match_actions",
                newName: "MatchActions");

            migrationBuilder.RenameTable(
                name: "card_abilities",
                newName: "CardAbilities");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                newName: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "Users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "last_login_at",
                table: "Users",
                newName: "LastLoginAt");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Users",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_users_username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_users_email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameColumn(
                name: "wins",
                table: "Ratings",
                newName: "Wins");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "Ratings",
                newName: "Region");

            migrationBuilder.RenameColumn(
                name: "losses",
                table: "Ratings",
                newName: "Losses");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Ratings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Ratings",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Ratings",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "rating_value",
                table: "Ratings",
                newName: "RatingValue");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Ratings",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_ratings_user_id",
                table: "Ratings",
                newName: "IX_Ratings_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ratings_rating_value",
                table: "Ratings",
                newName: "IX_Ratings_RatingValue");

            migrationBuilder.RenameColumn(
                name: "mode",
                table: "Matches",
                newName: "Mode");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Matches",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "winner_id",
                table: "Matches",
                newName: "WinnerId");

            migrationBuilder.RenameColumn(
                name: "room_code",
                table: "Matches",
                newName: "RoomCode");

            migrationBuilder.RenameColumn(
                name: "player2_reconnect_token",
                table: "Matches",
                newName: "Player2ReconnectToken");

            migrationBuilder.RenameColumn(
                name: "player2_rating_before",
                table: "Matches",
                newName: "Player2RatingBefore");

            migrationBuilder.RenameColumn(
                name: "player2_rating_after",
                table: "Matches",
                newName: "Player2RatingAfter");

            migrationBuilder.RenameColumn(
                name: "player2_id",
                table: "Matches",
                newName: "Player2Id");

            migrationBuilder.RenameColumn(
                name: "player2_disconnected_at",
                table: "Matches",
                newName: "Player2DisconnectedAt");

            migrationBuilder.RenameColumn(
                name: "player1_reconnect_token",
                table: "Matches",
                newName: "Player1ReconnectToken");

            migrationBuilder.RenameColumn(
                name: "player1_rating_before",
                table: "Matches",
                newName: "Player1RatingBefore");

            migrationBuilder.RenameColumn(
                name: "player1_rating_after",
                table: "Matches",
                newName: "Player1RatingAfter");

            migrationBuilder.RenameColumn(
                name: "player1_id",
                table: "Matches",
                newName: "Player1Id");

            migrationBuilder.RenameColumn(
                name: "player1_disconnected_at",
                table: "Matches",
                newName: "Player1DisconnectedAt");

            migrationBuilder.RenameColumn(
                name: "match_id",
                table: "Matches",
                newName: "MatchId");

            migrationBuilder.RenameColumn(
                name: "duration_seconds",
                table: "Matches",
                newName: "DurationSeconds");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Matches",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                table: "Matches",
                newName: "CompletedAt");

            migrationBuilder.RenameIndex(
                name: "IX_matches_player2_id_created_at",
                table: "Matches",
                newName: "IX_Matches_Player2Id_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_matches_player1_id_created_at",
                table: "Matches",
                newName: "IX_Matches_Player1Id_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_matches_match_id",
                table: "Matches",
                newName: "IX_Matches_MatchId");

            migrationBuilder.RenameColumn(
                name: "sequence",
                table: "Effects",
                newName: "Sequence");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "Effects",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Effects",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "effect_kind",
                table: "Effects",
                newName: "EffectKind");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Effects",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ability_definition_id",
                table: "Effects",
                newName: "AbilityDefinitionId");

            migrationBuilder.RenameIndex(
                name: "IX_effects_ability_definition_id_sequence",
                table: "Effects",
                newName: "IX_Effects_AbilityDefinitionId_Sequence");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Decks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Decks",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Decks",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "display_name",
                table: "Decks",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "deck_id",
                table: "Decks",
                newName: "DeckId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Decks",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "card_ids",
                table: "Decks",
                newName: "CardIds");

            migrationBuilder.RenameIndex(
                name: "IX_decks_user_id_deck_id",
                table: "Decks",
                newName: "IX_Decks_UserId_DeckId");

            migrationBuilder.RenameColumn(
                name: "health",
                table: "Cards",
                newName: "Health");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Cards",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "attack",
                table: "Cards",
                newName: "Attack");

            migrationBuilder.RenameColumn(
                name: "armor",
                table: "Cards",
                newName: "Armor");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Cards",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Cards",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "unit_type",
                table: "Cards",
                newName: "UnitType");

            migrationBuilder.RenameColumn(
                name: "turns_until_can_attack",
                table: "Cards",
                newName: "TurnsUntilCanAttack");

            migrationBuilder.RenameColumn(
                name: "mana_cost",
                table: "Cards",
                newName: "ManaCost");

            migrationBuilder.RenameColumn(
                name: "is_limited",
                table: "Cards",
                newName: "IsLimited");

            migrationBuilder.RenameColumn(
                name: "display_name",
                table: "Cards",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "default_attack_selector",
                table: "Cards",
                newName: "DefaultAttackSelector");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Cards",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "card_type",
                table: "Cards",
                newName: "CardType");

            migrationBuilder.RenameColumn(
                name: "card_rarity",
                table: "Cards",
                newName: "CardRarity");

            migrationBuilder.RenameColumn(
                name: "card_id",
                table: "Cards",
                newName: "CardId");

            migrationBuilder.RenameColumn(
                name: "card_faction",
                table: "Cards",
                newName: "CardFaction");

            migrationBuilder.RenameColumn(
                name: "allowed_row",
                table: "Cards",
                newName: "AllowedRow");

            migrationBuilder.RenameIndex(
                name: "IX_cards_card_id",
                table: "Cards",
                newName: "IX_Cards_CardId");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Abilities",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Abilities",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Abilities",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "trigger_kind",
                table: "Abilities",
                newName: "TriggerKind");

            migrationBuilder.RenameColumn(
                name: "target_selector_kind",
                table: "Abilities",
                newName: "TargetSelectorKind");

            migrationBuilder.RenameColumn(
                name: "display_name",
                table: "Abilities",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Abilities",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ability_id",
                table: "Abilities",
                newName: "AbilityId");

            migrationBuilder.RenameIndex(
                name: "IX_abilities_ability_id",
                table: "Abilities",
                newName: "IX_Abilities_AbilityId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ReplayLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "ReplayLogs",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "match_id",
                table: "ReplayLogs",
                newName: "MatchId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ReplayLogs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "action_type",
                table: "ReplayLogs",
                newName: "ActionType");

            migrationBuilder.RenameColumn(
                name: "action_number",
                table: "ReplayLogs",
                newName: "ActionNumber");

            migrationBuilder.RenameColumn(
                name: "action_data",
                table: "ReplayLogs",
                newName: "ActionData");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "MatchActions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "player_id",
                table: "MatchActions",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "match_id",
                table: "MatchActions",
                newName: "MatchId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "MatchActions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "action_type",
                table: "MatchActions",
                newName: "ActionType");

            migrationBuilder.RenameColumn(
                name: "action_number",
                table: "MatchActions",
                newName: "ActionNumber");

            migrationBuilder.RenameColumn(
                name: "action_data",
                table: "MatchActions",
                newName: "ActionData");

            migrationBuilder.RenameIndex(
                name: "IX_match_actions_match_id_action_number",
                table: "MatchActions",
                newName: "IX_MatchActions_MatchId_ActionNumber");

            migrationBuilder.RenameIndex(
                name: "IX_match_actions_match_id",
                table: "MatchActions",
                newName: "IX_MatchActions_MatchId");

            migrationBuilder.RenameColumn(
                name: "sequence",
                table: "CardAbilities",
                newName: "Sequence");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CardAbilities",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "card_definition_id",
                table: "CardAbilities",
                newName: "CardDefinitionId");

            migrationBuilder.RenameColumn(
                name: "ability_definition_id",
                table: "CardAbilities",
                newName: "AbilityDefinitionId");

            migrationBuilder.RenameIndex(
                name: "IX_card_abilities_card_definition_id_sequence",
                table: "CardAbilities",
                newName: "IX_CardAbilities_CardDefinitionId_Sequence");

            migrationBuilder.RenameIndex(
                name: "IX_card_abilities_ability_definition_id",
                table: "CardAbilities",
                newName: "IX_CardAbilities_AbilityDefinitionId");

            migrationBuilder.RenameColumn(
                name: "resource",
                table: "AuditLogs",
                newName: "Resource");

            migrationBuilder.RenameColumn(
                name: "details",
                table: "AuditLogs",
                newName: "Details");

            migrationBuilder.RenameColumn(
                name: "action",
                table: "AuditLogs",
                newName: "Action");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AuditLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AuditLogs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "status_code",
                table: "AuditLogs",
                newName: "StatusCode");

            migrationBuilder.RenameColumn(
                name: "resource_id",
                table: "AuditLogs",
                newName: "ResourceId");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                table: "AuditLogs",
                newName: "IpAddress");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "AuditLogs",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_user_id",
                table: "AuditLogs",
                newName: "IX_AuditLogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_resource_resource_id",
                table: "AuditLogs",
                newName: "IX_AuditLogs_Resource_ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_created_at",
                table: "AuditLogs",
                newName: "IX_AuditLogs_CreatedAt");

            migrationBuilder.AddColumn<int>(
                name: "SkillType",
                table: "Abilities",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActionData",
                table: "ReplayLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ratings",
                table: "Ratings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Matches",
                table: "Matches",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Effects",
                table: "Effects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Decks",
                table: "Decks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cards",
                table: "Cards",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Abilities",
                table: "Abilities",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReplayLogs",
                table: "ReplayLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MatchActions",
                table: "MatchActions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CardAbilities",
                table: "CardAbilities",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CardAbilities_Abilities_AbilityDefinitionId",
                table: "CardAbilities",
                column: "AbilityDefinitionId",
                principalTable: "Abilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CardAbilities_Cards_CardDefinitionId",
                table: "CardAbilities",
                column: "CardDefinitionId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Effects_Abilities_AbilityDefinitionId",
                table: "Effects",
                column: "AbilityDefinitionId",
                principalTable: "Abilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Users_UserId",
                table: "Ratings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
