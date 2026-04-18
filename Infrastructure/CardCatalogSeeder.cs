using System.Text.Json;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Infrastructure;

public static class CardCatalogSeeder
{
    public static void SeedCards(AppDbContext db)
    {
        if (db.Cards.Any()) return;

        var cards = BuildCatalog();
        foreach (var card in cards)
        {
            db.Cards.Add(card);
        }
        db.SaveChanges();
    }

    private static List<CardDefinition> BuildCatalog()
    {
        var cards = new List<CardDefinition>();

        cards.Add(Card("ember_vanguard", "Ember Vanguard", 2, 3, 3, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst));
        cards.Add(Card("ember_archer", "Ember Archer", 2, 2, 2, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst));
        cards.Add(Card("ember_burnseer", "Burnseer", 3, 2, 3, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst,
            Ability("hero_ping", TriggerKind.OnTurnEnd, TargetSelectorKind.Self, Effect(EffectKind.HitHero, 2))));

        cards.Add(Card("tidal_priest", "Tidal Priest", 2, 1, 3, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst,
            Ability("battle_heal", TriggerKind.OnTurnEnd, TargetSelectorKind.LowestHealthAlly, Effect(EffectKind.Heal, 2))));
        cards.Add(Card("tidal_lancer", "Tidal Lancer", 2, 3, 2, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst));
        cards.Add(Card("tidal_sniper", "Tidal Sniper", 3, 3, 2, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst));

        cards.Add(Card("grove_guardian", "Grove Guardian", 3, 2, 5, 1, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst));
        cards.Add(Card("grove_shaper", "Grove Shaper", 3, 1, 4, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst,
            Ability("battle_buff", TriggerKind.OnTurnStart, TargetSelectorKind.Self, Effect(EffectKind.BuffAttack, 1))));
        cards.Add(Card("grove_raincaller", "Raincaller", 2, 1, 3, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst,
            Ability("ally_heal", TriggerKind.OnTurnEnd, TargetSelectorKind.LowestHealthAlly, Effect(EffectKind.Heal, 2))));

        cards.Add(Card("alloy_bulwark", "Alloy Bulwark", 3, 2, 4, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst,
            Ability("armor_on_play", TriggerKind.OnPlay, TargetSelectorKind.Self, Effect(EffectKind.GainArmor, 2))));
        cards.Add(Card("alloy_ballista", "Alloy Ballista", 4, 4, 2, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst));
        cards.Add(Card("alloy_hoplite", "Alloy Hoplite", 2, 2, 3, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst));

        cards.Add(Card("void_stalker", "Void Stalker", 2, 3, 2, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst));
        cards.Add(Card("void_caller", "Void Caller", 4, 2, 3, 0, AllowedRow.BackOnly, TargetSelectorKind.FrontlineFirst,
            Ability("splash", TriggerKind.OnBattlePhase, TargetSelectorKind.AllEnemies, Effect(EffectKind.Damage, 1))));
        cards.Add(Card("void_magus", "Void Magus", 4, 3, 4, 0, AllowedRow.BackOnly, TargetSelectorKind.FrontlineFirst,
            Ability("self_buff", TriggerKind.OnTurnStart, TargetSelectorKind.Self, Effect(EffectKind.BuffAttack, 1))));

        cards.Add(Card("ember_colossus", "Ember Colossus", 5, 5, 6, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst));
        cards.Add(Card("tidal_waveblade", "Waveblade", 1, 2, 1, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst));
        cards.Add(Card("grove_myr", "Grove Myr", 1, 1, 2, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst));

        return cards;
    }

    private static CardDefinition Card(string cardId, string name, int mana, int atk, int hp, int armor, AllowedRow row, TargetSelectorKind selector, params object[] abilities)
    {
        var json = abilities.Length == 0 ? "[]" : JsonSerializer.Serialize(abilities.Cast<dynamic>().ToList());
        return new CardDefinition
        {
            CardId = cardId,
            DisplayName = name,
            ManaCost = mana,
            Attack = atk,
            Health = hp,
            Armor = armor,
            AllowedRow = (int)row,
            DefaultAttackSelector = (int)selector,
            AbilitiesJson = json
        };
    }

    private static dynamic Ability(string id, TriggerKind trigger, TargetSelectorKind selector, params dynamic[] effects)
    {
        return new { AbilityId = id, Trigger = (int)trigger, Selector = (int)selector, Effects = effects.ToList() };
    }

    private static dynamic Effect(EffectKind kind, int amount)
    {
        return new { Kind = (int)kind, Amount = amount };
    }
}
