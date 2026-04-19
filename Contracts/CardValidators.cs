using FluentValidation;

namespace CardDuel.ServerApi.Contracts;

public sealed class CreateCardRequestValidator : AbstractValidator<CreateCardRequest>
{
    public CreateCardRequestValidator()
    {
        RuleFor(x => x.CardId)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(128)
            .Matches(@"^[a-z0-9_]+$")
            .WithMessage("CardId must be lowercase alphanumeric with underscores");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(1024);

        RuleFor(x => x.ManaCost)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20);

        RuleFor(x => x.Attack)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20);

        RuleFor(x => x.Health)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(20);

        RuleFor(x => x.Armor)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10);

        RuleFor(x => x.CardType)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(3);

        RuleFor(x => x.CardRarity)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(3);

        RuleFor(x => x.CardFaction)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(4);

        RuleFor(x => x.AllowedRow)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(2);

        RuleFor(x => x.DefaultAttackSelector)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(4);

        RuleFor(x => x.TurnsUntilCanAttack)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(5);
    }
}

public sealed class UpdateCardRequestValidator : AbstractValidator<UpdateCardRequest>
{
    public UpdateCardRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .MinimumLength(2)
            .MaximumLength(255)
            .When(x => x.DisplayName != null);

        RuleFor(x => x.Description)
            .MaximumLength(1024)
            .When(x => x.Description != null);

        RuleFor(x => x.ManaCost)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20)
            .When(x => x.ManaCost.HasValue);

        RuleFor(x => x.Attack)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20)
            .When(x => x.Attack.HasValue);

        RuleFor(x => x.Health)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(20)
            .When(x => x.Health.HasValue);

        RuleFor(x => x.Armor)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10)
            .When(x => x.Armor.HasValue);
    }
}

public sealed class CreateAbilityRequestValidator : AbstractValidator<CreateAbilityRequest>
{
    public CreateAbilityRequestValidator()
    {
        RuleFor(x => x.AbilityId)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(128)
            .Matches(@"^[a-z0-9_]+$");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(512);

        RuleFor(x => x.TriggerKind)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(3);

        RuleFor(x => x.TargetSelectorKind)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(4);

        RuleFor(x => x.Effects)
            .NotEmpty()
            .WithMessage("Must have at least one effect");

        RuleForEach(x => x.Effects)
            .SetValidator(new CreateEffectRequestValidator());
    }
}

public sealed class CreateEffectRequestValidator : AbstractValidator<CreateEffectRequest>
{
    public CreateEffectRequestValidator()
    {
        RuleFor(x => x.EffectKind)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(26);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x.Sequence)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10);
    }
}
