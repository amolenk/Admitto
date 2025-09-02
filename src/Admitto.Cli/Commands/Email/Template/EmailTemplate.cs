namespace Amolenk.Admitto.Cli.Commands.Email.Template;

public record EmailTemplate(string SubjectTemplate, string BodyTemplate)
{
    public static EmailTemplate Load(string templateFolderPath)
    {
        // TODO Check if files exist and throw meaningful exception
        
        var subjectTemplate = File.ReadAllText(System.IO.Path.Combine(templateFolderPath, "subject.txt"));
        var bodyTemplate = File.ReadAllText(System.IO.Path.Combine(templateFolderPath, "body.html"));

        return new EmailTemplate(subjectTemplate, bodyTemplate);
    }
}