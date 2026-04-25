using System.Text.Json;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Infrastructure;

public static class AuthoringSeeder
{
    public static void SeedAuthoringData(AppDbContext db)
    {
        if (!db.CardVisualProfileTemplates.Any())
        {
            db.CardVisualProfileTemplates.AddRange(
                Template("hand-default", "Hand Default", "Baseline hand-card composition with bg, frame, art, nameplate and rules area.",
                    Layer("hand", "bg", "sprite", "card/bg/common_blue", 0),
                    Layer("hand", "frame", "sprite", "card/frame/common_metal", 10, "{\"rarity\":\"common\"}"),
                    Layer("hand", "art", "image", "card/art/{{cardId}}", 20, "{\"crop\":\"portrait\"}"),
                    Layer("hand", "nameplate", "sprite", "card/ui/nameplate_default", 30),
                    Layer("hand", "rules", "sprite", "card/ui/rules_text_default", 40, "{\"font\":\"serif-small\"}")),
                Template("played-default", "Played Default", "Baseline played-board composition with board frame and compact art crop.",
                    Layer("played", "frame", "sprite", "board/frame/common_metal", 0),
                    Layer("played", "art", "image", "card/art/{{cardId}}", 10, "{\"crop\":\"board-close\"}")),
                Template("hand-full-art", "Hand Full Art", "Full-bleed premium hand card composition.",
                    Layer("hand", "art", "image", "card/art/{{cardId}}_full", 0, "{\"fullArt\":true,\"safeTextTop\":0.14,\"safeTextBottom\":0.22}"),
                    Layer("hand", "frame", "sprite", "card/frame/legendary_fullart", 20, "{\"rarity\":\"legendary\"}"),
                    Layer("hand", "foil", "sprite", "card/fx/foil_rainbow_soft", 30, "{\"scrollSpeed\":0.15}")),
                Template("reward-legendary", "Reward Legendary", "Reward/pick screen composition for legendary cards.",
                    Layer("reward", "art", "image", "card/art/{{cardId}}_reward", 0, "{\"fullBleed\":true}"),
                    Layer("reward", "frame", "sprite", "card/frame/reward_legendary", 10),
                    Layer("reward", "burst", "sprite", "card/fx/reward_burst_legendary", 20)),
                Template("decklist-minimal", "Decklist Minimal", "Small deck builder list/thumbnail composition.",
                    Layer("decklist", "thumbnail", "image", "card/thumb/{{cardId}}", 0, "{\"shape\":\"square\"}"),
                    Layer("decklist", "rarity-pip", "sprite", "card/icon/rarity_rare", 10)),
                Template("status-indicator-default", "Status Indicator Default", "Reusable status/buff/debuff icon layer family for authoring docs and future UI.",
                    Layer("status", "icon-bg", "sprite", "status/bg/default", 0),
                    Layer("status", "icon", "sprite", "status/{{statusKey}}", 10, "{\"size\":\"small\"}"),
                    Layer("status", "duration-badge", "sprite", "status/ui/duration_badge", 20, "{\"visibleWhen\":\"remainingTurns>0\"}")));
        }

        foreach (var ability in db.Abilities)
        {
            if (!string.IsNullOrWhiteSpace(ability.IconAssetRef))
            {
                continue;
            }

            ability.IconAssetRef = $"abilities/{ability.AbilityId}";
            ability.StatusIconAssetRef = ability.AbilityId switch
            {
                "poison" => "status/poisoned",
                "stun" => "status/stunned",
                "shield" => "status/shielded",
                "enrage" => "status/enrage_cooldown",
                _ => null
            };
            ability.VfxCueId = $"vfx_{ability.AbilityId}";
            ability.AudioCueId = $"sfx_{ability.AbilityId}";
            ability.TooltipSummary = ability.Description;
        }

        db.SaveChanges();
    }

    private static CardVisualProfileTemplate Template(string key, string displayName, string description, params object[] layers) => new()
    {
        ProfileKey = key,
        DisplayName = displayName,
        Description = description,
        IsActive = true,
        LayersJson = JsonSerializer.Serialize(layers),
        MetadataJson = "{}"
    };

    private static object Layer(string surface, string layer, string sourceKind, string assetRef, int sortOrder, string metadataJson = "{}") => new
    {
        Surface = surface,
        Layer = layer,
        SourceKind = sourceKind,
        AssetRef = assetRef,
        SortOrder = sortOrder,
        MetadataJson = metadataJson
    };
}
