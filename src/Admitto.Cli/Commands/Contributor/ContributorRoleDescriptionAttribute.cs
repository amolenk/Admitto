namespace Amolenk.Admitto.Cli.Commands.Contributor;

[AttributeUsage(AttributeTargets.All)]
public class ContributorRoleDescriptionAttribute : DescriptionAttribute
{
    public override string Description => "The contributor role. Valid values are: " +
                                          string.Join(", ", Enum.GetNames<ContributorRole>());
}