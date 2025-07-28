namespace Amolenk.Admitto.Cli.Commands.Team.Member;

[AttributeUsage(AttributeTargets.All)]
public class TeamMemberRoleDescriptionAttribute : DescriptionAttribute
{
    public override string Description => "The team member role. Valid values are: " + string.Join(", ", Enum.GetNames(typeof(TeamMemberRole)));
}