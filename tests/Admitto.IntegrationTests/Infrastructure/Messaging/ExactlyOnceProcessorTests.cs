using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Amolenk.Admitto.IntegrationTests.Infrastructure.Messaging;

[TestClass]
public class ExactlyOnceProcessorTests
{
    private DbContext _context = null!;
    private IProcessedMessageContext _processedMessageContext = null!;
    private ExactlyOnceProcessor _processor = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _processedMessageContext = (IProcessedMessageContext)_context;
        
        var logger = new NullLogger<ExactlyOnceProcessor>();
        _processor = new ExactlyOnceProcessor(_processedMessageContext, logger);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    public async Task TryMarkAsProcessedAsync_NewMessage_ShouldReturnTrue()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        var result = await _processor.TryMarkAsProcessedAsync(messageId);

        // Assert
        Assert.IsTrue(result);
        
        // Verify the message was added to context (but not yet saved)
        var processedMessage = _processedMessageContext.ProcessedMessages.Local
            .FirstOrDefault(pm => pm.MessageId == messageId);
        Assert.IsNotNull(processedMessage);
        Assert.AreEqual(messageId, processedMessage.MessageId);
    }

    [TestMethod]
    public async Task TryMarkAsProcessedAsync_DuplicateMessage_ShouldReturnFalse()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        
        // Add and save the first message
        await _processor.TryMarkAsProcessedAsync(messageId);
        await _context.SaveChangesAsync();

        // Act - Second processing attempt
        var result = await _processor.TryMarkAsProcessedAsync(messageId);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task TryMarkAsProcessedAsync_MultipleMessages_ShouldTrackAllUnique()
    {
        // Arrange
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();

        // Act
        var result1 = await _processor.TryMarkAsProcessedAsync(messageId1);
        var result2 = await _processor.TryMarkAsProcessedAsync(messageId2);

        // Assert
        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
        
        // Verify both messages were added to context
        var localMessages = _processedMessageContext.ProcessedMessages.Local.ToList();
        Assert.AreEqual(2, localMessages.Count);
        Assert.IsTrue(localMessages.Any(pm => pm.MessageId == messageId1));
        Assert.IsTrue(localMessages.Any(pm => pm.MessageId == messageId2));
    }
}

// Test DbContext that includes ProcessedMessage
public class TestDbContext : DbContext, IProcessedMessageContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<ProcessedMessage> ProcessedMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure ProcessedMessage entity for in-memory testing
        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.ProcessedAt).IsRequired();
        });
    }
}