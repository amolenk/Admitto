using FluentValidation;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

public sealed class CreateBulkEmailValidator : AbstractValidator<CreateBulkEmailHttpRequest>
{
    public CreateBulkEmailValidator()
    {
        RuleFor(x => x.EmailType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.TextBody)
            .NotEmpty();

        RuleFor(x => x.HtmlBody)
            .NotEmpty();

        RuleFor(x => x.Source).NotNull();

        RuleFor(x => x.Source).Custom((source, ctx) =>
        {
            if (source is null) return;

            var hasAttendee = source.Attendee is not null;
            var hasExternal = source.ExternalList is not null;

            if (hasAttendee == hasExternal)
            {
                ctx.AddFailure(
                    nameof(source),
                    "Exactly one of 'attendee' or 'externalList' must be specified.");
            }

            if (hasExternal && source.ExternalList!.Items.Count == 0)
            {
                ctx.AddFailure(
                    "source.externalList.items",
                    "External list must contain at least one recipient.");
            }
        });
    }
}
