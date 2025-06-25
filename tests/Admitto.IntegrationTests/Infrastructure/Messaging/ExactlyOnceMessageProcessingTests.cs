using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.IntegrationTests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.IntegrationTests.Infrastructure.Messaging;

[TestClass]
public class ExactlyOnceMessageProcessingTests : BaseForWorkerTests
{
    [TestMethod]
    public async Task HandleCommandAsync_DuplicateMessage_ShouldProcessOnlyOnce()
    {
        // Arrange
        using var scope = WorkerHost.Services.CreateScope();
        var exactlyOnceProcessor = scope.ServiceProvider.GetRequiredService<IExactlyOnceProcessor>();
        var processedMessageContext = scope.ServiceProvider.GetRequiredService<IProcessedMessageContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        var messageId = Guid.NewGuid();
        
        // Act - Process the same message ID twice
        var firstResult = await exactlyOnceProcessor.TryMarkAsProcessedAsync(messageId);
        await unitOfWork.SaveChangesAsync(); // Commit the first message
        
        var secondResult = await exactlyOnceProcessor.TryMarkAsProcessedAsync(messageId);
        
        // Assert
        Assert.IsTrue(firstResult, "First processing attempt should succeed");
        Assert.IsFalse(secondResult, "Second processing attempt should be rejected");
        
        // Verify only one record exists in the database
        var processedMessages = await processedMessageContext.ProcessedMessages
            .Where(pm => pm.MessageId == messageId)
            .ToListAsync();
        Assert.AreEqual(1, processedMessages.Count, "Only one processed message record should exist");
    }

    [TestMethod]
    public async Task Handler_ImplementsExactlyOnceProcessing_ShouldHaveMarkerInterface()
    {
        // Arrange
        using var scope = WorkerHost.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ReserveTicketsCommand>>();
        
        // Act & Assert
        Assert.IsInstanceOfType<IProcessMessagesExactlyOnce>(handler, 
            "ReserveTicketsHandler should implement IProcessMessagesExactlyOnce for exactly-once processing");
    }

    [TestMethod]
    public async Task ExactlyOnceProcessor_IsRegistered_ShouldBeAvailableInContainer()
    {
        // Arrange & Act
        using var scope = WorkerHost.Services.CreateScope();
        var processor = scope.ServiceProvider.GetService<IExactlyOnceProcessor>();
        
        // Assert
        Assert.IsNotNull(processor, "IExactlyOnceProcessor should be registered in the DI container");
    }

    [TestMethod]
    public async Task UnitOfWork_DuplicateProcessedMessage_ShouldThrowProcessedMessageDuplicateException()
    {
        // Arrange
        using var scope = WorkerHost.Services.CreateScope();
        var processedMessageContext = scope.ServiceProvider.GetRequiredService<IProcessedMessageContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        var messageId = Guid.NewGuid();
        
        // Add the first message and commit it
        processedMessageContext.ProcessedMessages.Add(new(messageId));
        await unitOfWork.SaveChangesAsync();
        
        // Act & Assert - Try to add the same message again
        processedMessageContext.ProcessedMessages.Add(new(messageId));
        
        var exception = await Assert.ThrowsExceptionAsync<ProcessedMessageDuplicateException>(
            () => unitOfWork.SaveChangesAsync().AsTask());
            
        Assert.IsNotNull(exception);
    }
}