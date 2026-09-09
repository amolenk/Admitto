using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EmailDbContext))]
[Migration("20260827000003_AddActiveReconfirmJobReservation")]
public partial class AddActiveReconfirmJobReservation { }
