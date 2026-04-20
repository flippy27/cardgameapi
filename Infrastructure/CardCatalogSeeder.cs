using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure.Models;
using System.Security.Cryptography;
using System.Text;

namespace CardDuel.ServerApi.Infrastructure;

public static class CardCatalogSeeder
{
    private static readonly string[] CardNames = new[]
    {
        "Dragon Slayer", "Ice Mage", "Fire Knight", "Shadow Assassin", "Holy Priest",
        "Storm Caller", "Nature Guardian", "Zombie Horde", "Void Wraith", "Crystal Golem",
        "Demon Lord", "Angel Sentinel", "Undead Knight", "Forest Archer", "Ocean Leviathan",
        "Lightning Elemental", "Frost Giant", "Inferno Beast", "Dark Sorcerer", "Light Paladin",
        "Cursed Warrior", "Blessed Monk", "Swamp Troll", "Sky Drake", "Earth Colossus"
    };

    private static readonly string[] Factions = { "Ember", "Tidal", "Grove", "Alloy", "Void" };
    private static readonly string[] Rarities = { "Common", "Rare", "Epic", "Legendary" };
    private static readonly string[] Types = { "Unit", "Spell", "Artifact" };

    public static void SeedCards(AppDbContext db)
    {
        if (db.Abilities.Any()) return;

        // Create abilities (matching cardgame reference exactly)
        var abilities = CreateAbilities();
        foreach (var ability in abilities)
        {
            db.Abilities.Add(ability);
        }
        db.SaveChanges();

        // Create users
        var playerOne = CreateUser("playerone@flippy.com", "PlayerOne", "123456");
        var playerTwo = CreateUser("playertwo@flippy.com", "PlayerTwo", "123456");
        db.Users.Add(playerOne);
        db.Users.Add(playerTwo);
        db.SaveChanges();

        // Create 200 cards
        var cards = CreateCards(200);
        foreach (var card in cards)
        {
            db.Cards.Add(card);
        }
        db.SaveChanges();

        // Assign abilities to cards randomly
        AssignAbilitiesToCards(db, cards, abilities);

        // Create 10 decks (5 per player, 20 cards each)
        CreateDecksForUser(db, playerOne, cards, 5);
        CreateDecksForUser(db, playerTwo, cards, 5);

        db.SaveChanges();
    }

    private static List<AbilityDefinition> CreateAbilities()
    {
        return new List<AbilityDefinition>
        {
            // armor - Defensive
            new AbilityDefinition
            {
                AbilityId = "armor",
                DisplayName = "Armor",
                Description = "Absorbs incoming damage before health is reduced",
                TriggerKind = (int)TriggerKind.OnPlay,
                TargetSelectorKind = (int)TargetSelectorKind.Self,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.GainArmor, Amount = 5, Sequence = 0 }
                }
            },
            // shield - Defensive
            new AbilityDefinition
            {
                AbilityId = "shield",
                DisplayName = "Shield",
                Description = "Absorbs one full attack (divine shield)",
                TriggerKind = (int)TriggerKind.OnPlay,
                TargetSelectorKind = (int)TargetSelectorKind.Self,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.GainArmor, Amount = 10, Sequence = 0 }
                }
            },
            // fly - Modifier
            new AbilityDefinition
            {
                AbilityId = "fly",
                DisplayName = "Fly",
                Description = "Only flying units can attack this card",
                TriggerKind = (int)TriggerKind.OnPlay,
                TargetSelectorKind = (int)TargetSelectorKind.Self,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.BuffAttack, Amount = 1, Sequence = 0 }
                }
            },
            // trample - Modifier
            new AbilityDefinition
            {
                AbilityId = "trample",
                DisplayName = "Trample",
                Description = "Ignores armor when attacking",
                TriggerKind = (int)TriggerKind.OnBattlePhase,
                TargetSelectorKind = (int)TargetSelectorKind.AllEnemies,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.Damage, Amount = 2, Sequence = 0 }
                }
            },
            // poison - Offensive
            new AbilityDefinition
            {
                AbilityId = "poison",
                DisplayName = "Poison",
                Description = "Applies poison stacks: X damage per turn",
                TriggerKind = (int)TriggerKind.OnBattlePhase,
                TargetSelectorKind = (int)TargetSelectorKind.LowestHealthAlly,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.Damage, Amount = 1, Sequence = 0 }
                }
            },
            // stun - Offensive
            new AbilityDefinition
            {
                AbilityId = "stun",
                DisplayName = "Stun",
                Description = "Target skips next attack",
                TriggerKind = (int)TriggerKind.OnPlay,
                TargetSelectorKind = (int)TargetSelectorKind.LowestHealthAlly,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.Damage, Amount = 3, Sequence = 0 }
                }
            },
            // leech - Offensive
            new AbilityDefinition
            {
                AbilityId = "leech",
                DisplayName = "Leech",
                Description = "Heals hero equal to damage dealt",
                TriggerKind = (int)TriggerKind.OnBattlePhase,
                TargetSelectorKind = (int)TargetSelectorKind.Self,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.Heal, Amount = 2, Sequence = 0 }
                }
            },
            // enrage - Offensive
            new AbilityDefinition
            {
                AbilityId = "enrage",
                DisplayName = "Enrage",
                Description = "Gains +1 ATK when damaged",
                TriggerKind = (int)TriggerKind.OnTurnStart,
                TargetSelectorKind = (int)TargetSelectorKind.Self,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.BuffAttack, Amount = 2, Sequence = 0 }
                }
            },
            // regenerate - Utility
            new AbilityDefinition
            {
                AbilityId = "regenerate",
                DisplayName = "Regenerate",
                Description = "Heals X HP at end of turn",
                TriggerKind = (int)TriggerKind.OnTurnEnd,
                TargetSelectorKind = (int)TargetSelectorKind.Self,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.Heal, Amount = 1, Sequence = 0 }
                }
            },
            // taunt - Utility
            new AbilityDefinition
            {
                AbilityId = "taunt",
                DisplayName = "Taunt",
                Description = "All enemy attacks must target this card if alive",
                TriggerKind = (int)TriggerKind.OnPlay,
                TargetSelectorKind = (int)TargetSelectorKind.Self,
                Effects = new List<EffectDefinition>
                {
                    new EffectDefinition { EffectKind = (int)EffectKind.BuffAttack, Amount = 1, Sequence = 0 }
                }
            }
        };
    }

    private static List<CardDefinition> CreateCards(int count)
    {
        var cards = new List<CardDefinition>();
        var random = new Random(42);

        for (int i = 1; i <= count; i++)
        {
            var faction = Factions[random.Next(Factions.Length)];
            var rarity = Rarities[random.Next(Rarities.Length)];
            var type = Types[random.Next(Types.Length)];
            var name = CardNames[random.Next(CardNames.Length)];

            var card = new CardDefinition
            {
                CardId = $"{faction.ToLower()}_{i:D4}",
                DisplayName = $"{faction} {name} #{i}",
                Description = $"A {rarity} {type} from the {faction} faction",
                ManaCost = random.Next(1, 8),
                Attack = random.Next(1, 7),
                Health = random.Next(1, 8),
                Armor = random.Next(0, 3),
                CardType = (int)(type == "Unit" ? 0 : type == "Spell" ? 1 : 2),
                CardRarity = (int)Enum.Parse(typeof(CardRarity), rarity),
                CardFaction = (int)Enum.Parse(typeof(CardFaction), faction),
                UnitType = type == "Unit" ? (int)UnitType.Melee : null,
                AllowedRow = random.Next(0, 3),
                DefaultAttackSelector = random.Next(0, 5),
                TurnsUntilCanAttack = random.Next(0, 2),
                IsLimited = random.Next(100) > 90
            };
            cards.Add(card);
        }

        return cards;
    }

    private static void AssignAbilitiesToCards(AppDbContext db, List<CardDefinition> cards, List<AbilityDefinition> abilities)
    {
        var random = new Random(42);

        foreach (var card in cards)
        {
            // 0-3 abilities per card
            int abilityCount = random.Next(0, 4);
            var selectedAbilities = new List<int>();

            for (int i = 0; i < abilityCount; i++)
            {
                int abilityIndex = random.Next(abilities.Count);
                if (!selectedAbilities.Contains(abilityIndex))
                {
                    selectedAbilities.Add(abilityIndex);
                    var cardAbility = new CardAbilityDefinition
                    {
                        CardDefinitionId = card.Id,
                        AbilityDefinitionId = abilities[abilityIndex].Id,
                        Sequence = i
                    };
                    db.CardAbilities.Add(cardAbility);
                }
            }
        }
        db.SaveChanges();
    }

    private static UserAccount CreateUser(string email, string username, string password)
    {
        var hashedPassword = HashPassword(password);
        return new UserAccount
        {
            Email = email,
            Username = username,
            PasswordHash = hashedPassword,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void CreateDecksForUser(AppDbContext db, UserAccount user, List<CardDefinition> allCards, int deckCount)
    {
        var random = new Random(Guid.NewGuid().GetHashCode());

        for (int d = 0; d < deckCount; d++)
        {
            var deck = new PlayerDeck
            {
                UserId = user.Id,
                DeckId = $"deck_{user.Username.ToLower()}_{d + 1}",
                DisplayName = $"{user.Username}'s Deck {d + 1}",
                CardIds = new List<string>()
            };

            // Add exactly 20 cards per deck (shuffled)
            var shuffled = allCards.OrderBy(x => random.Next()).ToList();
            for (int i = 0; i < Math.Min(20, shuffled.Count); i++)
            {
                deck.CardIds.Add(shuffled[i].CardId);
            }

            db.Decks.Add(deck);
        }
        db.SaveChanges();
    }

    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
