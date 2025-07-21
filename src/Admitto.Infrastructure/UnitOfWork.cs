using System.Linq.Expressions;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;

namespace Amolenk.Admitto.Infrastructure;

public class UnitOfWork(ApplicationContext context, MessageOutbox outbox) : IUnitOfWork
{
    private readonly List<Func<ValueTask>> _afterSaveCallbacks = [];

    public void MarkAsModified<TEntity, TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class
    {
        context.Entry(entity).Property(propertyExpression).IsModified = true;
    }

    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await context.SaveChangesAsync(cancellationToken);
        
        // Execute and clear callbacks after changes are saved.
        foreach (var callback in _afterSaveCallbacks)
        {
            await callback();
        }
        _afterSaveCallbacks.Clear();
        
        // Flush the outbox to ensure all messages are sent.
        if (result > 0 && await outbox.FlushAsync(cancellationToken))
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public void RegisterAfterSaveCallback(Func<ValueTask> callback)
    {
        _afterSaveCallbacks.Add(callback);
    }

    // public ValueTask EnqueueCommandAsync(CompleteRegistrationCommand command, bool useOutbox)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async ValueTask EnqueueJobAsync<TJobData>(TJobData jobData, CancellationToken cancellationToken = default)
    //     where TJobData : IJobData
    // {
    //     var job = Job.Create(jobData);
    //     
    //     var existingJob = await context.Jobs.FindAsync([job.Id], cancellationToken);
    //     if (existingJob is not null)
    //     {
    //         context.Jobs.Add(job);
    //     }
    //     else
    //     {
    //         // TODO Log
    //     }
    //
    //     // // Enqueue the job for processing. To be sure, do this even if the job was already found in the database.
    //     // // The JobsWorker will not restart a completed job.
    //     // unitOfWork.RegisterAfterSaveCallback(() => jobsWorker.EnqueueJobAsync(job.Id, cancellationToken));
    // }
}