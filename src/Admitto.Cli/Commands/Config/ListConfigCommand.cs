using System.Text.Json.Nodes;

namespace Amolenk.Admitto.Cli.Commands.Config;

public class GetConfigCommand : ConfigCommandBase<ConfigSettings>
{
    public override int Execute(CommandContext context, ConfigSettings settings)
    {
        var config = GetConfig();

        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Value");

        table.AddRow("Endpoint", GetSettingValue(config, ConfigSettings.EndpointSetting));
        table.AddRow("Default Team", GetSettingValue(config, ConfigSettings.DefaultTeamSetting));
        table.AddRow("Default Event", GetSettingValue(config, ConfigSettings.DefaultEventSetting));

        AnsiConsole.Write(table);
        
        return 0;
    }
    
    private static string GetSettingValue(JsonObject config, string settingName)
    {
        return config[settingName]?.GetValue<string>() ?? "[grey]Not set[/]";
    }
}