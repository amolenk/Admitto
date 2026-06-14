using Amolenk.Admitto.AppHost.Extensions;
using Azure.Provisioning.AppContainers;
using Aspire.Hosting.Azure;
using Azure.Provisioning.PostgreSql;
using Microsoft.Extensions.Hosting;

// Disable warnings for experimental features.
#pragma warning disable ASPIREJAVASCRIPT001
#pragma warning disable ASPIREACADOMAINS001

var builder = DistributedApplication.CreateBuilder(args);

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
else
{
    // Allow PGCRYPTO extension so we can generate a signing key in the database for use with protected data like
    // SMTP passwords.
    postgres.ConfigureInfrastructure(infra =>
    {
        var server = infra.GetProvisionableResources()
            .OfType<PostgreSqlFlexibleServer>()
            .Single();

        infra.Add(new PostgreSqlFlexibleServerConfiguration("postgresAzureExtensions")
        {
            Parent = server,
            Name = "azure.extensions",
            Source = "user-override",
            Value = "PGCRYPTO"
        });
    });
}

var postgresDb = postgres.AddDatabase("admitto-db");
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
// Keycloak
///////////////////////////////////////////////////////////////////////////////

var keycloakAdminUser = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("keycloakAdminUser")
    : builder.AddParameter("keycloakAdminUser", value: "admin");

var keycloakAdminPassword = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("keycloakAdminPassword", secret: true)
    : builder.AddParameter("keycloakAdminPassword", value: "admin", secret: true);

IResourceBuilder<ContainerResource> keycloak;

if (builder.ExecutionContext.IsPublishMode)
{
    var keycloakPublicUrl = builder.AddParameter("keycloakPublicUrl");

    keycloak = builder.AddContainer("keycloak", "admitto-keycloak")
        .WithDockerfile("./KeycloakConfiguration")
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithExternalHttpEndpoints()
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
        .WithEnvironment("KC_DB", "postgres")
        .WithEnvironment(
            "KC_DB_URL",
            ReferenceExpression.Create(
                $"jdbc:postgresql://{postgres.Resource.HostName}/keycloak-db?sslmode=require"))
        .WithEnvironment("KC_DB_USERNAME", postgres.Resource.UserName!)
        .WithEnvironment("KC_DB_PASSWORD", postgres.Resource.Password!)
        .WithEnvironment("KC_HTTP_ENABLED", "true")
        .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
        .WithEnvironment("KC_HOSTNAME", keycloakPublicUrl)
        .WithEnvironment("KC_HEALTH_ENABLED", "true")
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

    keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak:26.6")
        .WithArgs("start-dev", "--import-realm")
        .WithHttpEndpoint(port: keycloakHttpPort, targetPort: 8080, name: "http", isProxied: false)
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
        .WithEnvironment("KC_HTTP_ENABLED", "true")
        .WithEnvironment("KC_HEALTH_ENABLED", "true")
        .WithBindMount(
            Path.Combine(builder.Environment.ContentRootPath, "KeycloakConfiguration", "AdmittoRealm.Deployment.json"),
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

var keycloakAuthorityRef = ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/admitto");

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
}

///////////////////////////////////////////////////////////////////////////////
// Migrations
///////////////////////////////////////////////////////////////////////////////

var migrations = builder.AddProject<Projects.Admitto_Migrations>("migrations")
    .WithReference(postgresDb).WaitFor(postgresDb)
    .WithReference(quartzDb).WaitFor(quartzDb)
    .WithReference(betterAuthDb).WaitFor(betterAuthDb);

if (builder.ExecutionContext.IsPublishMode)
{
    migrations
        .WithReference(appInsights!)
        .PublishAsAzureContainerAppJob((_, job) =>
        {
            job.Configuration.TriggerType = ContainerAppJobTriggerType.Manual;
            job.Configuration.ReplicaRetryLimit = 1;
        });
}

///////////////////////////////////////////////////////////////////////////////
// API
///////////////////////////////////////////////////////////////////////////////

IResourceBuilder<ProjectResource>? api = null;
ReferenceExpression? apiUrlRef = null;

if (builder.ExecutionContext.IsPublishMode
    || builder.Environment.IsDevelopment() || builder.Environment.IsEndToEndTesting())
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
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__AUTHORITY", keycloakAuthorityRef)
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__TOKENPATH", "/realms/master/protocol/openid-connect/token")
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__CLIENTID", "admin-cli")
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__USERNAME", keycloakAdminUser)
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__PASSWORD", keycloakAdminPassword)
        .WithReference(postgresDb)
        .WithReference(quartzDb)
        .WithReference(serviceBus).WaitFor(serviceBus)
        .WaitFor(keycloak)
        .WaitForCompletion(migrations);



    // Bootstrap admin user
    var apiBootstrapAdmin = builder.ExecutionContext.IsPublishMode
        ? builder.AddParameter("apiBootstrapAdmin")
        : builder.AddParameter("apiBootstrapAdmin", value: "alice@example.com");
    //
    api.WithEnvironment("ORGANIZATION__BOOTSTRAPADMIN__EMAILADDRESS", apiBootstrapAdmin);

    if (builder.ExecutionContext.IsRunMode)
    {
        if (builder.Environment.IsEndToEndTesting())
        {
            api
                .WithEnvironment("RATELIMITING__PUBLIC__STRICT__PERMITLIMIT", "1000")
                .WithEnvironment("RATELIMITING__PUBLIC__STANDARD__PERMITLIMIT", "5000");
        }
    }
    else
    {
        var apiCustomDomain = builder.AddParameter("apiCustomDomain");
        var apiCertificateName = builder.AddParameter("apiCertificateName");

        api.WithReference(appInsights!).PublishAsAzureContainerApp((_, app) =>
        {
            app.Template.Scale = new ContainerAppScale { MinReplicas = 1, MaxReplicas = 1 };
            app.ConfigureCustomDomain(apiCustomDomain, apiCertificateName);
        });
    }

    apiUrlRef = ReferenceExpression.Create($"{api.GetEndpoint("http")}");
}

///////////////////////////////////////////////////////////////////////////////
// Worker
///////////////////////////////////////////////////////////////////////////////

if (builder.ExecutionContext.IsPublishMode
    || builder.Environment.IsDevelopment() || builder.Environment.IsEndToEndTesting())
{
    var worker = builder.AddProject<Projects.Admitto_Worker>("worker")
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__AUTHORITY", keycloakAuthorityRef)
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__TOKENPATH", "/realms/master/protocol/openid-connect/token")
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__CLIENTID", "admin-cli")
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__USERNAME", keycloakAdminUser)
        .WithEnvironment("ORGANIZATION__USERDIRECTORIES__KEYCLOAK__PASSWORD", keycloakAdminPassword)
        .WithReferenceEnvironment(ReferenceEnvironmentInjectionFlags.ConnectionString)
        .WithReference(postgresDb)
        .WithReference(quartzDb)
        .WithReference(serviceBus).WaitFor(serviceBus)
        .WaitFor(keycloak)
        .WaitForCompletion(migrations);

    if (builder.ExecutionContext.IsRunMode)
    {
        worker
            // Disable caching to avoid stale data issues in tests
            .WithEnvironment("CACHING__ENABLED", builder.Environment.IsDevelopment().ToString())
            // Disable the per-message delay so bulk-email fan-out completes quickly
            .WithEnvironment("BULKEMAIL__PERMESSAGEDELAY", "00:00:00")
            .WaitFor(mailDev!);
    }
    else
    {
        worker
            .WithReference(appInsights!)
            .PublishAsAzureContainerApp((_, app) =>
            {
                app.Template.Scale = new ContainerAppScale { MinReplicas = 1, MaxReplicas = 1 };
            });
    }
}

///////////////////////////////////////////////////////////////////////////////
// UI
///////////////////////////////////////////////////////////////////////////////

if (builder.ExecutionContext.IsPublishMode || builder.Environment.IsDevelopment())
{
    var uiPublicUrl = builder.ExecutionContext.IsPublishMode
        ? builder.AddParameter("uiPublicUrl")
        : builder.AddParameter("uiPublicUrl", "http://localhost:3000");

    var uiAuthSigningSecret = builder.ExecutionContext.IsPublishMode
        ? builder.AddParameter("uiAuthSigningSecret", secret: true)
        : builder.AddParameter("uiAuthSigningSecret", "development-secret-at-least-32-chars", secret: true);

    var uiAuthClientSecret = builder.ExecutionContext.IsPublishMode
        ? builder.AddParameter("uiAuthClientSecret", secret: true)
        : builder.AddParameter("uiAuthClientSecret", value: "admitto-ui-dev-secret", secret: true);

    var uiKeycloakAuthorityRef = builder.ExecutionContext.IsPublishMode
        ? keycloakAuthorityRef
        : ReferenceExpression.Create($"http://localhost:15001/realms/admitto");

    // Using TLS is mandatory with Azure Database for PostgreSQL flexible server instances
    var dbConnectionStringRef = builder.ExecutionContext.IsPublishMode
        ? ReferenceExpression.Create($"{betterAuthDb.Resource.UriExpression}?sslmode=require")
        : betterAuthDb.Resource.UriExpression;

    var uiCustomDomain = builder.AddParameter("uiCustomDomain");
    var uiCertificateName = builder.AddParameter("uiCertificateName");

    builder.AddNextJsApp("ui", "../Admitto.UI.Admin")
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
        .WaitFor(api!)
        .PublishAsAzureContainerApp((_, app) =>
        {
            app.Template.Scale = new ContainerAppScale { MinReplicas = 1, MaxReplicas = 1 };
            app.ConfigureCustomDomain(uiCustomDomain, uiCertificateName);
        });

    keycloak
        .WithEnvironment("ADMITTO_UI_PUBLIC_URL", uiPublicUrl)
        .WithEnvironment("ADMITTO_UI_CLIENT_SECRET", uiAuthClientSecret);
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
