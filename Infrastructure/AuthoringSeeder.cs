using System.Text.Json;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Infrastructure;

public static class AuthoringSeeder
{
    /// <summary>
    /// Seeds lookup definitions (SkillTypes, TriggerKinds, etc.) required by abilities.
    /// Must be called BEFORE CardCatalogSeeder.SeedCards()
    /// </summary>
    public static void SeedLookupDefinitions(AppDbContext db)
    {
        // Seed SkillType definitions
        if (!db.SkillTypeDefinitions.Any())
        {
            db.SkillTypeDefinitions.AddRange(AuthoringDefinitions.SkillTypes);
            db.SaveChanges();
        }

        // Seed TriggerKind definitions
        if (!db.TriggerKindDefinitions.Any())
        {
            db.TriggerKindDefinitions.AddRange(AuthoringDefinitions.TriggerKinds);
            db.SaveChanges();
        }

        // Seed TargetSelectorKind definitions
        if (!db.TargetSelectorKindDefinitions.Any())
        {
            db.TargetSelectorKindDefinitions.AddRange(AuthoringDefinitions.TargetSelectors);
            db.SaveChanges();
        }

        // Seed EffectKind definitions
        if (!db.EffectKindDefinitions.Any())
        {
            db.EffectKindDefinitions.AddRange(AuthoringDefinitions.EffectKinds);
            db.SaveChanges();
        }

        // Seed StatusEffectKind definitions
        if (!db.StatusEffectKindDefinitions.Any())
        {
            db.StatusEffectKindDefinitions.AddRange(AuthoringDefinitions.StatusEffectKinds);
            db.SaveChanges();
        }

        // Seed ItemType definitions
        if (!db.ItemTypeDefinitions.Any())
        {
            db.ItemTypeDefinitions.AddRange(AuthoringDefinitions.ItemTypes);
            db.SaveChanges();
        }
    }
}
