using System.Security.Cryptography;
using System.Text;
using Amolenk.Admitto.Cli.Commands;
using Amolenk.Admitto.Cli.Commands.Attendee;
using Amolenk.Admitto.Cli.Commands.Auth;
using Amolenk.Admitto.Cli.Commands.Email;
using Amolenk.Admitto.Cli.Commands.Email.Template.Event;
using Amolenk.Admitto.Cli.Commands.Email.Template.Team;
using Amolenk.Admitto.Cli.Commands.Email.Verification;
using Amolenk.Admitto.Cli.Commands.Events;
using Amolenk.Admitto.Cli.Commands.Events.TicketType;
using Amolenk.Admitto.Cli.Commands.Migration;
using Amolenk.Admitto.Cli.Commands.Team;
using Amolenk.Admitto.Cli.Commands.Team.Member;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Common.Auth;
using Microsoft.Extensions.DependencyInjection;
using Commands = Amolenk.Admitto.Cli.Commands;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .Build();

// Determine the folder to store data. This is based on a hash of the endpoint to allow
// multiple configurations for different endpoints.
var endpoint = configuration["Admitto:Endpoint"] ?? "http://localhost:5100";
var dataFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "admitto-cli",
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint)))[..8]);

var services = new ServiceCollection();

services.Configure<AdmittoOptions>(configuration.GetSection("Admitto"));
services.Configure<AuthOptions>(configuration.GetSection("Authentication"));

services.AddSingleton<IConfigService>(_ =>
    new ConfigService(Path.Combine(dataFolder, "config.json")));

services.AddSingleton<ITokenCache>(_ =>
    new TokenCache(
        Path.Combine(dataFolder, "tokens.json")));

services.AddTransient<IAccessTokenProvider, AccessTokenProvider>();
services.AddSingleton<IAuthService, AuthService>();
services.AddTransient<IApiService, ApiService>();
services.AddHttpClient();

// Set up the CLI app
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddBranch(
        "attendee",
        attendee =>
        {
            attendee.SetDescription("Manage attendees");

            attendee.AddCommand<CancelAttendeeCommand>("cancel")
                .WithDescription("Cancel an attendee registration");

            attendee.AddCommand<ListAttendeesCommand>("list")
                .WithDescription("List all attendee registrations");

            attendee.AddCommand<ReconfirmAttendeeCommand>("reconfirm")
                .WithDescription("Reconfirms an attendee registration");

            attendee.AddCommand<RegisterAttendeeCommand>("register")
                .WithDescription("Register a new attendee");

            attendee.AddCommand<ShowAttendeeCommand>("show")
                .WithDescription("Show the details of an attendee registration");
        });

    config.AddBranch(
        "auth",
        auth =>
        {
            auth.SetDescription("Manage authentication");

            auth.AddCommand<LoginCommand>("login")
                .WithDescription("Login to the Admitto API");

            auth.AddCommand<LogoutCommand>("logout")
                .WithDescription("Logout from the Admitto API");
        });

    config.AddBranch(
        "config",
        cfg =>
        {
            cfg.SetDescription("Manage configuration");

            cfg.AddCommand<ClearConfigCommand>("clear")
                .WithDescription("Clear configuration values");

            cfg.AddCommand<GetConfigCommand>("list")
                .WithDescription("List all configuration values");

            cfg.AddCommand<SetConfigCommand>("set")
                .WithDescription("Set configuration values");
        });

    config.AddBranch(
        "contributor",
        contributor =>
        {
            contributor.SetDescription("Manage contributors");

            contributor.AddCommand<Commands.Contributor.AddContributorCommand>("add")
                .WithDescription("Add a contributor");

            contributor.AddCommand<Commands.Contributor.ListContributorsCommand>("list")
                .WithDescription("List all contributors of an event");

            contributor.AddCommand<Commands.Contributor.RemoveContributorCommand>("remove")
                .WithDescription("Remove a contributor");

            contributor.AddCommand<Commands.Contributor.UpdateContributorCommand>("update")
                .WithDescription("Update an existing contributor");
        });

    config.AddBranch(
        "email",
        email =>
        {
            email.SetDescription("Manage emails");

            email.AddBranch(
                "bulk",
                bulk =>
                {
                    bulk.SetDescription("Manage bulk emails");

                    bulk.AddCommand<Commands.Email.Bulk.ListBulkEmailsCommand>("list")
                        .WithDescription("List all bulk emails");

                    bulk.AddCommand<Commands.Email.Bulk.RemoveBulkEmailCommand>("remove")
                        .WithDescription("Remove a scheduled bulk email");

                    bulk.AddCommand<Commands.Email.Bulk.ScheduleBulkEmailCommand>("schedule")
                        .WithDescription("Schedule a bulk email");
                });

            // email.AddBranch(
            //     "send",
            //     sendEmail =>
            //     {
            //         sendEmail.SetDescription("Manage registration email templates.");
            //
            //         sendEmail.AddBranch(
            //             "attendee",
            //             attendeeEmail =>
            //             {
            //                 attendeeEmail.AddCommand<SendRegistrationVerifyEmailCommand>("ticket");
            //             });
            //
            //         sendEmail.AddBranch(
            //             "registration",
            //             registrationEmail =>
            //             {
            //                 registrationEmail.AddCommand<SendRegistrationVerifyEmailCommand>("verify");
            //             });
            //     });


            email.AddBranch(
                "template",
                template =>
                {
                    template.SetDescription("Manage email templates");

                    template.AddBranch(
                        "team",
                        team =>
                        {
                            template.SetDescription("Manage team templates");

                            team.AddCommand<ClearTeamEmailTemplateCommand>("clear")
                                .WithDescription("Clear a team email template");

                            team.AddCommand<ListTeamEmailTemplatesCommand>("list")
                                .WithDescription("List all team email templates");

                            team.AddCommand<SetTeamEmailTemplateCommand>("set")
                                .WithDescription("Set a team email template");
                        });

                    template.AddBranch(
                        "event",
                        ticketedEvent =>
                        {
                            template.SetDescription("Manage team templates");

                            ticketedEvent.AddCommand<ClearEventEmailTemplateCommand>("clear")
                                .WithDescription("Clear an event email template");

                            ticketedEvent.AddCommand<ListEventEmailTemplatesCommand>("list")
                                .WithDescription("List all event email templates");

                            ticketedEvent.AddCommand<SetEventEmailTemplateCommand>("set")
                                .WithDescription("Set an event email template");
                        });
                });

            email.AddCommand<TestEmailCommand>("test")
                .WithDescription("Send a test email");
#if DEBUG
            email.AddBranch(
                "verification",
                verification =>
                {
                    verification.SetDescription("Manage email verification (debug only)");

                    verification.AddCommand<RequestOtpCodeCommand>("request")
                        .WithDescription("Request a one-time verification code");

                    verification.AddCommand<VerifyOtpCodeCommand>("verify")
                        .WithDescription("Verify a one-time verification code");
                });
#endif
        });


    config.AddBranch(
        "event",
        ticketedEvent =>
        {
            ticketedEvent.SetDescription("Manage events");

            ticketedEvent.AddCommand<CreateEventCommand>("create")
                .WithDescription("Create a new event");

            ticketedEvent.AddCommand<ListEventsCommand>("list")
                .WithDescription("List all events for a team");

            ticketedEvent.AddCommand<ShowEventCommand>("show")
                .WithDescription("Show the details of an event");

            ticketedEvent.AddBranch(
                "ticketType",
                ticketType =>
                {
                    ticketType.SetDescription("Manage ticket types");

                    ticketType.AddCommand<AddTicketTypeCommand>("add")
                        .WithDescription("Add a ticket type");

                    ticketType.AddCommand<UpdateTicketTypeCommand>("update")
                        .WithDescription("Update an existing ticket type");
                });

            ticketedEvent.AddBranch(
                "policy",
                policy =>
                {
                    policy.SetDescription("Manage event policies");

                    policy.AddBranch(
                        "reconfirm",
                        reconfirm =>
                        {
                            reconfirm.SetDescription("Manage reconfirm policy");

                            reconfirm.AddCommand<Commands.Events.Policy.Reconfirm.ClearReconfirmPolicyCommand>("clear")
                                .WithDescription("Clear the reconfirm policy");

                            // reconfirm.AddCommand<Commands.Events.Policy.Reconfirm.SetReconfirmPolicyCommand>("set")
                            //     .WithDescription("Set the reconfirm policy");
                        });

                    policy.AddBranch(
                        "registration",
                        registration =>
                        {
                            registration.SetDescription("Manage registration policy");

                            registration
                                .AddCommand<Commands.Events.Policy.Registration.SetRegistrationPolicyCommand>("set")
                                .WithDescription("Set the registration policy");
                        });
                });
        });

    config.AddBranch(
        "migration",
        migration =>
        {
            migration.SetDescription("Manage migrations");

            migration.AddCommand<ListMigrationsCommand>("list")
                .WithDescription("List all migration");

            migration.AddCommand<RunMigrationCommand>("run")
                .WithDescription("Run a migration");
        });

    config.AddBranch(
        "team",
        team =>
        {
            team.SetDescription("Manage teams");

            team.AddCommand<CreateTeamCommand>("create")
                .WithDescription("Create a new team");

            team.AddCommand<ListTeamsCommand>("list")
                .WithDescription("List all teams");

            team.AddBranch(
                "member",
                member =>
                {
                    member.SetDescription("Manage team members");

                    member.AddCommand<AddTeamMemberCommand>("add")
                        .WithDescription("Add a team member");
                });

            team.AddCommand<ShowTeamCommand>("show")
                .WithDescription("Show the details of a team");

            team.AddCommand<UpdateTeamCommand>("update")
                .WithDescription("Updates details of an existing team");
        });

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Show version information");
});

return app.Run(args);