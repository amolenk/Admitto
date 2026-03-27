using Amolenk.Admitto.Module.Shared.Contracts;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public interface IMessagePolicy
{
    bool ShouldPublishModuleEvent(IDomainEvent domainEvent);

    bool ShouldPublishIntegrationEvent(IDomainEvent domainEvent);

    IModuleEvent MapToModuleEvent(IDomainEvent domainEvent);

    IIntegrationEvent MapToIntegrationEvent(IDomainEvent domainEvent);
}

public abstract class MessagePolicy : IMessagePolicy
{
    private readonly Dictionary<Type, MessagePolicyRule> _rules = new();

    public bool ShouldPublishModuleEvent(IDomainEvent domainEvent)
    {
        return _rules.TryGetValue(domainEvent.GetType(), out var rule) && rule.ModuleEventMapper is not null;
    }

    public bool ShouldPublishIntegrationEvent(IDomainEvent domainEvent)
    {
        return _rules.TryGetValue(domainEvent.GetType(), out var rule) && rule.IntegrationEventMapper is not null;
    }

    public IModuleEvent MapToModuleEvent(IDomainEvent domainEvent)
    {
        if (!_rules.TryGetValue(domainEvent.GetType(), out var rule) || rule.ModuleEventMapper is null)
        {
            throw new InvalidOperationException(
                $"No module event mapping function found for domain event of type {domainEvent.GetType().FullName}.");
        }

        return rule.ModuleEventMapper(domainEvent);
    }

    public IIntegrationEvent MapToIntegrationEvent(IDomainEvent domainEvent)
    {
        if (!_rules.TryGetValue(domainEvent.GetType(), out var rule) || rule.IntegrationEventMapper is null)
        {
            throw new InvalidOperationException(
                $"No integration event mapping function found for domain event of type {domainEvent.GetType().FullName}.");
        }

        return rule.IntegrationEventMapper(domainEvent);
    }
    
    protected MessagePolicyRuleBuilder<TDomainEvent> Configure<TDomainEvent>()
    {
        var domainEventType = typeof(TDomainEvent);
        if (_rules.ContainsKey(domainEventType))
            throw new InvalidOperationException($"Rule already configured for {domainEventType.Name}.");

        var rule = new MessagePolicyRule();
        _rules.Add(domainEventType, rule);
        
        return new MessagePolicyRuleBuilder<TDomainEvent>(rule);
    }
}

public sealed class MessagePolicyRuleBuilder<TDomainEvent>(MessagePolicyRule rule)
{
    public MessagePolicyRuleBuilder<TDomainEvent> PublishModuleEvent(Func<TDomainEvent, IModuleEvent> mapper)
    {
        rule.ModuleEventMapper = domainEvent => mapper((TDomainEvent)domainEvent);
        return this;
    }
    
    public MessagePolicyRuleBuilder<TDomainEvent> PublishIntegrationEvent(Func<TDomainEvent, IIntegrationEvent> mapper)
    {
        rule.IntegrationEventMapper = domainEvent => mapper((TDomainEvent)domainEvent);
        return this;
    }
}

public sealed class MessagePolicyRule
{
    public Func<IDomainEvent, IModuleEvent>? ModuleEventMapper { get; set; } 

    public Func<IDomainEvent, IIntegrationEvent>? IntegrationEventMapper { get; set; } 
}
