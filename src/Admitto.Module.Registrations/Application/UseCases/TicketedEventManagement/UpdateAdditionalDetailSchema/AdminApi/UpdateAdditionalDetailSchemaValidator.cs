using System.Text.RegularExpressions;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema.AdminApi;

public sealed partial class UpdateAdditionalDetailSchemaValidator
    : AbstractValidator<UpdateAdditionalDetailSchemaHttpRequest>
{
    public UpdateAdditionalDetailSchemaValidator()
    {
        RuleFor(x => x.Fields)
            .NotNull()
            .Must(fields => fields.Count <= AdditionalDetailSchema.MaxFields)
            .WithMessage($"An event may have at most {AdditionalDetailSchema.MaxFields} additional detail fields.");

        RuleFor(x => x.Fields)
            .Must(HaveUniqueKeys)
            .WithMessage("Field keys must be unique within the schema.")
            .When(x => x.Fields is not null);

        RuleFor(x => x.Fields)
            .Must(HaveUniqueNames)
            .WithMessage("Field names must be unique within the schema (case-insensitive).")
            .When(x => x.Fields is not null);

        RuleForEach(x => x.Fields).ChildRules(field =>
        {
            field.RuleFor(f => f.Key)
                .NotEmpty()
                .Matches(KeyRegex())
                .WithMessage("Field key must match '^[a-z0-9][a-z0-9-]{0,49}$'.");

            field.RuleFor(f => f.Name)
                .NotEmpty()
                .MaximumLength(AdditionalDetailField.NameMaxLength);

            field.RuleFor(f => f.MaxLength)
                .InclusiveBetween(1, AdditionalDetailField.MaxValueLength);
        });
    }

    private static bool HaveUniqueKeys(IReadOnlyList<UpdateAdditionalDetailSchemaHttpRequest.FieldDto> fields) =>
        fields.Select(f => f.Key).Distinct(StringComparer.Ordinal).Count() == fields.Count;

    private static bool HaveUniqueNames(IReadOnlyList<UpdateAdditionalDetailSchemaHttpRequest.FieldDto> fields) =>
        fields.Where(f => !string.IsNullOrWhiteSpace(f.Name))
            .Select(f => f.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).Count()
            == fields.Count(f => !string.IsNullOrWhiteSpace(f.Name));

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,49}$")]
    private static partial Regex KeyRegex();
}
