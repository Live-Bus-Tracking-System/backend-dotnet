using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Documents.Queries.GetDocumentViewUrl
{
    public class GetDocumentViewUrlQueryHandler : IRequestHandler<GetDocumentViewUrlQuery, string>
    {
        private readonly IApplicationDbContext _db;
        private readonly IDocumentService _documentService;
        private readonly ICurrentUserService _currentUser;
        private readonly IStorageService _storageService;

        public GetDocumentViewUrlQueryHandler(
            IApplicationDbContext db,
            IDocumentService documentService,
            ICurrentUserService currentUser,
            IStorageService storageService)
        {
            _db = db;
            _documentService = documentService;
            _currentUser = currentUser;
            _storageService = storageService;
        }

        public async Task<string> Handle(GetDocumentViewUrlQuery request, CancellationToken cancellationToken)
        {
            var doc = await _db.ComplianceDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId && !d.IsDeleted, cancellationToken);

            if (doc is null)
                throw new NotFoundException(nameof(doc), request.DocumentId);

            // Authorization: SuperAdmin or Member of Owning Organization
            if (!_currentUser.IsSuperAdmin)
            {
                var vehicle = await _db.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == doc.EntityId && !v.IsDeleted, cancellationToken);

                if (vehicle is null || vehicle.OrganizationId != _currentUser.OrganisationId)
                    throw new ForbiddenException("You do not have permission to view this document.");
            }

            var decryptedUrl = _documentService.DecryptUrl(doc.DocumentUrl);
            
            // Extract the ObjectKey from the full URL.
            // e.g., "https://s3.../bucket/b8a3...pdf" -> "b8a3...pdf"
            var objectKey = decryptedUrl.Split('/').Last();

            if (string.IsNullOrWhiteSpace(objectKey))
                throw new Exception("Stored document URL is invalid or malformed.");

            var presignedUrl = await _storageService.GeneratePresignedDownloadUrlAsync(objectKey);

            return presignedUrl;
        }
    }
}
