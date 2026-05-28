using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.AddBadgeType.AdminApi;

public sealed class AddBadgeTypeValidator : AbstractValidator<AddBadgeTypeHttpRequest>
{
    private static readonly string ValidKindsMessage =
        $"Kind must be one of: {string.Join(", ", Enum.GetNames<BadgeKind>())}.";

    public AddBadgeTypeValidator()
    {
        RuleFor(x => x.Name).MustBeParseable(BadgeTypeName.TryFrom);
        RuleFor(x => x.Kind).NotEmpty()
            .Must(k => k is not null && Enum.TryParse<BadgeKind>(k, ignoreCase: true, out _))
            .WithMessage(ValidKindsMessage);
        RuleFor(x => x.TicketTypeIds)
            .NotEmpty()
            .When(x => x.Kind.Equals(nameof(BadgeKind.TicketBased), StringComparison.OrdinalIgnoreCase))
            .WithMessage("A ticket-based badge type must reference at least one ticket type.");
    }
}
