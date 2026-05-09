// using System;
// using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
// using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

// namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

// public readonly record struct IntegrationEventId : IGuidValueObject
// {
//     public Guid Value { get; }
    
//     private IntegrationEventId(Guid value) => Value = value;

//     public static IntegrationEventId New() => new(Guid.NewGuid());

//     public static ValidationResult<IntegrationEventId> TryFrom(Guid value)
//         => GuidValueObject.TryFrom(value, v => new IntegrationEventId(v));

//     public static IntegrationEventId From(Guid value)
//         => GuidValueObject.TryFrom(value, v => new IntegrationEventId(v)).GetValueOrThrow();

//     public override string ToString() => Value.ToString();
// }