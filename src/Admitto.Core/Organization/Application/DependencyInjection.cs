using Amolenk.Admitto.Core.Organization.Application.Jobs;
using Amolenk.Admitto.Core.Organization.Application.UseCases;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.GetApiKeys;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.RevokeApiKey;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.ArchiveTeam;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.CreateTeam;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeam;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeams;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.UpdateTeam;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser.EventHandlers;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventArchived;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventArchived.EventHandlers;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCancelled;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCancelled.EventHandlers;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated.EventHandlers;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected.EventHandlers;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Users.GetTeamMembershipRole;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Amolenk.Admitto.Core.Organization.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddOrganizationModule(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Command handlers
        services.AddScoped<CreateApiKeyHandler>();
        services.AddScoped<ICommandHandler<CreateApiKeyCommand, CreateApiKeyResult>, CreateApiKeyHandler>(
            sp => sp.GetRequiredService<CreateApiKeyHandler>());
        services.AddScoped<RevokeApiKeyHandler>();
        services.AddScoped<ICommandHandler<RevokeApiKeyCommand>, RevokeApiKeyHandler>(
            sp => sp.GetRequiredService<RevokeApiKeyHandler>());
        services.AddScoped<ArchiveTeamHandler>();
        services.AddScoped<ICommandHandler<ArchiveTeamCommand>, ArchiveTeamHandler>(
            sp => sp.GetRequiredService<ArchiveTeamHandler>());
        services.AddScoped<CreateTeamHandler>();
        services.AddScoped<ICommandHandler<CreateTeamCommand>, CreateTeamHandler>(
            sp => sp.GetRequiredService<CreateTeamHandler>());
        services.AddScoped<UpdateTeamHandler>();
        services.AddScoped<ICommandHandler<UpdateTeamCommand>, UpdateTeamHandler>(
            sp => sp.GetRequiredService<UpdateTeamHandler>());
        services.AddScoped<AssignTeamMembershipHandler>();
        services.AddScoped<ICommandHandler<AssignTeamMembershipCommand>, AssignTeamMembershipHandler>(
            sp => sp.GetRequiredService<AssignTeamMembershipHandler>());
        services.AddScoped<ChangeTeamMembershipRoleHandler>();
        services.AddScoped<ICommandHandler<ChangeTeamMembershipRoleCommand>, ChangeTeamMembershipRoleHandler>(
            sp => sp.GetRequiredService<ChangeTeamMembershipRoleHandler>());
        services.AddScoped<RemoveTeamMembershipHandler>();
        services.AddScoped<ICommandHandler<RemoveTeamMembershipCommand>, RemoveTeamMembershipHandler>(
            sp => sp.GetRequiredService<RemoveTeamMembershipHandler>());
        services.AddScoped<RequestTicketedEventCreationHandler>();
        services.AddScoped<ICommandHandler<RequestTicketedEventCreationCommand, Guid>, RequestTicketedEventCreationHandler>(
            sp => sp.GetRequiredService<RequestTicketedEventCreationHandler>());
        services.AddScoped<RegisterTicketedEventCreatedHandler>();
        services.AddScoped<ICommandHandler<RegisterTicketedEventCreatedCommand>, RegisterTicketedEventCreatedHandler>(
            sp => sp.GetRequiredService<RegisterTicketedEventCreatedHandler>());
        services.AddScoped<RegisterTicketedEventCancelledHandler>();
        services.AddScoped<ICommandHandler<RegisterTicketedEventCancelledCommand>, RegisterTicketedEventCancelledHandler>(
            sp => sp.GetRequiredService<RegisterTicketedEventCancelledHandler>());
        services.AddScoped<RegisterTicketedEventCreationRejectedHandler>();
        services.AddScoped<ICommandHandler<RegisterTicketedEventCreationRejectedCommand>, RegisterTicketedEventCreationRejectedHandler>(
            sp => sp.GetRequiredService<RegisterTicketedEventCreationRejectedHandler>());
        services.AddScoped<RegisterTicketedEventArchivedHandler>();
        services.AddScoped<ICommandHandler<RegisterTicketedEventArchivedCommand>, RegisterTicketedEventArchivedHandler>(
            sp => sp.GetRequiredService<RegisterTicketedEventArchivedHandler>());

        // Query handlers
        services.AddScoped<GetApiKeysHandler>();
        services.AddScoped<IQueryHandler<GetApiKeysQuery, IReadOnlyList<ApiKeyListItemDto>>, GetApiKeysHandler>(
            sp => sp.GetRequiredService<GetApiKeysHandler>());
        services.AddScoped<ValidateApiKeyHandler>();
        services.AddScoped<IQueryHandler<ValidateApiKeyQuery, Guid?>, ValidateApiKeyHandler>(
            sp => sp.GetRequiredService<ValidateApiKeyHandler>());
        services.AddScoped<GetTeamHandler>();
        services.AddScoped<IQueryHandler<GetTeamQuery, TeamDto>, GetTeamHandler>(
            sp => sp.GetRequiredService<GetTeamHandler>());
        services.AddScoped<GetTeamsHandler>();
        services.AddScoped<IQueryHandler<GetTeamsQuery, IReadOnlyList<TeamListItemDto>>, GetTeamsHandler>(
            sp => sp.GetRequiredService<GetTeamsHandler>());
        services.AddScoped<GetTeamMembersHandler>();
        services.AddScoped<IQueryHandler<GetTeamMembersQuery, IReadOnlyList<TeamMemberListItemDto>>, GetTeamMembersHandler>(
            sp => sp.GetRequiredService<GetTeamMembersHandler>());
        services.AddScoped<GetTeamMembershipRoleHandler>();
        services.AddScoped<IQueryHandler<GetTeamMembershipRoleQuery, TeamMembershipRoleDto?>, GetTeamMembershipRoleHandler>(
            sp => sp.GetRequiredService<GetTeamMembershipRoleHandler>());
        services.AddScoped<GetEventCreationRequestHandler>();
        services.AddScoped<IQueryHandler<GetEventCreationRequestQuery, EventCreationRequestDto>, GetEventCreationRequestHandler>(
            sp => sp.GetRequiredService<GetEventCreationRequestHandler>());

        // Domain event handlers
        services.AddScoped<IDomainEventHandler<UserCreatedDomainEvent>, UserCreatedDomainEventHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateTeamHandler>();

        // Facade
        services.AddScoped<OrganizationFacade>();
        services.AddScoped<IOrganizationFacade>(sp =>
        {
            if (builder.Configuration["ORGANIZATION__CACHING__ENABLED"] != "true")
                return sp.GetRequiredService<OrganizationFacade>();

            var inner = sp.GetRequiredService<OrganizationFacade>();
            return new CachingOrganizationFacade(inner);
        });

        return builder;
    }

    public static IHostApplicationBuilder AddOrganizationModuleWorker(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Integration event handlers
        services.AddScoped<IIntegrationEventHandler<TicketedEventCreatedIntegrationEvent>,
            TicketedEventCreatedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketedEventCancelledIntegrationEvent>,
            TicketedEventCancelledIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketedEventCreationRejectedIntegrationEvent>,
            TicketedEventCreationRejectedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketedEventArchivedIntegrationEvent>,
            TicketedEventArchivedIntegrationEventHandler>();

        // Worker-only command handler
        services.AddScoped<RegisterExternalUserHandler>();
        services.AddScoped<ICommandHandler<RegisterExternalUserCommand>, RegisterExternalUserHandler>(
            sp => sp.GetRequiredService<RegisterExternalUserHandler>());

        builder.AddOrganizationJobs();

        return builder;
    }

    public static void AddOrganizationMessageTypes(this MessageTypeRegistryBuilder builder)
    {
        builder.AddIntegrationEvent<TicketedEventCreationRequestedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventCreatedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventCancelledIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventCreationRejectedIntegrationEvent>();
        builder.AddIntegrationEvent<TicketedEventArchivedIntegrationEvent>();
        builder.AddCommand<RegisterExternalUserCommand>();
    }

    private static void AddOrganizationJobs(this IHostApplicationBuilder builder)
    {
        builder.Services.AddQuartz(options =>
        {
            options.AddJob<DeprovisionUserIdpJob>(c => c
                .StoreDurably()
                .WithIdentity(DeprovisionUserIdpJob.Name));

            options.AddTrigger(t => t
                .ForJob(DeprovisionUserIdpJob.Name)
                .WithIdentity($"{DeprovisionUserIdpJob.Name}.trigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInHours(1)
                    .RepeatForever())
                .StartNow());

            options.AddJob<ExpireStaleEventCreationRequestsJob>(c => c
                .StoreDurably()
                .WithIdentity(ExpireStaleEventCreationRequestsJob.Name));

            options.AddTrigger(t => t
                .ForJob(ExpireStaleEventCreationRequestsJob.Name)
                .WithIdentity($"{ExpireStaleEventCreationRequestsJob.Name}.trigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInMinutes(15)
                    .RepeatForever())
                .StartNow());
        });

        builder.Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
    }
}
