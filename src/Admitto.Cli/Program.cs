using Amolenk.Admitto.Cli;
using Spectre.Console.Cli;

// var configuration = new ConfigurationBuilder()
//     .AddJsonFile("appsettings.json")
//     .Build();
//
// var services = new ServiceCollection();


// // Set up the CLI app
// var registrar = new TypeRegistrar(services);
var app = new CommandApp();//registrar);

app.Configure(config =>
{
    config.AddBranch("team", team =>
    {
        team.SetDescription("Manage teams.");
        
        team.AddCommand<CreateTeamCommand>("create")
            .WithDescription("Create a new team");
        
        team.AddCommand<CreateTeamCommand>("list")
            .WithDescription("List teams");
        
        team.AddBranch("member", member =>
        {
            member.SetDescription("Manage team members");
            
            member.AddCommand<CreateTeamCommand>("add")
                .WithDescription("Add a member to a team");
            
            // member.AddCommand<RemoveMemberCommand>("remove")
            //     .WithDescription("Remove a member from a team");
        });
    });

});

return app.Run(args);