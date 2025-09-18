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
}

public class CreateEventCommand(InputService inputService, OutputService outputService, IApiService apiService)
    : AsyncCommand<CreateEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateEventSettings settings)
    {
        var teamSlug = settings.TeamSlug?.Kebaberize() ?? inputService.GetTeamSlug();
        var request = CreateRequest(settings);

        var succes =
            await apiService.CallApiAsync(async client => await client.Teams[teamSlug].Events.PostAsync(request));
        if (!succes) return 1;
        
        outputService.WriteSuccesMessage($"Successfully created event '{settings.Name}'.");
        return 0;
    }

    private CreateTicketedEventRequest CreateRequest(CreateEventSettings settings)
    {
        var name = settings.Name ?? inputService.GetString("Event name");
        var slug = settings.EventSlug?.Kebaberize() ??
                   inputService.GetString("Event slug", name.Kebaberize(), kebaberize: true);
        var website = settings.Website ?? inputService.GetString("Event website");
        
        var baseUrl = settings.Website ?? inputService.GetString("Event base URL");
        var startsAt = settings.StartsAt ?? inputService.GetDateTimeOffset("Event start");
        var endsAt = settings.EndsAt ?? inputService.GetDateTimeOffset("Event end");
        var additionalDetailSchemas =
            ParseAdditionalDetailSchemas(settings.RequiredAdditionalDetails, settings.OptionalAdditionalDetails)
            ?? GetAdditionalDetailSchemas();
        
        return new CreateTicketedEventRequest
        {
            Slug = slug,
            Name = settings.Name,
            Website = settings.Website,
            BaseUrl = settings.BaseUrl,
            StartsAt = settings.StartsAt,
            EndsAt = settings.EndsAt,
            AdditionalDetailSchemas = additionalDetailSchemas
        };
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
    
    private List<AdditionalDetailSchemaDto> GetAdditionalDetailSchemas()
    {
        var schemas = new List<AdditionalDetailSchemaDto>();
        
        while (true)
        {
            var schema = TryGetAdditionalDetailSchema();
            if (schema is null) break;
            schemas.Add(schema);
        }

        return schemas;
    }
    
    private AdditionalDetailSchemaDto? TryGetAdditionalDetailSchema()
    {
        var name = inputService.GetString("[[Optional]] Custom field for registration", allowEmpty: true);
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        
        var maxLength = inputService.GetNumber($"{name} max length", 1,255, 50);
        var isRequired = inputService.GetBoolean($"Is {name} required", false);

        return new AdditionalDetailSchemaDto
        {
            Name = name,
            MaxLength = maxLength,
            IsRequired = isRequired
        };
    }
}