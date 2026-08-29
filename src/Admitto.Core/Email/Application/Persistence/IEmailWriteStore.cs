using Amolenk.Admitto.Core.Email.Domain.Entities;

namespace Amolenk.Admitto.Core.Email.Application.Persistence;

public interface IEmailWriteStore
{
    DbSet<EmailLog> EmailLog { get; }
    DbSet<ReconfirmationBatch> ReconfirmationBatches { get; }
    DbSet<ReconfirmPolicyCloseEvaluation> ReconfirmPolicyCloseEvaluations { get; }
}
