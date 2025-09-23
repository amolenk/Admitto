namespace Amolenk.Admitto.Cli.Commands.Email.Template;

public record EmailTemplate(string SubjectTemplate, string TextBodyTemplate, string HtmlBodyTemplate)
{
    public static EmailTemplate Load(string templateFolderPath)
    {
        // TODO Check if files exist and throw meaningful exception
        
        var subjectTemplate = File.ReadAllText(System.IO.Path.Combine(templateFolderPath, "subject.txt"));
        var textBodyTemplate = File.ReadAllText(System.IO.Path.Combine(templateFolderPath, "body.txt"));
        var htmlBodyTemplate = File.ReadAllText(System.IO.Path.Combine(templateFolderPath, "body.html"));

        return new EmailTemplate(subjectTemplate, textBodyTemplate, htmlBodyTemplate);
    }
}