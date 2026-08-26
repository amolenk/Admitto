using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Npgsql;
using Shouldly;

namespace Amolenk.Admitto.Core.DomainTests.Registrations.Infrastructure.Persistence;

[TestClass]
public sealed class RegistrationsPostgresExceptionMappingTests
{
    // Given a Postgres unique-constraint violation on the registrations event/email index
    // When the exception is mapped
    // Then it maps to an AlreadyExists error for Registration
    [TestMethod]
    public void TryMapToError_DuplicateRegistrationConstraint_MapsToAlreadyExistsError()
    {
        var sut = new RegistrationsPostgresExceptionMapping();
        var ex = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR", "ERROR", "23505",
            constraintName: "IX_registrations_event_id_email");

        var result = sut.TryMapToError(ex, out var error);

        result.ShouldBeTrue();
        error.ShouldMatch(AlreadyExistsError.Create<Registration>());
    }

    // Given a Postgres unique-constraint violation on the ticketed events public slug index
    // When the exception is mapped
    // Then it maps to an AlreadyExists error for TicketedEvent
    [TestMethod]
    public void TryMapToError_DuplicateEventSlugConstraint_MapsToAlreadyExistsError()
    {
        var sut = new RegistrationsPostgresExceptionMapping();
        var ex = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR", "ERROR", "23505",
            constraintName: "IX_ticketed_events_public_slug");

        var result = sut.TryMapToError(ex, out var error);

        result.ShouldBeTrue();
        error.ShouldMatch(AlreadyExistsError.Create<TicketedEvent>());
    }

    // Given a Postgres unique-constraint violation on a constraint the mapping does not recognize
    // When the exception is mapped
    // Then it returns false and no error is produced
    [TestMethod]
    public void TryMapToError_UnknownConstraint_ReturnsFalse()
    {
        var sut = new RegistrationsPostgresExceptionMapping();
        var ex = new PostgresException(
            "some other constraint violation",
            "ERROR", "ERROR", "23505",
            constraintName: "IX_some_other_constraint");

        var result = sut.TryMapToError(ex, out var error);

        result.ShouldBeFalse();
        error.ShouldBeNull();
    }
}
