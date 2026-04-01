using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Domain.Entities;
using BusTracker.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace BusTracker.Infrastructure.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _dbContext;

        public EventService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task EmitAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event is null) return;

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOnUtc = DateTime.UtcNow,
                Type = @event.GetType().Name,
                Content = JsonConvert.SerializeObject(
                    @event,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    })
            };

            await _dbContext.Set<OutboxMessage>().AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
