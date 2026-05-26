using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.RenameBadgeType.AdminApi;

public sealed class RenameBadgeTypeValidator : AbstractValidator<RenameBadgeTypeHttpRequest>
{
    public RenameBadgeTypeValidator()
    {
        RuleFor(x => x.Name).MustBeParseable(BadgeTypeName.TryFrom);
    }
}
