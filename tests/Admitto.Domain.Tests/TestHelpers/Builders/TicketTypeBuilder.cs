using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Domain.Tests.TestHelpers.Builders;

public class TicketTypeBuilder
{
    private string _slug = "general-admission";
    private string _name = "General Admission";
    private string _slotName = "default";
    private int _maxCapacity = 100;

    public TicketTypeBuilder WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }

    public TicketTypeBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TicketTypeBuilder WithSlotName(string slotName)
    {
        _slotName = slotName;
        return this;
    }
    
    public TicketTypeBuilder WithMaxCapacity(int maxCapacity)
    {
        _maxCapacity = maxCapacity;
        return this;
    }

    public TicketType Build() => TicketType.Create(_slug, _name, _slotName, _maxCapacity);
}