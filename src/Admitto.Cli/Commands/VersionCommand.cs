using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Amolenk.Admitto.Cli.Commands;

public sealed class VersionCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        
        AnsiConsole.MarkupLine($"[green]Admitto CLI[/] version [yellow]{version}[/]");
        
        if (!string.IsNullOrEmpty(informationalVersion) && informationalVersion != version?.ToString())
        {
            AnsiConsole.MarkupLine($"Build: [dim]{informationalVersion}[/]");
        }
        
        return 0;
    }
}