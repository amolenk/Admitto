using Amolenk.Admitto.Cli;
using Amolenk.Admitto.Cli.Commands;
using Amolenk.Admitto.Cli.Infrastructure;
using Amolenk.Admitto.Cli.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection();

// Register configuration
services.AddSingleton<IConfiguration>(configuration);

// Register HTTP client
services.AddHttpClient<ApiService>(client =>
{
    var baseUrl = configuration["Api:BaseUrl"] ?? "https://localhost:5001/api/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register services
services.AddSingleton<IAuthService, AuthService>();
services.AddSingleton<ApiService>();

// Set up the CLI app
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddCommand<LoginCommand>("login")
        .WithDescription("Login to the Admitto API");

    config.AddCommand<LogoutCommand>("logout")
        .WithDescription("Logout from the Admitto API");

    config.AddBranch("team", team =>
    {
        team.SetDescription("Manage teams.");
        
        team.AddCommand<CreateTeamCommand>("create")
            .WithDescription("Create a new team");
        
        // TODO: Add other team commands
        // team.AddCommand<ListTeamsCommand>("list")
        //     .WithDescription("List teams");
        
        // team.AddBranch("member", member =>
        // {
        //     member.SetDescription("Manage team members");
        //     
        //     member.AddCommand<AddMemberCommand>("add")
        //         .WithDescription("Add a member to a team");
        //     
        //     member.AddCommand<RemoveMemberCommand>("remove")
        //         .WithDescription("Remove a member from a team");
        // });
    });
});

return app.Run(args);