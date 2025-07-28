namespace Amolenk.Admitto.Cli.Commands;

public class PagingSettings : CommandSettings
{
    [CommandOption("--page-size")]
    public int PageSize { get; set; }

    [CommandOption("--page")]
    public int Page { get; set; }
}

