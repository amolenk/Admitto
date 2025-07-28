namespace Amolenk.Admitto.Cli.Services;

public class ConfigProvider
{
    public const string EndpointKey = "endpoint";
    public const string DefaultTeamKey = "default-team";
    public const string DefaultEventKey = "default-event";
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    
    private readonly string _filePath;
    private Dictionary<string, string> _items = null!;
    
    public ConfigProvider(string filePath)
    {
        _filePath = filePath;
        Load();
    }
    
    public void Set(string key, string value)
    {
        _items[key] = value;
        Save();
    }

    public string? Get(string key)
    {
        _items.TryGetValue(key, out var value);
        return value;
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(_items, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _items = new Dictionary<string, string>();
        }
        
        try
        {
            var json = File.ReadAllText(_filePath);
            var items = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            _items = items ?? new Dictionary<string, string>();
        }
        catch
        {
            _items = new Dictionary<string, string>();
        }
    }
}