using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EmailDbContext))]
[Migration("20260827000002_RemoveReconfirmCadenceAddQuietHours")]
public partial class RemoveReconfirmCadenceAddQuietHours { }
