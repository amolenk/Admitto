using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Infrastructure.Auth.AdminOverride;

public class AdminOverrideAuthorizationService(
    IAuthorizationService innerAuthorizationService,
    IApplicationContext context,
    IConfiguration configuration) : IAuthorizationService
{
    private readonly HashSet<Guid> _adminUserIds = (configuration["AdminUserIds"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(Guid.Parse)
        .ToHashSet();

    public ValueTask<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return IsAdmin(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.IsAdminAsync(userId, cancellationToken);
    }

    public ValueTask<bool> CanUpdateTeamAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return IsAdmin(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanUpdateTeamAsync(userId, teamId, cancellationToken);
    }

    public ValueTask<bool> CanViewTeamAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return IsAdmin(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanViewTeamAsync(userId, teamId, cancellationToken);
    }

    public ValueTask<bool> CanCreateEventAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return IsAdmin(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanCreateEventAsync(userId, teamId, cancellationToken);
    }

    public ValueTask<bool> CanUpdateEventAsync(
        Guid userId,
        Guid teamId,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        return IsAdmin(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanUpdateEventAsync(userId, teamId, ticketedEventId, cancellationToken);
    }

    public ValueTask<bool> CanViewEventAsync(
        Guid userId,
        Guid teamId,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        return IsAdmin(userId)
            ? ValueTask.FromResult(true)
            : innerAuthorizationService.CanViewEventAsync(userId, teamId, ticketedEventId, cancellationToken);
    }

    public ValueTask AddTicketedEventAsync(
        Guid teamId,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default) =>
        innerAuthorizationService.AddTicketedEventAsync(teamId, ticketedEventId, cancellationToken);

    public ValueTask AddTeamRoleAsync(
        Guid userId,
        Guid teamId,
        TeamMemberRole role,
        CancellationToken cancellationToken = default) =>
        innerAuthorizationService.AddTeamRoleAsync(userId, teamId, role, cancellationToken);

    public async ValueTask<IEnumerable<Guid>> GetTeamsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (IsAdmin(userId))
        {
            return await context.Teams
                .AsNoTracking()
                .Select(team => team.Id)
                .ToListAsync(cancellationToken);
        }

        return await innerAuthorizationService.GetTeamsAsync(userId, cancellationToken);
    }

    public async ValueTask<IEnumerable<Guid>> GetTicketedEventsAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        if (IsAdmin(userId))
        {
            return await context.TicketedEvents
                .AsNoTracking()
                .Where(e => e.TeamId == teamId)
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);
        }

        return await innerAuthorizationService.GetTicketedEventsAsync(userId, teamId, cancellationToken);
    }

    private bool IsAdmin(Guid userId) => _adminUserIds.Contains(userId);
}