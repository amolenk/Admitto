using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RegistrationsDbContext))]
[Migration("20260909152810_AddRegistrationCancelledAt")]
public partial class AddRegistrationCancelledAt { }
