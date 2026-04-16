using BusTracker.Application.Common.Events;
using BusTracker.Domain.Common;
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
                    // Deserialize to the concrete type using $type metadata embedded by TypeNameHandling.All
                    var deserialized = JsonConvert.DeserializeObject<object>(
                        message.Content,
                        new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                    if (deserialized is null)
                        throw new InvalidOperationException($"Failed to deserialize outbox message {message.Id}.");

                    INotification notification;

                    if (deserialized is INotification appEvent)
                    {
                        // Auth events (LoginEvent, RegisterEvent, etc.) already implement INotification directly.
                        notification = appEvent;
                    }
                    else if (deserialized is IDomainEvent domainEvent)
                    {
                        // Pure Domain Events: wrap them in the Application-layer envelope so
                        // MediatR can route them without Domain knowing about MediatR.
                        var wrapperType = typeof(DomainEventNotification<>)
                            .MakeGenericType(domainEvent.GetType());

                        notification = (INotification)Activator.CreateInstance(wrapperType, domainEvent)!;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Outbox message {message.Id} (type: {message.Type}) is neither INotification nor IDomainEvent.");
                    }

                    await _publisher.Publish(notification, context.CancellationToken);

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
