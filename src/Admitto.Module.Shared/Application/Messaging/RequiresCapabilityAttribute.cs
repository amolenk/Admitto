namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

[AttributeUsage(AttributeTargets.Class)]
public class RequiresCapabilityAttribute(HostCapability capability) : Attribute
{
    public HostCapability Capability => capability;
}

[Flags]
public enum HostCapability
{
    None = 0,
    Email = 1
}