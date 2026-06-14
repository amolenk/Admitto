using Amolenk.Admitto.Core.Badges.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Migrations run without an HTTP context, so provide a no-op IUserContextAccessor
// to satisfy the AuditInterceptor dependency.
builder.Services.AddSingleton<IUserContextAccessor>(
    new StaticUserContextAccessor(new UserContextDto(Guid.Empty, "migrations", "migrations@system.local")));

builder.AddOrganizationModule();
builder.AddEmailModule();
builder.AddRegistrationsModule();
builder.AddBadgesModule();

var app = builder.Build();

using var migrationScope = app.Services.CreateScope();

await MigrateDatabasesAsync<OrganizationDbContext>(migrationScope);
await MigrateDatabasesAsync<EmailDbContext>(migrationScope);
await MigrateDatabasesAsync<RegistrationsDbContext>(migrationScope);
await MigrateDatabasesAsync<BadgesDbContext>(migrationScope);
await MigrateQuartzAsync(builder.Configuration);
await MigrateBetterAuthAsync(builder.Configuration);
return;

async ValueTask MigrateDatabasesAsync<TDbContext>(IServiceScope scope) where TDbContext : DbContext
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
    await dbContext.Database.MigrateAsync();
}

async ValueTask MigrateBetterAuthAsync(IConfiguration configuration)
{
    const string BetterAuthSchemaSql = """
                                       CREATE TABLE IF NOT EXISTS "user" (
                                           id              TEXT PRIMARY KEY,
                                           name            TEXT NOT NULL,
                                           email           TEXT NOT NULL UNIQUE,
                                           "emailVerified" BOOLEAN NOT NULL DEFAULT FALSE,
                                           image           TEXT,
                                           "createdAt"     TIMESTAMP NOT NULL DEFAULT NOW(),
                                           "updatedAt"     TIMESTAMP NOT NULL DEFAULT NOW()
                                       );

                                       CREATE TABLE IF NOT EXISTS session (
                                           id           TEXT PRIMARY KEY,
                                           "expiresAt"  TIMESTAMP NOT NULL,
                                           token        TEXT NOT NULL UNIQUE,
                                           "createdAt"  TIMESTAMP NOT NULL DEFAULT NOW(),
                                           "updatedAt"  TIMESTAMP NOT NULL DEFAULT NOW(),
                                           "ipAddress"  TEXT,
                                           "userAgent"  TEXT,
                                           "userId"     TEXT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE
                                       );

                                       CREATE TABLE IF NOT EXISTS account (
                                           id                      TEXT PRIMARY KEY,
                                           "accountId"             TEXT NOT NULL,
                                           "providerId"            TEXT NOT NULL,
                                           "userId"                TEXT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
                                           "accessToken"           TEXT,
                                           "refreshToken"          TEXT,
                                           "idToken"               TEXT,
                                           "accessTokenExpiresAt"  TIMESTAMP,
                                           "refreshTokenExpiresAt" TIMESTAMP,
                                           scope                   TEXT,
                                           password                TEXT,
                                           "createdAt"             TIMESTAMP NOT NULL DEFAULT NOW(),
                                           "updatedAt"             TIMESTAMP NOT NULL DEFAULT NOW()
                                       );

                                       CREATE TABLE IF NOT EXISTS verification (
                                           id          TEXT PRIMARY KEY,
                                           identifier  TEXT NOT NULL,
                                           value       TEXT NOT NULL,
                                           "expiresAt" TIMESTAMP NOT NULL,
                                           "createdAt" TIMESTAMP DEFAULT NOW(),
                                           "updatedAt" TIMESTAMP DEFAULT NOW()
                                       );
                                       """;

    var connectionString = configuration.GetConnectionString("better-auth-db");
    if (string.IsNullOrWhiteSpace(connectionString))
        return;

    await using var dataSource = NpgsqlDataSource.Create(connectionString);
    await using var cmd = dataSource.CreateCommand(BetterAuthSchemaSql);
    await cmd.ExecuteNonQueryAsync();
}

async ValueTask MigrateQuartzAsync(IConfiguration configuration)
{
    const string QuartzSchemaSql = """
                                   CREATE TABLE IF NOT EXISTS qrtz_job_details
                                     (
                                       sched_name TEXT NOT NULL,
                                       job_name TEXT NOT NULL,
                                       job_group TEXT NOT NULL,
                                       description TEXT NULL,
                                       job_class_name TEXT NOT NULL,
                                       is_durable BOOL NOT NULL,
                                       is_nonconcurrent BOOL NOT NULL,
                                       is_update_data BOOL NOT NULL,
                                       requests_recovery BOOL NOT NULL,
                                       job_data BYTEA NULL,
                                       PRIMARY KEY (sched_name, job_name, job_group)
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_triggers
                                     (
                                       sched_name TEXT NOT NULL,
                                       trigger_name TEXT NOT NULL,
                                       trigger_group TEXT NOT NULL,
                                       job_name TEXT NOT NULL,
                                       job_group TEXT NOT NULL,
                                       description TEXT NULL,
                                       next_fire_time BIGINT NULL,
                                       prev_fire_time BIGINT NULL,
                                       priority INTEGER NULL,
                                       trigger_state TEXT NOT NULL,
                                       trigger_type TEXT NOT NULL,
                                       start_time BIGINT NOT NULL,
                                       end_time BIGINT NULL,
                                       calendar_name TEXT NULL,
                                       misfire_instr SMALLINT NULL,
                                       misfire_orig_fire_time BIGINT NULL,
                                       execution_group VARCHAR(200) NULL,
                                       job_data BYTEA NULL,
                                       PRIMARY KEY (sched_name, trigger_name, trigger_group),
                                       FOREIGN KEY (sched_name, job_name, job_group)
                                         REFERENCES qrtz_job_details (sched_name, job_name, job_group)
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_simple_triggers
                                     (
                                       sched_name TEXT NOT NULL,
                                       trigger_name TEXT NOT NULL,
                                       trigger_group TEXT NOT NULL,
                                       repeat_count BIGINT NOT NULL,
                                       repeat_interval BIGINT NOT NULL,
                                       times_triggered BIGINT NOT NULL,
                                       PRIMARY KEY (sched_name, trigger_name, trigger_group),
                                       FOREIGN KEY (sched_name, trigger_name, trigger_group)
                                         REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                                         ON DELETE CASCADE
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_simprop_triggers
                                     (
                                       sched_name TEXT NOT NULL,
                                       trigger_name TEXT NOT NULL,
                                       trigger_group TEXT NOT NULL,
                                       str_prop_1 TEXT NULL,
                                       str_prop_2 TEXT NULL,
                                       str_prop_3 TEXT NULL,
                                       int_prop_1 INTEGER NULL,
                                       int_prop_2 INTEGER NULL,
                                       long_prop_1 BIGINT NULL,
                                       long_prop_2 BIGINT NULL,
                                       dec_prop_1 NUMERIC NULL,
                                       dec_prop_2 NUMERIC NULL,
                                       bool_prop_1 BOOL NULL,
                                       bool_prop_2 BOOL NULL,
                                       time_zone_id TEXT NULL,
                                       PRIMARY KEY (sched_name, trigger_name, trigger_group),
                                       FOREIGN KEY (sched_name, trigger_name, trigger_group)
                                         REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                                         ON DELETE CASCADE
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_cron_triggers
                                     (
                                       sched_name TEXT NOT NULL,
                                       trigger_name TEXT NOT NULL,
                                       trigger_group TEXT NOT NULL,
                                       cron_expression TEXT NOT NULL,
                                       time_zone_id TEXT,
                                       PRIMARY KEY (sched_name, trigger_name, trigger_group),
                                       FOREIGN KEY (sched_name, trigger_name, trigger_group)
                                         REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                                         ON DELETE CASCADE
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_blob_triggers
                                     (
                                       sched_name TEXT NOT NULL,
                                       trigger_name TEXT NOT NULL,
                                       trigger_group TEXT NOT NULL,
                                       blob_data BYTEA NULL,
                                       PRIMARY KEY (sched_name, trigger_name, trigger_group),
                                       FOREIGN KEY (sched_name, trigger_name, trigger_group)
                                         REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                                         ON DELETE CASCADE
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_calendars
                                     (
                                       sched_name TEXT NOT NULL,
                                       calendar_name TEXT NOT NULL,
                                       calendar BYTEA NOT NULL,
                                       PRIMARY KEY (sched_name, calendar_name)
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_paused_trigger_grps
                                     (
                                       sched_name TEXT NOT NULL,
                                       trigger_group TEXT NOT NULL,
                                       PRIMARY KEY (sched_name, trigger_group)
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_fired_triggers
                                     (
                                       sched_name TEXT NOT NULL,
                                       entry_id TEXT NOT NULL,
                                       trigger_name TEXT NOT NULL,
                                       trigger_group TEXT NOT NULL,
                                       instance_name TEXT NOT NULL,
                                       fired_time BIGINT NOT NULL,
                                       sched_time BIGINT NOT NULL,
                                       priority INTEGER NOT NULL,
                                       state TEXT NOT NULL,
                                       job_name TEXT NULL,
                                       job_group TEXT NULL,
                                       is_nonconcurrent BOOL NOT NULL,
                                       requests_recovery BOOL NULL,
                                       execution_group VARCHAR(200) NULL,
                                       PRIMARY KEY (sched_name, entry_id)
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_scheduler_state
                                     (
                                       sched_name TEXT NOT NULL,
                                       instance_name TEXT NOT NULL,
                                       last_checkin_time BIGINT NOT NULL,
                                       checkin_interval BIGINT NOT NULL,
                                       PRIMARY KEY (sched_name, instance_name)
                                   );

                                   CREATE TABLE IF NOT EXISTS qrtz_locks
                                     (
                                       sched_name TEXT NOT NULL,
                                       lock_name TEXT NOT NULL,
                                       PRIMARY KEY (sched_name, lock_name)
                                   );

                                   CREATE INDEX IF NOT EXISTS idx_qrtz_j_req_recovery ON qrtz_job_details (requests_recovery);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_t_next_fire_time ON qrtz_triggers (next_fire_time);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_t_state ON qrtz_triggers (trigger_state);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_t_nft_st ON qrtz_triggers (next_fire_time, trigger_state);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_ft_trig_name ON qrtz_fired_triggers (trigger_name);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_ft_trig_group ON qrtz_fired_triggers (trigger_group);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_ft_trig_nm_gp ON qrtz_fired_triggers (sched_name, trigger_name, trigger_group);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_ft_trig_inst_name ON qrtz_fired_triggers (instance_name);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_ft_job_name ON qrtz_fired_triggers (job_name);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_ft_job_group ON qrtz_fired_triggers (job_group);
                                   CREATE INDEX IF NOT EXISTS idx_qrtz_ft_job_req_recovery ON qrtz_fired_triggers (requests_recovery);
                                   """;

    var connectionString = configuration.GetConnectionString("quartz-db");
    if (string.IsNullOrWhiteSpace(connectionString))
        return;

    await using var dataSource = NpgsqlDataSource.Create(connectionString);
    await using var cmd = dataSource.CreateCommand(QuartzSchemaSql);
    await cmd.ExecuteNonQueryAsync();
}
