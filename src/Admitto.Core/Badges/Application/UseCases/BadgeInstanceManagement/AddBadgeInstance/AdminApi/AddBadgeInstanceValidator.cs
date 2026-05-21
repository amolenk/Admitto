using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.AddBadgeInstance.AdminApi;

public sealed class AddBadgeInstanceValidator : AbstractValidator<AddBadgeInstanceHttpRequest>
{
    public AddBadgeInstanceValidator()
    {
        RuleFor(x => x.DisplayName).MustBeParseable(BadgeInstanceDisplayName.TryFrom);
        RuleFor(x => x.Notes).MustBeNullOrParseable(BadgeInstanceNotes.TryFrom);
    }
}
