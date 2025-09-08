using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Infrastructure.Auth.AdminOverride;

public class AdminOverrideAuthorizationService(
    IAuthorizationService innerAuthorizationService,
    IConfiguration configuration) : IAuthorizationService
{
    private readonly HashSet<Guid> _adminUserIds = (configuration["AdminUserIds"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(Guid.Parse)
        .ToHashSet();

    public ValueTask<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _adminUserIds.Contains(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.IsAdminAsync(userId, cancellationToken);
    }
    
    public ValueTask<bool> CanUpdateTeamAsync(
        Guid userId,
        string teamSlug,
        CancellationToken cancellationToken = default)
    {
        return _adminUserIds.Contains(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanUpdateTeamAsync(userId, teamSlug, cancellationToken);
    }

    public ValueTask<bool> CanViewTeamAsync(
        Guid userId,
        string teamSlug,
        CancellationToken cancellationToken = default)
    {
        return _adminUserIds.Contains(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanViewTeamAsync(userId, teamSlug, cancellationToken);
    }

    public ValueTask<bool> CanCreateEventAsync(
        Guid userId,
        string teamSlug,
        CancellationToken cancellationToken = default)
    {
        return _adminUserIds.Contains(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanCreateEventAsync(userId, teamSlug, cancellationToken);
    }

    public ValueTask<bool> CanUpdateEventAsync(
        Guid userId,
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        return _adminUserIds.Contains(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanUpdateEventAsync(userId, teamSlug, eventSlug, cancellationToken);
    }

    public ValueTask<bool> CanViewEventAsync(
        Guid userId,
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        return _adminUserIds.Contains(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanViewEventAsync(userId, teamSlug, eventSlug, cancellationToken);
    }

    public ValueTask AddTicketedEventAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default) =>
        innerAuthorizationService.AddTicketedEventAsync(teamSlug, eventSlug, cancellationToken);

    public ValueTask AddTeamRoleAsync(
        Guid userId,
        string teamSlug,
        TeamMemberRole role,
        CancellationToken cancellationToken = default) =>
        innerAuthorizationService.AddTeamRoleAsync(userId, teamSlug, role, cancellationToken);

    public ValueTask<IEnumerable<string>> GetTeamsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        innerAuthorizationService.GetTeamsAsync(userId, cancellationToken);

    public ValueTask<IEnumerable<string>> GetTicketedEventsAsync(
        Guid userId,
        string teamSlug,
        CancellationToken cancellationToken = default) =>
        innerAuthorizationService.GetTicketedEventsAsync(userId, teamSlug, cancellationToken);
}