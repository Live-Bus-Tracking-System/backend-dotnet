using BusTracker.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;

namespace BusTracker.Infrastructure.BackgroundJobs
{
    [DisallowConcurrentExecution]
    public class ProcessOutboxMessagesJob : IJob
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPublisher _publisher;
        private readonly ILogger<ProcessOutboxMessagesJob> _logger;

        public ProcessOutboxMessagesJob(
            ApplicationDbContext dbContext,
            IPublisher publisher,
            ILogger<ProcessOutboxMessagesJob> logger)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var messages = await _dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount < 3)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(20)
                .ToListAsync(context.CancellationToken);

            if (!messages.Any()) return;

            foreach (var message in messages)
            {
                try
                {
                    // Deserializing dynamically using TypeNameHandling
                    var domainEvent = JsonConvert.DeserializeObject<INotification>(
                        message.Content,
                        new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.All
                        });

                    if (domainEvent is null)
                    {
                        throw new Exception($"Failed to deserialize outbox message {message.Id} to INotification.");
                    }

                    await _publisher.Publish(domainEvent, context.CancellationToken);

                    message.ProcessedOnUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                    message.Error = ex.Message;
                    message.RetryCount++;
                }
            }

            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}
