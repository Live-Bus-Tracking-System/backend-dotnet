using BusTracker.Domain.Common;
using MediatR;

namespace BusTracker.Application.Common.Events
{
    /// <summary>
    /// An Application-layer envelope that wraps a pure Domain Event so it can be
    /// published through MediatR's IPublisher pipeline without the Domain layer
    /// having any dependency on MediatR (Clean Architecture compliant).
    /// </summary>
    public sealed class DomainEventNotification<TDomainEvent> : INotification
        where TDomainEvent : IDomainEvent
    {
        public TDomainEvent DomainEvent { get; }

        public DomainEventNotification(TDomainEvent domainEvent)
        {
            DomainEvent = domainEvent;
        }
    }
}
