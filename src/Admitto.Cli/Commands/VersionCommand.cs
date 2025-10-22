using System.Reflection;

namespace Amolenk.Admitto.Cli.Commands;

public sealed class VersionCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        
        AnsiConsole.WriteLine($"Admitto CLI version {version}");
        
        return 0;
    }
}