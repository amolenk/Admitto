namespace Amolenk.Admitto.Cli.Commands.Events;

public class CreateEventSettings : TeamSettings
{
    [CommandOption("-s|--slug")]
    [Description("Slug of the event to create (e.g. 'my-cool-event')")]
    public string? EventSlug { get; init; }

    [CommandOption("-n|--name")]
    [Description("The name of the event")]
    public string? Name { get; init; }

    [CommandOption("--website")]
    [Description("The website of the event")]
    public string? Website { get; init; }

    [CommandOption("--start")]
    [Description("The start date and time of the event.")]
    public DateTimeOffset? StartsAt { get; init; }

    [CommandOption("--end")]
    [Description("The end date and time of the event.")]
    public DateTimeOffset? EndsAt { get; init; }

    [CommandOption("--baseUrl")]
    [Description("The base URL for event links (e.g. qr-codes, cancellations, etc.)")]
    public string? BaseUrl { get; init; }

    [CommandOption("--requiredField")]
    [Description("Required custom field (in the format '<FieldName>=<MaxLength>') to collect additional information from attendees during registration (e.g. dietary preferences, company name, etc.).")]
    public string[]? RequiredAdditionalDetails { get; init; }

    [CommandOption("--optionalField")]
    [Description("Optional custom field (in the format '<FieldName>=<MaxLength>') to collect additional information from attendees during registration.")]
    public string[]? OptionalAdditionalDetails { get; init; }
    
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationResult.Error("Event name must be specified.");
        }
        
        if (string.IsNullOrWhiteSpace(EventSlug))
        {
            return ValidationResult.Error("Event slug must be specified.");
        }
        
        if (string.IsNullOrWhiteSpace(Website))
        {
            return ValidationResult.Error("Event website must be specified.");
        }
        
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            return ValidationResult.Error("Event base URL must be specified.");
        }
        
        if (StartsAt is null)
        {
            return ValidationResult.Error("Event start date and time must be specified.");
        }
        
        if (EndsAt is null)
        {
            return ValidationResult.Error("Event end date and time must be specified.");
        }
        
        return base.Validate();
    }
}

public class CreateEventCommand(OutputService outputService, IApiService apiService, IConfiguration configuration)
    : AsyncCommand<CreateEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateEventSettings settings)
    {
        var teamSlug = (settings.TeamSlug ?? configuration[ConfigSettings.DefaultTeamSetting])?.Kebaberize();
        if (string.IsNullOrWhiteSpace(teamSlug))
        {
            throw new ArgumentException("Team slug must be specified.");
        }

        var additionalDetailSchemas = ParseAdditionalDetailSchemas(
            settings.RequiredAdditionalDetails, 
            settings.OptionalAdditionalDetails);
        
        var request = new CreateTicketedEventRequest
        {
            Slug = settings.EventSlug!.Kebaberize(),
            Name = settings.Name,
            Website = settings.Website,
            BaseUrl = settings.BaseUrl,
            StartsAt = settings.StartsAt,
            EndsAt = settings.EndsAt,
            AdditionalDetailSchemas = additionalDetailSchemas
        };

        var succes =
            await apiService.CallApiAsync(async client => await client.Teams[teamSlug].Events.PostAsync(request));
        if (!succes) return 1;
        
        outputService.WriteSuccesMessage($"Successfully created event '{settings.Name}'.");
        return 0;
    }
    
    private static List<AdditionalDetailSchemaDto>? ParseAdditionalDetailSchemas(string[]? requiredDetails, string[]? optionalDetails)
    {
        var schemas = new List<AdditionalDetailSchemaDto>();

        if (requiredDetails is not null)
        {
            schemas.AddRange(ParseAdditionalDetailSchemas(requiredDetails, true));
        }

        if (optionalDetails is not null)
        {
            schemas.AddRange(ParseAdditionalDetailSchemas(optionalDetails, false));
        }
        
        return schemas.Count > 0 ? schemas : null;
    }

    private static IEnumerable<AdditionalDetailSchemaDto> ParseAdditionalDetailSchemas(string[] details, bool required)
    {
        foreach (var detail in details)
        {
            var parts = detail.Split('=', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) ||
                !int.TryParse(parts[1], out var maxLength) || maxLength <= 0)
            {
                throw new ArgumentException(
                    $"Invalid additional detail format: '{detail}'. Expected format is 'DetailName=<max-length>'");
            }

            yield return new AdditionalDetailSchemaDto
            {
                Name = parts[0].Trim(),
                MaxLength = maxLength,
                IsRequired = required
            };
        }
    }
}