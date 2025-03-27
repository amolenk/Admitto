using System.Reflection;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.ReadModel.Views;
using Amolenk.Admitto.Application.UseCases.Auth;
using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class ApplicationContext(DbContextOptions options) : DbContext(options), IDomainContext, IReadModelContext,
    IAuthContext, IEmailContext, IEmailOutbox, IMessageOutbox, IUnitOfWork
{
    // IDomainContext sets
    public DbSet<AttendeeRegistration> AttendeeRegistrations { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<TicketedEvent> TicketedEvents { get; set; } = null!;
    
    // IReadModelContext sets
    public DbSet<AttendeeActivityView> AttendeeActivities { get; set; } = null!;
    public DbSet<TeamMembersView> TeamMembers { get; set; } = null!;
    
    // IAuthContext sets
    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    
    // IEmailContext sets
    public DbSet<EmailMessage> EmailMessages { get; set; } = null!;
    
    // Other sets
    public DbSet<OutboxMessage> Outbox { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public void EnqueueEmail(string recipientEmail, string templateId, Dictionary<string, string> templateParameters, 
        bool priority)
    {
        var email = new EmailMessage
        {
            RecipientEmail = recipientEmail,
            TemplateId = templateId,
            TemplateParameters = [..templateParameters.Select(kv => new EmailTemplateParameter(kv.Key, kv.Value))],
            Priority = priority
        };
        
        EmailMessages.Add(email);
        
        // Add a command to the outbox to deliver the e-mail to the user.
        EnqueueCommand(new SendEmailCommand(email.Id), priority);
    }

    public void EnqueueCommand(ICommand command, bool priority)
    {
        Outbox.Add(OutboxMessage.FromCommand(command, priority));
    }
}
