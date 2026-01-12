namespace Amolenk.Admitto.Cli.Configuration;

public interface IConfigService
{
    string? DefaultTeam { get; set; }
    
    string? DefaultEvent { get; set; }
}

public class ConfigService : IConfigService
{
    private const string DefaultTeamKey = "defaultTeam";
    private const string DefaultEventKey = "defaultEvent";
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    
    private readonly string _filePath;
    private Dictionary<string, string> _defaults = null!;
    
    public ConfigService(string filePath)
    {
        _filePath = filePath;
        Load();
    }
    
    public string? DefaultTeam
    {
        get => GetDefault(DefaultTeamKey);
        set => SetDefault(DefaultTeamKey, value);
    }
    
    public string? DefaultEvent
    {
        get => GetDefault(DefaultEventKey);
        set => SetDefault(DefaultEventKey, value);
    }

    private string? GetDefault(string key)
    {
        _defaults.TryGetValue(key, out var value);
        return value;
    }
    
    private void SetDefault(string key, string? value)
    {
        if (value is null)
        {
            _defaults.Remove(key);
            return;
        }
        
        _defaults[key] = value;
        Save();
    }
    
    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _defaults = new Dictionary<string, string>();
        }
        
        try
        {
            var json = File.ReadAllText(_filePath);
            var items = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            _defaults = items ?? new Dictionary<string, string>();
        }
        catch
        {
            _defaults = new Dictionary<string, string>();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_defaults, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}