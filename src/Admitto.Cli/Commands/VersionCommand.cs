using System.Reflection;

namespace Amolenk.Admitto.Cli.Commands;

public sealed class VersionCommand(OutputService outputService) : Command
{
    public override int Execute(CommandContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        
        outputService.WriteMarkupLine($"[green]Admitto CLI[/] version [yellow]{version}[/]");
        
        return 0;
    }
}