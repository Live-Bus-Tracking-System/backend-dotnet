using BusTracker.Domain.Common;
using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BusTracker.Infrastructure.Persistence.Interceptors
{
    public sealed class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            DbContext? dbContext = eventData.Context;

            if (dbContext is null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            var outboxMessages = dbContext.ChangeTracker
                .Entries<BaseEntity>()
                .Select(x => x.Entity)
                .SelectMany(entity =>
                {
                    var domainEvents = entity.DomainEvents.ToList();
                    entity.ClearDomainEvents();
                    return domainEvents;
                })
                .Select(domainEvent => new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    OccurredOnUtc = DateTime.UtcNow,
                    Type = domainEvent.GetType().Name,
                    Content = JsonConvert.SerializeObject(
                        domainEvent,
                        new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.All
                        })
                })
                .ToList();

            dbContext.Set<OutboxMessage>().AddRange(outboxMessages);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
