using BusTracker.Application.Common.Interfaces.Services;
using MediatR;

namespace BusTracker.Application.Features.Documents.Queries.GetUploadUrl
{
    public class GetUploadUrlQueryHandler : IRequestHandler<GetUploadUrlQuery, PresignedUploadResult>
    {
        private readonly IStorageService _storageService;

        public GetUploadUrlQueryHandler(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<PresignedUploadResult> Handle(GetUploadUrlQuery request, CancellationToken cancellationToken)
        {
            var extension = request.Extension;
            if (!extension.StartsWith("."))
                extension = "." + extension;

            return await _storageService.GeneratePresignedUploadUrlAsync(request.ContentType, extension);
        }
    }
}
