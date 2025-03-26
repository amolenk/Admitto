using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.ReadModel.Views;

public class TeamMembersView
{
    public Guid TeamId { get; init; }

    public Guid UserId { get; init; }

    public string UserEmail { get; init; } = null!;
    
    public UserRole Role { get; init; } 
}