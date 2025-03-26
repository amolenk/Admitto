namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IEmailOutbox
{
    void EnqueueEmail(string recipientEmail, string templateId, Dictionary<string, string> templateParameters, 
        bool priority = false);
}