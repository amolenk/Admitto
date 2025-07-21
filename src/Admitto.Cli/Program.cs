using Amolenk.Admitto.Cli.Commands;
using Amolenk.Admitto.Cli.Commands.Attendees;
using Amolenk.Admitto.Cli.Commands.Config;
using Amolenk.Admitto.Cli.Commands.Development;
using Amolenk.Admitto.Cli.Commands.Email.Template.Event;
using Amolenk.Admitto.Cli.Commands.Email.Template.Team;
using Amolenk.Admitto.Cli.Commands.Email.Test;
using Amolenk.Admitto.Cli.Commands.Events;
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
        "attendee",
        attendee =>
        {
            attendee.SetDescription("Manage attendees.");

            attendee.AddCommand<InviteAttendeeCommand>("invite");
            attendee.AddCommand<ListAttendeesCommand>("list");
        });

    config.AddBranch(
        "config",
        config =>
        {
            config.SetDescription("Manage configuration.");

            config.AddCommand<ClearConfigCommand>("clear").WithDescription("Clear configuration values");
            config.AddCommand<GetConfigCommand>("list").WithDescription("Get configuration values");
            config.AddCommand<SetConfigCommand>("set").WithDescription("Set configuration values");
        });
    
    config.AddBranch(
        "development",
        dev =>
        {
            dev.SetDescription("Development commands.");

            dev.AddCommand<CreateAttendeeCommand>("createAttendee");
            dev.AddCommand<VerifyAttendeeCommand>("verifyAttendee");
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
            
            email.AddBranch(
                "test",
                testEmail =>
                {
                    testEmail.AddBranch(
                        "registration",
                        registrationEmail =>
                        {
                            registrationEmail.AddCommand<TestRegistrationVerifyEmailCommand>("verify");
                        });
                });
            
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
        });
});

return app.Run(args);