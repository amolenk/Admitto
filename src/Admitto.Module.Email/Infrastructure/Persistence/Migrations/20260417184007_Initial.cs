using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "email");

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_email_settings",
                schema: "email",
                columns: table => new
                {
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    smtp_port = table.Column<int>(type: "integer", nullable: false),
                    from_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    auth_mode = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    protected_password = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_email_settings", x => x.ticketed_event_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_protection_keys",
                schema: "email");

            migrationBuilder.DropTable(
                name: "event_email_settings",
                schema: "email");
        }
    }
}
