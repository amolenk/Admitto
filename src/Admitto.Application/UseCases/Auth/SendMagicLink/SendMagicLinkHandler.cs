namespace Amolenk.Admitto.Application.UseCases.Auth.SendMagicLink;

/// <summary>
/// Start passwordless login by generating an authorization code and sending a magic link.
/// </summary>
public class SendMagicLinkHandler(IReadModelContext readModelContext, IAuthContext authContext, IEmailOutbox emailOutbox)
    : ICommandHandler<SendMagicLinkCommand>
{
    public async ValueTask HandleAsync(SendMagicLinkCommand command, CancellationToken cancellationToken)
    {
        // Find the team member by email.
        var teamMember = await readModelContext.TeamMembers
            .FirstOrDefaultAsync(m => m.UserEmail == command.Email, cancellationToken);
        if (teamMember is null)
        {
            // TODO
            throw new Exception("User not found.");
        }
        
        // Generate a new authorization code.
        var authorizationCode = new AuthorizationCode
        {
            Code = Guid.NewGuid(),
            UserId = teamMember.UserId,
            CodeChallenge = command.CodeChallenge, // The code challenge is only a hash of the code verifier
            Expires = DateTime.UtcNow.AddMinutes(5)
        };
        authContext.AuthorizationCodes.Add(authorizationCode);
        
        // Create an e-mail for the magic link.
        emailOutbox.EnqueueEmail(
            teamMember.UserEmail,
            "magic-link", 
            new Dictionary<string, string>
            {
                { "AuthorizationCode", authorizationCode.Code.ToString() }
            },
            true);
    }
}
