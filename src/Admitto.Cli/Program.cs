using System.Security.Cryptography;
using System.Text;
using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Auth;
using Amolenk.Admitto.Cli.Commands;
// Quarantined namespaces (no matching admin endpoints in the regenerated API client).
// Restore when the backend re-exposes the corresponding surface.
// using Amolenk.Admitto.Cli.Commands.Attendee;
// using Amolenk.Admitto.Cli.Commands.Email;
// using Amolenk.Admitto.Cli.Commands.Email.Template.Event;
// using Amolenk.Admitto.Cli.Commands.Email.Template.Team;
// using Amolenk.Admitto.Cli.Commands.Email.Verification;
// using Amolenk.Admitto.Cli.Commands.Team.Member;
using Amolenk.Admitto.Cli.Commands.Auth;
using Amolenk.Admitto.Cli.Commands.Coupon;
using Amolenk.Admitto.Cli.Commands.Events;
using Amolenk.Admitto.Cli.Commands.Events.TicketType;
using Amolenk.Admitto.Cli.Commands.Team;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
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

if (!Directory.Exists(dataFolder))
{
    Directory.CreateDirectory(dataFolder);
}

var services = new ServiceCollection();

services.Configure<AdmittoOptions>(configuration.GetSection("Admitto"));
services.Configure<AuthOptions>(configuration.GetSection("Authentication"));

services.AddSingleton<IConfigService>(_ =>
    new ConfigService(Path.Combine(dataFolder, "config.json")));

services.AddSingleton<ITokenCache>(_ =>
    new TokenCache(
        Path.Combine(dataFolder, "tokens.json")));

services.AddSingleton<IAuthService, AuthService>();
services.AddTransient<IAdmittoService, AdmittoService>();
services.AddHttpClient();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    // Quarantined: backend admin attendee endpoints removed. Restore when the API exposes them again.
    // config.AddBranch(
    //     "attendee",
    //     attendee =>
    //     {
    //         attendee.SetDescription("Manage attendees");
    //
    //         attendee.AddCommand<CancelAttendeeCommand>("cancel")
    //             .WithDescription("Cancel an attendee registration");
    //
    //         attendee.AddCommand<DenyAttendeeVisaLetterCommand>("denyVisaLetter")
    //             .WithDescription("Deny a request for a visa letter and cancel the registration");
    //
    //         attendee.AddCommand<ExportAttendeesCommand>("export")
    //             .WithDescription("Export all attendee registrations to an Excel file");
    //
    //         attendee.AddCommand<ListAttendeesCommand>("list")
    //             .WithDescription("List all attendee registrations");
    //
    //         attendee.AddCommand<ReconfirmAttendeeCommand>("reconfirm")
    //             .WithDescription("Reconfirms an attendee registration");
    //
    //         attendee.AddCommand<RegisterAttendeeCommand>("register")
    //             .WithDescription("Register a new attendee");
    //
    //         attendee.AddCommand<ShowAttendeeCommand>("show")
    //             .WithDescription("Show the details of an attendee registration");
    //
    //         attendee.AddCommand<UpdateAttendeeCommand>("update")
    //             .WithDescription("Updates an existing attendee");
    //     });

    config.AddCommand<LoginCommand>("login")
        .WithDescription("Login to the Admitto API");

    config.AddCommand<LogoutCommand>("logout")
        .WithDescription("Logout from the Admitto API");

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
        "coupon",
        coupon =>
        {
            coupon.SetDescription("Manage coupons");

            coupon.AddCommand<CreateCouponCommand>("create")
                .WithDescription("Create a new coupon");

            coupon.AddCommand<ListCouponsCommand>("list")
                .WithDescription("List all coupons for an event");

            coupon.AddCommand<RevokeCouponCommand>("revoke")
                .WithDescription("Revoke a coupon");

            coupon.AddCommand<ShowCouponCommand>("show")
                .WithDescription("Show coupon details");
        });

    // config.AddBranch(
    //     "contributor",
    //     contributor =>
    //     {
    //         contributor.SetDescription("Manage contributors");
    //
    //         contributor.AddCommand<Commands.Contributor.AddContributorCommand>("add")
    //             .WithDescription("Add a contributor");
    //
    //         contributor.AddCommand<Commands.Contributor.ListContributorsCommand>("list")
    //             .WithDescription("List all contributors of an event");
    //
    //         contributor.AddCommand<Commands.Contributor.RemoveContributorCommand>("remove")
    //             .WithDescription("Remove a contributor");
    //
    //         contributor.AddCommand<Commands.Contributor.UpdateContributorCommand>("update")
    //             .WithDescription("Update an existing contributor");
    //     });

    // Quarantined: backend admin endpoints removed for bulk emails, recipient lists,
    // single-attendee email actions, templates, test email, and OTP verification.
    // Restore once the API exposes the corresponding admin surfaces again.
    // (Event-level email *settings* are handled under the `event` branch instead.)
    //
    // config.AddBranch(
    //     "email",
    //     email =>
    //     {
    //         email.SetDescription("Manage emails");
    //
    //         email.AddBranch("bulk", bulk => { /* ... */ });
    //         email.AddBranch("recipientList", list => { /* ... */ });
    //         email.AddBranch("send", send => { /* ... */ });
    //         email.AddBranch("template", tpl => { /* ... */ });
    //         email.AddCommand<TestEmailCommand>("test");
    //
    //         #if DEBUG
    //         email.AddBranch("verification", verification => { /* ... */ });
    //         #endif
    //     });


    config.AddBranch(
        "event",
        ticketedEvent =>
        {
            ticketedEvent.SetDescription("Manage events");

            ticketedEvent.AddCommand<CreateEventCommand>("create")
                .WithDescription("Request creation of a new event");

            // Quarantined: backend no longer exposes a list-events admin endpoint.
            // Restore once the API exposes a team-scoped event listing again.
            // ticketedEvent.AddCommand<ListEventsCommand>("list")
            //     .WithDescription("List all events for a team");

            ticketedEvent.AddCommand<ShowEventCommand>("show")
                .WithDescription("Show the details of an event");

            ticketedEvent.AddCommand<UpdateEventCommand>("update")
                .WithDescription("Update event details");

            ticketedEvent.AddCommand<CancelEventCommand>("cancel")
                .WithDescription("Cancel an event");

            ticketedEvent.AddCommand<ArchiveEventCommand>("archive")
                .WithDescription("Archive an event");

            ticketedEvent.AddBranch(
                "ticketType",
                ticketType =>
                {
                    ticketType.SetDescription("Manage ticket types");

                    ticketType.AddCommand<AddTicketTypeCommand>("add")
                        .WithDescription("Add a ticket type");

                    ticketType.AddCommand<CancelTicketTypeCommand>("cancel")
                        .WithDescription("Cancel a ticket type and update/cancel existing registrations");

                    ticketType.AddCommand<UpdateTicketTypeCommand>("update")
                        .WithDescription("Update an existing ticket type");
                });

            ticketedEvent.AddBranch(
                "registration",
                registration =>
                {
                    registration.SetDescription("Manage event registration lifecycle");

                    registration.AddCommand<Commands.Events.Registration.ShowRegistrationStatusCommand>("show")
                        .WithDescription("Show the current registration open status");
                });

            ticketedEvent.AddBranch(
                "email",
                email =>
                {
                    email.SetDescription("Manage event email settings");

                    email.AddCommand<Commands.Events.Email.ShowEventEmailCommand>("show")
                        .WithDescription("Show the email settings for the event");

                    email.AddCommand<Commands.Events.Email.UpdateEventEmailCommand>("update")
                        .WithDescription("Create or update the email settings for the event");
                });

            ticketedEvent.AddBranch(
                "policy",
                policy =>
                {
                    policy.SetDescription("Manage event policies");

                    policy.AddBranch(
                        "registration",
                        registration =>
                        {
                            registration.SetDescription("Manage registration policy");

                            registration
                                .AddCommand<Commands.Events.Policy.Registration.ConfigureRegistrationPolicyCommand>("configure")
                                .WithDescription("Configure the registration policy");
                        });

                    policy.AddBranch(
                        "cancellation",
                        cancellation =>
                        {
                            cancellation.SetDescription("Manage cancellation policy");

                            cancellation
                                .AddCommand<Commands.Events.Policy.Cancellation.ConfigureCancellationPolicyCommand>("configure")
                                .WithDescription("Configure the cancellation policy");
                        });

                    policy.AddBranch(
                        "reconfirm",
                        reconfirm =>
                        {
                            reconfirm.SetDescription("Manage reconfirm policy");

                            reconfirm
                                .AddCommand<Commands.Events.Policy.Reconfirm.ConfigureReconfirmPolicyCommand>("configure")
                                .WithDescription("Configure the reconfirm policy");
                        });
                });
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

            // Quarantined: team member commands depend on removed TeamMemberRole and ApiClientMemberExtensions.
            // team.AddBranch(
            //     "member",
            //     member =>
            //     {
            //         member.SetDescription("Manage team members");
            //
            //         member.AddCommand<AddTeamMemberCommand>("add")
            //             .WithDescription("Add a team member");
            //
            //         member.AddCommand<ListTeamMembersCommand>("list")
            //             .WithDescription("List team members");
            //
            //         member.AddCommand<UpdateTeamMemberCommand>("update")
            //             .WithDescription("Update a team member's role");
            //
            //         member.AddCommand<RemoveTeamMemberCommand>("remove")
            //             .WithDescription("Remove a team member");
            //     });

            team.AddCommand<ShowTeamCommand>("show")
                .WithDescription("Show the details of a team");

            team.AddCommand<UpdateTeamCommand>("update")
                .WithDescription("Updates details of an existing team");

            team.AddCommand<ArchiveTeamCommand>("archive")
                .WithDescription("Archive a team");
        });

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Show version information");
});

return app.Run(args);