using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance.AdminApi;

public sealed class UpdateBadgeInstanceValidator : AbstractValidator<UpdateBadgeInstanceHttpRequest>
{
    public UpdateBadgeInstanceValidator()
    {
        RuleFor(x => x.DisplayName).MustBeParseable(BadgeInstanceDisplayName.TryFrom);
        RuleFor(x => x.Notes).MustBeNullOrParseable(BadgeInstanceNotes.TryFrom);
    }
}
