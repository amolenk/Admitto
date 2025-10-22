using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ShowSettings : TeamEventSettings
{
    [CommandOption("--id")]
    [Description("The id of the attendee")]
    public Guid? Id { get; set; }

    public override ValidationResult Validate()
    {
        return Id is null ? ValidationErrors.IdMissing : base.Validate();
    }
    
    // [CommandOption("--qrCodeOutputPath")]
    // public string? QRCodeFilePath { get; set; }
}

public class ShowAttendeeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ShowSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ShowSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
        
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[settings.Id!.Value].GetAsync());
        if (response is null) return 1;

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 7 });
        grid.AddColumn();

        grid.AddRow("Email:", response.Email!);
        grid.AddRow("Registration status:", response.RegistrationStatus!.Value.Format());

        AnsiConsole.Write(grid);

        // if (settings.QRCodeFilePath is null) return 0;
        //
        // await using var qrCodeStream = await CallApiAsync(async client =>
        //     await client.Teams[teamSlug].Events[eventSlug].Attendees[settings.RegistrationId!.Value].QrCode.GetAsync(c =>
        //     {
        //         c.QueryParameters.Signature = response.Signature;
        //     }));
        // if (qrCodeStream is null)
        // {
        //     outputService.WriteErrorMessage("Failed to retrieve QR code.");
        //     return 1;
        // }
        //
        // var filePath = settings.QRCodeFilePath;
        // var directory = Path.GetDirectoryName(filePath);
        // if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        // {
        //     Directory.CreateDirectory(directory);
        // }
        //
        // if (File.Exists(filePath))
        // {
        //     var overwrite = await AnsiConsole.ConfirmAsync($"File '{filePath}' exists. Overwrite?");
        //     if (!overwrite)
        //     {
        //         outputService.WriteWarning("QR code not saved.");
        //         return 1;
        //     }
        // }
        //
        // await using var fileStream = File.Create(filePath);
        // await qrCodeStream.CopyToAsync(fileStream);
        // outputService.WriteSuccesMessage($"QR code saved to '{filePath}'.");

        return 0;
    }
}