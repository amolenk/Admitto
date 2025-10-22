using System.Text.Json.Serialization;

namespace Amolenk.Admitto.Cli.Common.Auth;

public interface ITokenCache
{
    void Save(CachedToken token);
    CachedToken? Load();
    void Clear();
}

public class CachedToken
{
    public string AccessToken { get; set; } = "";
    public string? RefreshToken { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}

public class TokenCache : ITokenCache
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private readonly string _filePath;

    public TokenCache(string filePath)
    {
        _filePath = filePath;
    }

    public void Save(CachedToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(token, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public CachedToken? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<CachedToken>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Clear()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }
}