using MediatR;

namespace BusTracker.Application.Features.Documents.Queries.GetDocumentViewUrl
{
    public record GetDocumentViewUrlQuery(Guid DocumentId) : IRequest<string>;
}
