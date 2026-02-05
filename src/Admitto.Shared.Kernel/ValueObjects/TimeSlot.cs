namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public record TimeSlot : Slug
{
    private TimeSlot(string value) : base(value) { }
    
    public static TimeSlot From(string input) => new(input);
}