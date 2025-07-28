using System.Text.Json.Nodes;

namespace Amolenk.Admitto.Cli.Commands.Config;

public class ConfigSettings : CommandSettings
{
    public const string EndpointSetting = "endpoint";
    public const string DefaultTeamSetting = "defaultTeam";
    public const string DefaultEventSetting = "defaultEvent";
    
    public static string GetConfigPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "admitto-cli", "config.json");
}

public abstract class ConfigCommandBase<TSettings> : Command<TSettings> where TSettings : ConfigSettings
{
    protected static JsonObject GetConfig()
    {
        var path = ConfigSettings.GetConfigPath();
        return LoadConfig(path);
    }
    
    protected static void UpdateConfig(Action<JsonObject> updateAction)
    {
        var path = ConfigSettings.GetConfigPath();
        var config = LoadConfig(path);
        
        updateAction(config);
        
        File.WriteAllText(path, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
    
    private static JsonObject LoadConfig(string path)
    {
        JsonObject config;
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            config = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        }
        else
        {
            config = new JsonObject();
        }

        return config;
    }
}