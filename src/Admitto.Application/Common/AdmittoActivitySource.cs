using System.Diagnostics;

namespace Amolenk.Admitto.Application.Common;

public static class AdmittoActivitySource
{
    public const string Name = "Admitto";
    
    public static readonly ActivitySource ActivitySource = new(Name);
}

