using Amolenk.Admitto.Shared.Contracts;
using Amolenk.Admitto.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Shared.Application.Messaging;

public interface IMessagePolicy
{
    bool ShouldPublishAsyncDomainEvent(IDomainEvent domainEvent);

    bool ShouldPublishIntegrationEvent(IDomainEvent domainEvent);

    IIntegrationEvent MapToIntegrationEvent(IDomainEvent domainEvent);
}

public abstract class MessagePolicy : IMessagePolicy
{
    private readonly Dictionary<Type, MessagePolicyRule> _rules = new();

    public bool ShouldPublishAsyncDomainEvent(IDomainEvent domainEvent)
    {
        return _rules.TryGetValue(domainEvent.GetType(), out var rule) && rule.PersistAsDomainEvent;
    }

    public bool ShouldPublishIntegrationEvent(IDomainEvent domainEvent)
    {
        return _rules.TryGetValue(domainEvent.GetType(), out var rule) && rule.IntegrationEventMapper is not null;
    }

    public IIntegrationEvent MapToIntegrationEvent(IDomainEvent domainEvent)
    {
        if (!_rules.TryGetValue(domainEvent.GetType(), out var rule) || rule.IntegrationEventMapper is null)
        {
            throw new InvalidOperationException(
                $"No mapping function found for domain event of type {domainEvent.GetType().FullName}.");
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
    public MessagePolicyRuleBuilder<TDomainEvent> AsyncDomainEvent()
    {
        rule.PersistAsDomainEvent = true;
        return this;
    }
    
    public MessagePolicyRuleBuilder<TDomainEvent> IntegrationEvent(Func<TDomainEvent, IIntegrationEvent> mapper)
    {
        rule.IntegrationEventMapper = domainEvent => mapper((TDomainEvent)domainEvent);
        return this;
    }
}

public sealed class MessagePolicyRule
{
    public bool PersistAsDomainEvent { get; set; }

    public Func<IDomainEvent, IIntegrationEvent>? IntegrationEventMapper { get; set; } 
}
