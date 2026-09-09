using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RegistrationsDbContext))]
[Migration("20260827000001_RemoveReconfirmCadenceAddQuietHours")]
public partial class RemoveReconfirmCadenceAddQuietHours { }
