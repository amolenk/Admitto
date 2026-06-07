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

    [TestMethod]
    public void TryMapToError_DuplicateEventSlugConstraint_MapsToAlreadyExistsError()
    {
        var sut = new RegistrationsPostgresExceptionMapping();
        var ex = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR", "ERROR", "23505",
            constraintName: "IX_ticketed_events_team_id_slug");

        var result = sut.TryMapToError(ex, out var error);

        result.ShouldBeTrue();
        error.ShouldMatch(AlreadyExistsError.Create<TicketedEvent>());
    }

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
