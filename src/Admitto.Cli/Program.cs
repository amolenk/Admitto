using Amolenk.Admitto.Cli.Commands;
using Amolenk.Admitto.Cli.Commands.Attendee;
using Amolenk.Admitto.Cli.Commands.Email;
using Amolenk.Admitto.Cli.Commands.Email.Template.Event;
using Amolenk.Admitto.Cli.Commands.Email.Template.Team;
using Amolenk.Admitto.Cli.Commands.Email.Verification;
using Amolenk.Admitto.Cli.Commands.Events;
using Amolenk.Admitto.Cli.Commands.Events.TicketType;
using Amolenk.Admitto.Cli.Commands.Migration;
using Amolenk.Admitto.Cli.Commands.Team;
using Amolenk.Admitto.Cli.Commands.Team.Member;
using Amolenk.Admitto.Cli.Commands.Teams;
using Amolenk.Admitto.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Commands = Amolenk.Admitto.Cli.Commands;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(ConfigSettings.GetConfigPath(), optional: true)
    .AddUserSecrets<Program>()
    .Build();


var services = new ServiceCollection();


services.Configure<CliAuthOptions>(configuration.GetSection("Authentication"));


// Register configuration
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<InputService>();
services.AddSingleton<OutputService>();



// Register services

services.AddHttpClient();

services.AddSingleton<ITokenCache>(_ =>
    new TokenCache(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "admitto-cli",
            "tokens.json")));
services.AddSingleton<IAuthService, AuthService>();
services.AddTransient<IAccessTokenProvider, AccessTokenProvider>();

services.AddSingleton<ConfigProvider>(_ =>
    new ConfigProvider(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "admitto-cli",
            "config.json")));


services.AddTransient<IApiService, ApiService>();

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

            attendee.AddCommand<CancelCommand>("cancel")
                .WithDescription("Cancel an attendee registration");
            
            attendee.AddCommand<Commands.Attendee.ListCommand>("list")
                .WithDescription("List all attendee registrations");

            attendee.AddCommand<ReconfirmCommand>("reconfirm")
                .WithDescription("Reconfirms an attendee registration");

            attendee.AddCommand<RegisterAttendeeCommand>("register")
                .WithDescription("Register a new attendee");

            attendee.AddCommand<ShowCommand>("show")
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
    
            contributor.AddCommand<Commands.Contributor.AddCommand>("add")
                .WithDescription("Add a contributor");
                    
            contributor.AddCommand<Commands.Contributor.ListCommand>("list")
                .WithDescription("List all contributors of an event");

            contributor.AddCommand<Commands.Contributor.RemoveCommand>("remove")
                .WithDescription("Remove a contributor");
            
            contributor.AddCommand<Commands.Contributor.UpdateCommand>("update")
                .WithDescription("Updates a contributor");
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

                    bulk.AddCommand<Commands.Email.Bulk.ListCommand>("list")
                        .WithDescription("List all bulk emails");

                    bulk.AddCommand<Commands.Email.Bulk.RemoveCommand>("remove")
                        .WithDescription("Removes a scheduled bulk email");

                    bulk.AddCommand<Commands.Email.Bulk.ScheduleCommand>("schedule")
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

                            reconfirm.AddCommand<Commands.Events.Policy.Reconfirm.ClearCommand>("clear")
                                .WithDescription("Clear the reconfirm policy");
                            
                            reconfirm.AddCommand<Commands.Events.Policy.Reconfirm.SetCommand>("set")
                                .WithDescription("Set the reconfirm policy");
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
        });
    
    config.AddCommand<VersionCommand>("version")
        .WithDescription("Show version information");
});

return app.Run(args);