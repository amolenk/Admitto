namespace Amolenk.Admitto.Cli.Commands.Email;

[AttributeUsage(AttributeTargets.All)]
public class EmailTypeDescriptionAttribute : DescriptionAttribute
{
    public override string Description => "The email type. Valid values are: " + string.Join(", ", Enum.GetNames(typeof(EmailType)));
}