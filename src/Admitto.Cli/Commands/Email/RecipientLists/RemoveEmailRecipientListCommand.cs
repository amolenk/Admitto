using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.RecipientLists;

public class RemoveEmailRecipientListSettings : TeamEventSettings
{
    [CommandOption("--name")]
    [Description("The name of the list")]
    public string? ListName { get; init; }

    public override ValidationResult Validate()
    {
        if (ListName is null)
        {
            return ValidationErrors.NameMissing;
        }

        return base.Validate();
    }
}

public class RemoveEmailRecipientListCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<RemoveEmailRecipientListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RemoveEmailRecipientListSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var result = await admittoService.SendAsync(client =>
            client.RemoveEmailRecipientListAsync(teamSlug, eventSlug, settings.ListName, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully removed {settings.ListName} recipient list.");
        return 0;
    }
}