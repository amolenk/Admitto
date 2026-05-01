using FluentValidation;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

public sealed class PreviewBulkEmailValidator : AbstractValidator<PreviewBulkEmailHttpRequest>
{
    public PreviewBulkEmailValidator()
    {
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
