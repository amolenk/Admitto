namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

[AttributeUsage(AttributeTargets.Class)]
public class RequiresCapabilityAttribute(HostCapability capability) : Attribute
{
    public HostCapability Capability => capability;
}

[Flags]
public enum HostCapability
{
    None = 0,
    Email = 1,
    Jobs = 2
}