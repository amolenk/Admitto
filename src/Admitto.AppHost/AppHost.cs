using Amolenk.Admitto.AppHost.Extensions;
using Amolenk.Admitto.Core.Badges.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Azure.Provisioning.AppContainers;
using Aspire.Hosting.Azure;
using Aspire.Hosting.JavaScript;
using Azure.Provisioning.ApplicationInsights;
using Azure.Provisioning.PostgreSql;
using Microsoft.Extensions.Hosting;

// Disable warnings for experimental features.
#pragma warning disable ASPIREJAVASCRIPT001
#pragma warning disable ASPIREACADOMAINS001

var builder = DistributedApplication.CreateBuilder(args);

// Whether to only include the infrastructure components (e.g., databases, messaging) and exclude apps
// (API, worker, UI) in the builder.
var infraOnly = builder.ExecutionContext.IsRunMode && builder.Environment.IsIntegrationTesting();

var uiPublicUrl = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("uiPublicUrl")
    : builder.AddParameter("uiPublicUrl", "http://localhost:3000");

var uiAuthClientSecret = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("uiAuthClientSecret", secret: true)
    : builder.AddParameter("uiAuthClientSecret", value: "admitto-ui-dev-secret", secret: true);

var azureMonitorSamplingRatio = builder.AddParameter("azureMonitorSamplingRatio", value: string.Empty);

///////////////////////////////////////////////////////////////////////////////
// Azure Postgres Flexible Server
///////////////////////////////////////////////////////////////////////////////

// Use a consistent password to prevent authentication failures when the container is recreated while the data volume
// persists.
var postgresUser = builder.AddParameter("postgresUser", value: "admitto");
var postgresPassword = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("postgresPassword", secret: true)
    : builder.AddParameter("postgresPassword", value: "admin", secret: true);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .WithPasswordAuthentication(postgresUser, postgresPassword);

if (builder.ExecutionContext.IsRunMode)
{
    postgres.RunAsContainer(pg =>
    {
        if (builder.Environment.IsDevelopment())
        {
            pg
                .WithDataVolume("admitto-postgres")
                .WithLifetime(ContainerLifetime.Persistent);
        }

        if (builder.Environment.IsDevelopment())
        {
            pg
                .WithHostPort(15003)
                .WithPgAdmin(pgAdmin =>
                {
                    pgAdmin
                        .WithHostPort(15004)
                        .WithLifetime(ContainerLifetime.Persistent);
                });
        }
    });
}

if (builder.ExecutionContext.IsPublishMode)
{
    postgres.ConfigureInfrastructure(infra =>
    {
        var server = infra.GetProvisionableResources()
            .OfType<PostgreSqlFlexibleServer>()
            .Single();

        server.Sku = new PostgreSqlFlexibleServerSku
        {
            Name = "Standard_D2ds_v5",
            Tier = PostgreSqlFlexibleServerSkuTier.GeneralPurpose
        };

        server.Storage = new PostgreSqlFlexibleServerStorage
        {
            StorageSizeInGB = 64,
            AutoGrow = StorageAutoGrow.Enabled,
            Tier = PostgreSqlManagedDiskPerformanceTier.P6
        };

        server.Backup = new PostgreSqlFlexibleServerBackupProperties
        {
            BackupRetentionDays = 30,
            GeoRedundantBackup = PostgreSqlFlexibleServerGeoRedundantBackupEnum.Disabled
        };

        server.HighAvailability = new PostgreSqlFlexibleServerHighAvailability
        {
            Mode = PostgreSqlFlexibleServerHighAvailabilityMode.Disabled
        };

        // Allow PGCRYPTO extension so we can generate a signing key in the database for use with protected data like
        // SMTP passwords.
        infra.Add(
            new PostgreSqlFlexibleServerConfiguration("postgresAzureExtensions")
            {
                Parent = server,
                Name = "azure.extensions",
                Source = "user-override",
                Value = "PGCRYPTO"
            });
    });
}

var admittoDb = postgres.AddDatabase("admitto-db");
var quartzDb = postgres.AddDatabase("quartz-db");
var betterAuthDb = postgres.AddDatabase("better-auth-db");
var keycloakDb = postgres.AddDatabase("keycloak-db");

///////////////////////////////////////////////////////////////////////////////
// Azure Service Bus
///////////////////////////////////////////////////////////////////////////////

var serviceBus = builder.AddAzureServiceBus("messaging");

if (builder.ExecutionContext.IsRunMode &&
    (builder.Environment.IsDevelopment() || builder.Environment.IsEndToEndTesting()))
{
    serviceBus.RunAsEmulator(configure =>
    {
        if (builder.Environment.IsDevelopment())
        {
            configure
                .WithLifetime(ContainerLifetime.Persistent);
        }
    });
}

serviceBus.AddServiceBusQueue("queue");

///////////////////////////////////////////////////////////////////////////////
// MailDev
///////////////////////////////////////////////////////////////////////////////

IResourceBuilder<ContainerResource>? mailDev = null;

if (builder.ExecutionContext.IsRunMode)
{
    mailDev = builder.AddContainer("maildev", "maildev/maildev:latest")
        .WithHttpEndpoint(15002, targetPort: 1080)
        .WithEndpoint(name: "smtp", scheme: "smtp", targetPort: 1025, isExternal: true, port: 1025);

    if (builder.Environment.IsDevelopment())
    {
        mailDev.WithLifetime(ContainerLifetime.Persistent);
    }
}

///////////////////////////////////////////////////////////////////////////////
// SMTP
///////////////////////////////////////////////////////////////////////////////

IResourceBuilder<ParameterResource>? smtpHost = null;
IResourceBuilder<ParameterResource>? smtpPort = null;

if (builder.ExecutionContext.IsPublishMode)
{
    smtpHost = builder.AddParameter("smtpHost");
    smtpPort = builder.AddParameter("smtpPort");
}

var smtpFromAddress = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("smtpFromAddress")
    : builder.AddParameter("smtpFromAddress", value: "noreply@tickets.admitto.local");

var smtpFromDisplayName = builder.AddParameter("smtpFromDisplayName", value: "Admitto");

var smtpAuth = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("smtpAuth", value: "true")
    : builder.AddParameter("smtpAuth", value: "false");

var smtpUsername = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("smtpUsername")
    : builder.AddParameter("smtpUsername", value: string.Empty);

var smtpPassword = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("smtpPassword", secret: true)
    : builder.AddParameter("smtpPassword", value: string.Empty, secret: true);

// TODO Mailgun doesn't seem to work with only TLS (port 465)
var smtpSsl = builder.AddParameter("smtpSsl", value: "true");

var smtpStartTls = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("smtpStartTls", value: "true")
    : builder.AddParameter("smtpStartTls", value: "false");

///////////////////////////////////////////////////////////////////////////////
// Keycloak
///////////////////////////////////////////////////////////////////////////////

var keycloakAdminUser = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("keycloakAdminUser")
    : builder.AddParameter("keycloakAdminUser", value: "admin");

var keycloakAdminPassword = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("keycloakAdminPassword", secret: true)
    : builder.AddParameter("keycloakAdminPassword", value: "admin", secret: true);

var keycloak = builder.AddContainer("keycloak", "admitto-keycloak")
    .WithDockerfile("./KeycloakConfiguration")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithEnvironment("KEYCLOAK_SMTP_FROM", smtpFromAddress)
    .WithEnvironment("KEYCLOAK_SMTP_FROM_DISPLAY_NAME", smtpFromDisplayName)
    .WithEnvironment("KEYCLOAK_SMTP_AUTH", smtpAuth)
    .WithEnvironment("KEYCLOAK_SMTP_USERNAME", smtpUsername)
    .WithEnvironment("KEYCLOAK_SMTP_PASSWORD", smtpPassword)
    .WithEnvironment("KEYCLOAK_SMTP_SSL", smtpSsl)
    .WithEnvironment("KEYCLOAK_SMTP_STARTTLS", smtpStartTls);

if (builder.ExecutionContext.IsPublishMode)
{
    var keycloakPublicUrl = builder.AddParameter("keycloakPublicUrl");

    keycloak
        .WithArgs("start", "--import-realm", "--spi-user-profile--declarative-user-profile--read-only-attributes=email")
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithExternalHttpEndpoints()
        .WithEnvironment("KC_DB", "postgres")
        .WithEnvironment(
            "KC_DB_URL",
            ReferenceExpression.Create(
                $"jdbc:postgresql://{postgres.Resource.HostName}/keycloak-db?sslmode=require"))
        .WithEnvironment("KC_DB_USERNAME", postgres.Resource.UserName!)
        .WithEnvironment("KC_DB_PASSWORD", postgres.Resource.Password!)
        .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
        .WithEnvironment("KC_HOSTNAME", keycloakPublicUrl)
        .WithEnvironment("KEYCLOAK_SMTP_HOST", smtpHost!)
        .WithEnvironment("KEYCLOAK_SMTP_PORT", smtpPort!)
        .WaitFor(keycloakDb);


    var keycloakCustomDomain = builder.AddParameter("keycloakCustomDomain");
    var keycloakCertificateName = builder.AddParameter("keycloakCertificateName");

    keycloak.PublishAsAzureContainerApp((_, app) =>
    {
        app.Template.Scale = new ContainerAppScale { MinReplicas = 1, MaxReplicas = 1 };
        app.ConfigureCustomDomain(keycloakCustomDomain, keycloakCertificateName);
    });
}
else
{
    // For local development use a stable port for the Keycloak resource.
    // It can be any port, but it should be stable to avoid issues with browser cookies that will persist OIDC
    // tokens (which include the authority URL, with port) beyond the lifetime of the app host.
    var keycloakHttpPort = builder.Environment.IsDevelopment() ? 15001 : (int?)null;

    keycloak
        .WithArgs(
            "start-dev",
            "--import-realm",
            "--spi-user-profile--declarative-user-profile--read-only-attributes=email")
        .WithHttpEndpoint(port: keycloakHttpPort, targetPort: 8080, name: "http", isProxied: false)
        .WithEnvironment(
            "KEYCLOAK_SMTP_HOST",
            ReferenceExpression.Create($"{mailDev!.GetEndpoint("smtp").Property(EndpointProperty.Host)}"))
        .WithEnvironment(
            "KEYCLOAK_SMTP_PORT",
            ReferenceExpression.Create($"{mailDev!.GetEndpoint("smtp").Property(EndpointProperty.Port)}"))
        .WithBindMount(
            Path.Combine(builder.Environment.ContentRootPath, "KeycloakConfiguration", "AdmittoRealm.Local.json"),
            "/opt/keycloak/data/import/admitto-realm.json",
            isReadOnly: true)
        .WithBindMount(
            Path.Combine(builder.Environment.ContentRootPath, "KeycloakConfiguration", "themes", "admitto"),
            "/opt/keycloak/themes/admitto",
            isReadOnly: true)
        .WithEnvironment("KC_SPI_THEME_CACHE_THEMES", "false")
        .WithEnvironment("KC_SPI_THEME_CACHE_TEMPLATES", "false")
        .WithEnvironment("KC_SPI_THEME_STATIC_MAX_AGE", "-1");


    if (builder.Environment.IsDevelopment())
    {
        keycloak
            .WithLifetime(ContainerLifetime.Persistent)
            .WithVolume("admitto-keycloak", "/opt/keycloak/data");
    }

    if (builder.Environment.IsEndToEndTesting())
    {
        keycloak
            .WithEnvironment("ADMITTO_UI_PUBLIC_URL", "http://localhost:3000")
            .WithEnvironment("ADMITTO_UI_CLIENT_SECRET", "admitto-ui-dev-secret");
    }
}

// Can only set healthcheck after having specified the endpoint.
keycloak.WithHttpHealthCheck("/realms/admitto/.well-known/openid-configuration");

if (mailDev is not null)
{
    keycloak.WaitFor(mailDev);
}

var keycloakAuthorityRef = ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/admitto");

///////////////////////////////////////////////////////////////////////////////
// Azure Log Analytics Workspace
///////////////////////////////////////////////////////////////////////////////

IResourceBuilder<AzureLogAnalyticsWorkspaceResource>? logAnalytics = null;

if (builder.ExecutionContext.IsPublishMode)
{
    logAnalytics = builder.AddAzureLogAnalyticsWorkspace("log-analytics");
}

///////////////////////////////////////////////////////////////////////////////
// Azure Container App Environment
///////////////////////////////////////////////////////////////////////////////

if (builder.ExecutionContext.IsPublishMode)
{
    builder.AddAzureContainerAppEnvironment("aca-env")
        .WithAzureLogAnalyticsWorkspace(logAnalytics!);
}

///////////////////////////////////////////////////////////////////////////////
// Azure Application Insights
///////////////////////////////////////////////////////////////////////////////

IResourceBuilder<AzureApplicationInsightsResource>? appInsights = null;

if (builder.ExecutionContext.IsPublishMode)
{
    appInsights = builder.AddAzureApplicationInsights("app-insights", logAnalytics);

    var operatorAlertEmail = builder.AddParameter("operatorAlertEmail");
    var operatorAlertWebhookUrl = builder.AddParameter("operatorAlertWebhookUrl", value: string.Empty, secret: true);

    appInsights.ConfigureInfrastructure(infra =>
    {
        var insights = infra.GetProvisionableResources()
            .OfType<ApplicationInsightsComponent>()
            .Single();

        insights.RetentionInDays = 90;
        insights.IngestionMode = ComponentIngestionMode.LogAnalytics;
        insights.Tags.Add("environment", "production");
    });

    builder.AddBicepTemplate("observability-alerts", "Observability/alerts.bicep")
        .WithParameter("workspaceName", logAnalytics!.Resource.NameOutputReference)
        .WithParameter("appInsightsName", appInsights.Resource.NameOutputReference)
        .WithParameter("operatorAlertEmail", operatorAlertEmail)
        .WithParameter("operatorAlertWebhookUrl", operatorAlertWebhookUrl);
}

///////////////////////////////////////////////////////////////////////////////
// API
///////////////////////////////////////////////////////////////////////////////

IResourceBuilder<ProjectResource>? api = null;
IResourceBuilder<ParameterResource>? externalLinkCustomDomain = null;
ReferenceExpression? apiUrlRef = null;

if (!infraOnly)
{
    var authApiAudience = builder.AddParameter("authApiAudience", value: "admitto-api");

    api = builder.AddProject<Projects.Admitto_Api>("api")
        .WithUrlForEndpoint(
            "http",
            _ => new ResourceUrlAnnotation
            {
                Url = "/scalar",
                DisplayText = "Scalar",
                DisplayLocation = UrlDisplayLocation.SummaryAndDetails
            })
        .WithExternalHttpEndpoints()
        .WithReferenceEnvironment(ReferenceEnvironmentInjectionFlags.ConnectionString)
        .WithEnvironment("AUTHENTICATION__BEARER__AUTHORITY", keycloakAuthorityRef)
        .WithEnvironment("AUTHENTICATION__BEARER__TOKENVALIDATIONPARAMETERS__VALIDAUDIENCE", authApiAudience)
        .WithEnvironment("OBSERVABILITY__AZUREMONITOR__SAMPLINGRATIO", azureMonitorSamplingRatio)
        .WithReference(admittoDb)
        .WithReference(quartzDb)
        .WithReference(serviceBus).WaitFor(serviceBus)
        .WaitFor(keycloak);

    if (builder.ExecutionContext.IsRunMode && builder.Environment.IsEndToEndTesting())
    {
        api
            .WithEnvironment("RATELIMITING__PUBLIC__STRICT__PERMITLIMIT", "1000")
            .WithEnvironment("RATELIMITING__PUBLIC__STANDARD__PERMITLIMIT", "5000");
    }

    if (builder.ExecutionContext.IsPublishMode)
    {
        var apiCustomDomain = builder.AddParameter("apiCustomDomain");
        var apiCertificateName = builder.AddParameter("apiCertificateName");

        externalLinkCustomDomain = builder.AddParameter("externalLinkCustomDomain");
        var externalLinkCertificateName = builder.AddParameter("externalLinkCertificateName");

        api
            .WithReference(appInsights!)
            .PublishAsAzureContainerApp((_, app) =>
            {
                app.Template.Scale = new ContainerAppScale { MinReplicas = 1, MaxReplicas = 1 };
                app.ConfigureCustomDomain(apiCustomDomain, apiCertificateName);
                app.ConfigureCustomDomain(externalLinkCustomDomain, externalLinkCertificateName);
            });
    }

    apiUrlRef = ReferenceExpression.Create($"{api.GetEndpoint("http")}");
}

///////////////////////////////////////////////////////////////////////////////
// Worker
///////////////////////////////////////////////////////////////////////////////

if (!infraOnly)
{
    // Bootstrap admin user
    var apiBootstrapAdmin = builder.ExecutionContext.IsPublishMode
        ? builder.AddParameter("apiBootstrapAdmin")
        : builder.AddParameter(
            "apiBootstrapAdmin",
            value: builder.Environment.IsEndToEndTesting() ? string.Empty : "alice@example.com");

    var worker = builder.AddProject<Projects.Admitto_Worker>("worker")
        .WithEnvironment("ORGANIZATION__BOOTSTRAPADMIN__EMAILADDRESS", apiBootstrapAdmin)
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__AUTHORITY", keycloakAuthorityRef)
        .WithEnvironment(
            "ORGANIZATION__USERDIRECTORIES__KEYCLOAK__TOKENPATH",
            "/realms/master/protocol/openid-connect/token")
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__CLIENTID", "admin-cli")
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__USERNAME", keycloakAdminUser)
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__PASSWORD", keycloakAdminPassword)
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__EXECUTEACTIONSCLIENTID", "admitto-ui")
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__EXECUTEACTIONSREDIRECTURI", uiPublicUrl)
        .WithEnvironment("EMAIL__SYSTEM__FROMADDRESS", smtpFromAddress)
        .WithEnvironment("EMAIL__SYSTEM__AUTHMODE", smtpAuth)
        .WithEnvironment("EMAIL__SYSTEM__USERNAME", smtpUsername)
        .WithEnvironment("EMAIL__SYSTEM__PASSWORD", smtpPassword)
        .WithEnvironment("EMAIL__SYSTEM__SMTPSSL", smtpSsl)
        .WithEnvironment("EMAIL__SYSTEM__SMTPSTARTTLS", smtpStartTls)
        .WithEnvironment("OBSERVABILITY__AZUREMONITOR__SAMPLINGRATIO", azureMonitorSamplingRatio)
        .WithReferenceEnvironment(ReferenceEnvironmentInjectionFlags.ConnectionString)
        .WithReference(admittoDb)
        .WithReference(quartzDb)
        .WithReference(serviceBus)
        .WaitFor(api!);

    if (mailDev is not null)
    {
        worker.WaitFor(mailDev);
    }

    if (builder.ExecutionContext.IsRunMode)
    {
        worker
            // Disable caching to avoid stale data issues in tests
            .WithEnvironment("CACHING__ENABLED", builder.Environment.IsDevelopment().ToString())
            .WithEnvironment("EMAIL__SYSTEM__SMTPHOST",
                ReferenceExpression.Create($"{mailDev!.GetEndpoint("smtp").Property(EndpointProperty.Host)}"))
            .WithEnvironment(
                "EMAIL__SYSTEM__SMTPPORT",
                ReferenceExpression.Create($"{mailDev!.GetEndpoint("smtp").Property(EndpointProperty.Port)}"))
            // Disable the per-message delay so bulk-email fan-out completes quickly
            .WithEnvironment("BULKEMAIL__PERMESSAGEDELAY", "00:00:00")
            .WithEnvironment("REGISTRATIONS__PUBLICEVENTLINKS__BASEURL", ReferenceExpression.Create($"{apiUrlRef!}/e"))
            .WaitFor(mailDev!);
    }

    if (builder.ExecutionContext.IsPublishMode)
    {
        worker
            .WithReference(appInsights!)
            .WithEnvironment("EMAIL__SYSTEM__SMTPHOST", smtpHost!)
            .WithEnvironment("EMAIL__SYSTEM__SMTPPORT", smtpPort!)
            .WithEnvironment(
                "REGISTRATIONS__PUBLICEVENTLINKS__BASEURL",
                ReferenceExpression.Create($"https://{externalLinkCustomDomain!}/e"))
            .PublishAsAzureContainerApp((_, app) =>
            {
                app.Template.Scale = new ContainerAppScale { MinReplicas = 1, MaxReplicas = 1 };
            });
    }
}

///////////////////////////////////////////////////////////////////////////////
// UI
///////////////////////////////////////////////////////////////////////////////

IResourceBuilder<NextJsAppResource>? ui = null;

if (!infraOnly)
{
    var uiAuthSigningSecret = builder.ExecutionContext.IsPublishMode
        ? builder.AddParameter("uiAuthSigningSecret", secret: true)
        : builder.AddParameter("uiAuthSigningSecret", "development-secret-at-least-32-chars", secret: true);

    var uiKeycloakAuthorityRef = builder.ExecutionContext.IsPublishMode
        ? keycloakAuthorityRef
        : ReferenceExpression.Create($"http://localhost:15001/realms/admitto");

    // Using TLS is mandatory with Azure Database for PostgreSQL flexible server instances
    var dbConnectionStringRef = builder.ExecutionContext.IsPublishMode
        ? ReferenceExpression.Create($"{betterAuthDb.Resource.UriExpression}?sslmode=require")
        : betterAuthDb.Resource.UriExpression;

    ui = builder.AddNextJsApp("ui", "../Admitto.UI.Admin")
        .WithPnpm()
        .WithHttpEndpoint(port: 3000, name: "http", isProxied: false)
        .WithExternalHttpEndpoints()
        .WithEnvironment("BETTER_AUTH_SECRET", uiAuthSigningSecret)
        .WithEnvironment("BETTER_AUTH_URL", uiPublicUrl)
        .WithEnvironment("BETTER_AUTH_DB", dbConnectionStringRef)
        .WithEnvironment("AUTH_AUTHORITY", uiKeycloakAuthorityRef)
        .WithEnvironment("AUTH_CLIENT_ID", "admitto-ui")
        .WithEnvironment("AUTH_CLIENT_SECRET", uiAuthClientSecret)
        .WithEnvironment("AUTH_SCOPES", "openid profile email offline_access api.manage")
        .WithEnvironment("AUTH_PROMPT", "select_account")
        .WithEnvironment("ADMITTO_API_URL", apiUrlRef!)
        .WithEnvironment("PUBLIC_BASE_URL", uiPublicUrl)
        .WaitFor(betterAuthDb)
        .WaitFor(api!);

    if (builder.ExecutionContext.IsPublishMode)
    {
        var uiCustomDomain = builder.AddParameter("uiCustomDomain");
        var uiCertificateName = builder.AddParameter("uiCertificateName");

        ui.PublishAsAzureContainerApp((_, app) =>
        {
            app.Template.Scale = new ContainerAppScale { MinReplicas = 1, MaxReplicas = 1 };
            app.ConfigureCustomDomain(uiCustomDomain, uiCertificateName);
        });
    }

    // Add UI redirect URI and client secret to keycloak.
    keycloak
        .WithEnvironment("ADMITTO_UI_PUBLIC_URL", uiPublicUrl)
        .WithEnvironment("ADMITTO_UI_CLIENT_SECRET", uiAuthClientSecret);
}

///////////////////////////////////////////////////////////////////////////////
// Database Migrations
///////////////////////////////////////////////////////////////////////////////

var databaseScriptsPath = Path.Combine(builder.Environment.ContentRootPath, "DatabaseScripts");

if (api is not null)
{
    var organizationMigrations = api.AddEFMigrations(
            "organization-migrations",
            typeof(OrganizationDbContext).FullName!)
        .WithMigrationsProject("../Admitto.Core/Admitto.Core.csproj")
        .RunDatabaseUpdateOnStart()
        .PublishAsMigrationScript()
        .WithReference(admittoDb).WaitFor(admittoDb);

    var emailMigrations = api.AddEFMigrations(
            "email-migrations",
            typeof(EmailDbContext).FullName!)
        .WithMigrationsProject("../Admitto.Core/Admitto.Core.csproj")
        .RunDatabaseUpdateOnStart()
        .PublishAsMigrationScript()
        .WithReference(admittoDb).WaitFor(admittoDb);

    var registrationsMigrations = api.AddEFMigrations(
            "registrations-migrations",
            typeof(RegistrationsDbContext).FullName!)
        .WithMigrationsProject("../Admitto.Core/Admitto.Core.csproj")
        .RunDatabaseUpdateOnStart()
        .PublishAsMigrationScript()
        .WithReference(admittoDb).WaitFor(admittoDb);

    var badgesMigrations = api.AddEFMigrations(
            "badges-migrations",
            typeof(BadgesDbContext).FullName!)
        .WithMigrationsProject("../Admitto.Core/Admitto.Core.csproj")
        .RunDatabaseUpdateOnStart()
        .PublishAsMigrationScript()
        .WithReference(admittoDb).WaitFor(admittoDb);


    api
        .WaitForCompletion(organizationMigrations)
        .WaitForCompletion(emailMigrations)
        .WaitForCompletion(registrationsMigrations)
        .WaitForCompletion(badgesMigrations);

    if (builder.ExecutionContext.IsRunMode)
    {
        var quartzSchema = builder.AddContainer("quartz-schema", "postgres:17")
            .WithArgs(
                "sh",
                "-c",
                "psql \"$QUARTZ_DB_URI\" -v ON_ERROR_STOP=1 -f /scripts/quartz.sql")
            .WithParentRelationship(api)
            .WithBindMount(Path.Combine(databaseScriptsPath, "quartz.sql"), "/scripts/quartz.sql", isReadOnly: true)
            .WithReference(quartzDb).WaitFor(quartzDb);

        api.WaitForCompletion(quartzSchema);
    }
}

if (builder.ExecutionContext.IsRunMode && ui is not null)
{
    var betterAuthSchema = builder.AddContainer("better-auth-schema", "postgres:17")
        .WithArgs(
            "sh",
            "-c",
            "psql \"$BETTER_AUTH_DB_URI\" -v ON_ERROR_STOP=1 -f /scripts/better-auth.sql")
        .WithParentRelationship(ui)
        .WithBindMount(
            Path.Combine(databaseScriptsPath, "better-auth.sql"),
            "/scripts/better-auth.sql",
            isReadOnly: true)
        .WithReference(betterAuthDb).WaitFor(betterAuthDb);

    ui
        .WaitForCompletion(betterAuthSchema);
}

///////////////////////////////////////////////////////////////////////////////
// Build & Run
///////////////////////////////////////////////////////////////////////////////

try
{
    builder.Build().Run();
}
catch (AggregateException e) when (e.InnerException is TaskCanceledException)
{
    // Ignore task cancellation exceptions on shutdown. Annoying while debugging unit tests.
}
