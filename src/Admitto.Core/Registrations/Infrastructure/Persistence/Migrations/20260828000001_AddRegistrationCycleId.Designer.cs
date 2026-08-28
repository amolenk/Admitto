using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RegistrationsDbContext))]
[Migration("20260828000001_AddRegistrationCycleId")]
public partial class AddRegistrationCycleId { }
