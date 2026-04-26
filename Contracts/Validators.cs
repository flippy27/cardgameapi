using FluentValidation;

namespace CardDuel.ServerApi.Contracts;

public sealed class PlayCardRequestValidator : AbstractValidator<PlayCardRequest>
{
    public PlayCardRequestValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId is required")
            .MaximumLength(255).WithMessage("PlayerId too long");

        RuleFor(x => x.RuntimeHandKey)
            .NotEmpty().WithMessage("RuntimeHandKey is required");

        RuleFor(x => x.SlotIndex)
            .InclusiveBetween(0, 2).WithMessage("SlotIndex must be 0-2 (Front, BackLeft, BackRight)");
    }
}

public sealed class EndTurnRequestValidator : AbstractValidator<EndTurnRequest>
{
    public EndTurnRequestValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId is required")
            .MaximumLength(255).WithMessage("PlayerId too long");
    }
}

public sealed class DestroyCardRequestValidator : AbstractValidator<DestroyCardRequest>
{
    public DestroyCardRequestValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId is required")
            .MaximumLength(255).WithMessage("PlayerId too long");

        RuleFor(x => x.RuntimeCardId)
            .NotEmpty().WithMessage("RuntimeCardId is required");
    }
}

public sealed class SetReadyRequestValidator : AbstractValidator<SetReadyRequest>
{
    public SetReadyRequestValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId is required");
    }
}

public sealed class DeckUpsertRequestValidator : AbstractValidator<DeckUpsertRequest>
{
    public DeckUpsertRequestValidator()
    {
        RuleFor(x => x.DeckId)
            .NotEmpty().WithMessage("DeckId is required")
            .MaximumLength(128).WithMessage("DeckId too long");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required")
            .MaximumLength(255).WithMessage("DisplayName too long");

        RuleFor(x => x.CardIds)
            .NotEmpty().WithMessage("CardIds required")
            .Must(ids => ids.Count >= 20 && ids.Count <= 30).WithMessage("Deck must have 20-30 cards")
            .Must(ids => ids.GroupBy(x => x).All(g => g.Count() <= 3)).WithMessage("Max 3 copies per card");
    }
}

public sealed class MatchCompletionRequestValidator : AbstractValidator<MatchCompletionRequest>
{
    public MatchCompletionRequestValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId is required");

        RuleFor(x => x.OpponentId)
            .NotEmpty().WithMessage("OpponentId is required");

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0).WithMessage("DurationSeconds must be positive");
    }
}
