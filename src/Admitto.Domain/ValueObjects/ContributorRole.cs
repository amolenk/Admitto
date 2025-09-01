namespace Amolenk.Admitto.Domain.ValueObjects;

public record ContributorRole(string Name)
{
    private static readonly string[] ValidRoles = [ "crew", "speaker", "sponsor" ];
    
    public override string ToString() => Name;

    public static implicit operator string(ContributorRole role) => role.Name;
    public static implicit operator ContributorRole(string name) => new(name);
    
    public static ContributorRole Parse(string name)
    {
        var normalizedName = name.ToLowerInvariant();
        
        if (!ValidRoles.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainRuleException(DomainRuleError.Contributor.UnsupportedRole(name));
        }
        
        return new ContributorRole(normalizedName);
    }
}
