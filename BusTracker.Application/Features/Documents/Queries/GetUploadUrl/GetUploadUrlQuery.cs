using BusTracker.Application.Common.Interfaces.Services;
using MediatR;

namespace BusTracker.Application.Features.Documents.Queries.GetUploadUrl
{
    public record GetUploadUrlQuery(string ContentType, string Extension) : IRequest<PresignedUploadResult>;
}
