namespace Amolenk.Admitto.Module.Email.Application.Templating;

/// <summary>
/// Fixed sample values used when rendering a template for preview or test-send purposes.
/// Property names are converted to snake_case by Scriban's StandardMemberRenamer.
/// </summary>
internal static class EmailTemplateSampleParameters
{
    public static object Create() => new
    {
        EventName     = "DevConf 2026",
        FirstName     = "Alice",
        RegisterLink  = "https://example.com/register",
        QrcodeLink    = "https://example.com/qrcode",
        TicketTypes   = new[] { "Conference Pass" },
        CancelLink    = "https://example.com/cancel",
        EventWebsite  = "https://example.com",
        PlainCode     = "123456",
    };
}
