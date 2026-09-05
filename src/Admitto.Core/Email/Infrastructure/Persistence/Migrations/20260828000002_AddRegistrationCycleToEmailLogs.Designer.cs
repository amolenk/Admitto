using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EmailDbContext))]
[Migration("20260828000002_AddRegistrationCycleToEmailLogs")]
public partial class AddRegistrationCycleToEmailLogs { }
