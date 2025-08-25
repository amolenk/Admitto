using Amolenk.Admitto.Cli.Commands;
using Amolenk.Admitto.Cli.Commands.Config;
using Amolenk.Admitto.Cli.Commands.Email;
using Amolenk.Admitto.Cli.Commands.Email.Template.Event;
using Amolenk.Admitto.Cli.Commands.Email.Template.Team;
using Amolenk.Admitto.Cli.Commands.Email.Verification;
using Amolenk.Admitto.Cli.Commands.Events;
using Amolenk.Admitto.Cli.Commands.Events.TicketType;
using Amolenk.Admitto.Cli.Commands.Registration;
using Amolenk.Admitto.Cli.Commands.Registrations;
using Amolenk.Admitto.Cli.Commands.Team.Member;
using Amolenk.Admitto.Cli.Commands.Teams;
using Amolenk.Admitto.Cli.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(ConfigSettings.GetConfigPath(), optional: true)
    .AddUserSecrets<Program>()
    .Build();




var services = new ServiceCollection();


services.Configure<CliAuthOptions>(configuration.GetSection("Authentication"));


// Register configuration
services.AddSingleton<IConfiguration>(configuration);



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

// Set up the CLI app
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddBranch(
        "registration",
        attendee =>
        {
            attendee.SetDescription("Manage registrations.");

            #if DEBUG
            attendee.AddCommand<RegisterCommand>("create");
            #endif
            
            attendee.AddCommand<InviteCommand>("invite");
            attendee.AddCommand<ListRegistrationsCommand>("list");
            attendee.AddCommand<GetRegistrationCommand>("show");

        });

    config.AddBranch(
        "config",
        cfg =>
        {
            cfg.SetDescription("Manage configuration.");

            cfg.AddCommand<ClearConfigCommand>("clear").WithDescription("Clear configuration values");
            cfg.AddCommand<GetConfigCommand>("list").WithDescription("Get configuration values");
            cfg.AddCommand<SetConfigCommand>("set").WithDescription("Set configuration values");
        });
    
    config.AddCommand<LoginCommand>("login")
        .WithDescription("Login to the Admitto API");

    config.AddCommand<LogoutCommand>("logout")
        .WithDescription("Logout from the Admitto API");

    config.AddBranch(
        "team",
        team =>
        {
            team.SetDescription("Manage teams.");

            team.AddCommand<CreateTeamCommand>("create").WithDescription("Create a new team");
            team.AddCommand<ListTeamsCommand>("list").WithDescription("List teams");
            team.AddCommand<ShowTeamCommand>("show").WithDescription("Show the details of a team");

            team.AddBranch(
                "member",
                member =>
                {
                    member.AddCommand<AddTeamMemberCommand>("add");
                });
        });

    config.AddBranch(
        "event",
        ticketedEvent =>
        {
            ticketedEvent.SetDescription("Manage events.");

            ticketedEvent.AddCommand<CreateEventCommand>("create").WithDescription("Create a new event");
            ticketedEvent.AddCommand<ListEventsCommand>("list").WithDescription("List teams");
            ticketedEvent.AddCommand<ShowEventCommand>("show").WithDescription("Show the details of an event");

            ticketedEvent.AddBranch(
                "ticketType",
                ticketType =>
                {
                    ticketType.SetDescription("Manage event ticket types.");

                    ticketType.AddCommand<AddTicketTypeCommand>("add").WithDescription("Add a ticket type to an event");
                });
        });
    
    config.AddBranch(
        "email",
        email =>
        {
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
            
            email.AddCommand<TestEmailCommand>("test");
            email.AddCommand<PreviewEmailCommand>("preview");
            
            email.AddBranch(
                "template",
                template =>
                {
                    template.AddBranch(
                        "team",
                        team =>
                        {
                            team.AddCommand<ClearTeamEmailTemplateCommand>("clear");
                            team.AddCommand<ListTeamEmailTemplatesCommand>("list");
                            team.AddCommand<SetTeamEmailTemplateCommand>("set");
                        });

                    template.AddBranch(
                        "event",
                        ticketedEvent =>
                        {
                            ticketedEvent.AddCommand<ClearEventEmailTemplateCommand>("clear");
                            ticketedEvent.AddCommand<ListEventEmailTemplatesCommand>("list");
                            ticketedEvent.AddCommand<SetEventEmailTemplateCommand>("set");
                        });
                });
            
            #if DEBUG
            email.AddBranch(
                "verification",
                verification =>
                {
                    verification.AddCommand<RequestOtpCodeCommand>("request");
                    verification.AddCommand<VerifyOtpCodeCommand>("verify");
                });
            #endif

        });
});

return app.Run(args);