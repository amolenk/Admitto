using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Spectre.Console.Cli;

namespace Amolenk.Admitto.Cli;

public class CreateTeamSettings : CommandSettings
{
    [CommandOption("-n|--name")]
    [Description("A description of the team.")]
    public string Name { get; set; }

    [CommandOption("-e|--email")]
    [Required]
    [Description("A description of the team.")]
    public string Email { get; set; }

    // [CommandOption("--slug")]
    // [Description("A unique slug for the team.")]
    // public string SmtpServer { get; set; }
    //
    // [CommandOption("--slug")]
    // [Description("A unique slug for the team.")]
    // public int SmtpPort { get; set; }

}

public class CreateTeamCommand : AsyncCommand<CreateTeamSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, CreateTeamSettings settings)
    {
        throw new NotImplementedException();
    }
}