using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Registration;

public class GetRegistrationSettings : TeamEventSettings
{
    [CommandOption("-r|--registrationId")]
    public Guid? RegistrationId { get; set; }

    [CommandOption("--qrCodeOutputPath")]
    public string? QRCodeFilePath { get; set; }

    public override ValidationResult Validate()
    {
        if (RegistrationId is null)
        {
            return ValidationErrors.RegistrationIdMissing;
        }

        return base.Validate();
    }
}

public class GetRegistrationCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<GetRegistrationSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, GetRegistrationSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Registrations[settings.RegistrationId!.Value].GetAsync());
        if (response is null) return 1;

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 7 });
        grid.AddColumn();

        grid.AddRow("Email:", response.Email!);
        grid.AddRow("Status:", response.Status!.Value.Format());

        AnsiConsole.Write(grid);

        if (settings.QRCodeFilePath is null) return 0;

        await using var qrCodeStream = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Registrations[settings.RegistrationId!.Value].QrCode.GetAsync(c =>
            {
                c.QueryParameters.Signature = response.Signature;
            }));
        if (qrCodeStream is null)
        {
            AnsiConsole.MarkupLine($"[red]Failed to retrieve QR code.[/]");
            return 1;
        }
        
        var filePath = settings.QRCodeFilePath;
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(filePath))
        {
            var overwrite = await AnsiConsole.ConfirmAsync($"File '{filePath}' exists. Overwrite?");
            if (!overwrite)
            {
                AnsiConsole.MarkupLine("[yellow]QR code not saved.[/]");
                return 1;
            }
        }

        await using var fileStream = File.Create(filePath);
        await qrCodeStream.CopyToAsync(fileStream);
        AnsiConsole.MarkupLine($"[green]QR code saved to '{filePath}'.[/]");

        return 0;
    }
}