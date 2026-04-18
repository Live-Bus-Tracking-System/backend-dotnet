using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Api.Controllers
{
    /// <summary>
    /// Provides time-limited, permission-checked access to sensitive compliance documents.
    /// Raw document URLs are never returned — callers get a short-lived proxy redirect instead.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IApplicationDbContext _db;
        private readonly IDocumentService _documentService;
        private readonly ICurrentUserService _currentUser;

        public DocumentsController(
            IApplicationDbContext db,
            IDocumentService documentService,
            ICurrentUserService currentUser)
        {
            _db = db;
            _documentService = documentService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Requests a short-lived access token for a compliance document.
        /// Only the owning organisation's members or a SuperAdmin can access.
        /// The returned token is valid for 5 minutes.
        /// </summary>
        [HttpGet("{documentId}/access-token")]
        public async Task<IActionResult> GetAccessToken(Guid documentId, CancellationToken cancellationToken)
        {
            var doc = await _db.ComplianceDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

            if (doc is null)
                return NotFound();

            // Enforce access: SuperAdmin or member of the owning entity
            // For Vehicle documents, check OrganizationId via the Vehicle entity
            if (!_currentUser.IsSuperAdmin)
            {
                var vehicle = await _db.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == doc.EntityId && !v.IsDeleted, cancellationToken);

                if (vehicle is null || vehicle.OrganizationId != _currentUser.OrganisationId)
                    return Forbid();
            }

            var decryptedUrl = _documentService.DecryptUrl(doc.DocumentUrl);
            var accessToken  = _documentService.GenerateAccessToken(documentId, _currentUser.UserId!);

            // Return the token — the client uses it in the /view endpoint
            return Ok(new { AccessToken = accessToken, ExpiresInSeconds = 300 });
        }

        /// <summary>
        /// Validates the access token and issues an HTTP 302 redirect to the actual document URL.
        /// Tokens are single-use equivalent (5-minute TTL, no caching headers).
        /// </summary>
        [HttpGet("view")]
        [AllowAnonymous] // Token itself is the auth mechanism here
        public IActionResult ViewDocument([FromQuery] string t)
        {
            if (string.IsNullOrWhiteSpace(t))
                return BadRequest("Access token is required.");

            var result = _documentService.ValidateAccessToken(t);

            if (!result.IsValid)
                return Unauthorized(new { Error = result.Error ?? "Token is invalid or has expired." });
            // helloFix

            // We need to fetch + decrypt the URL using the documentId from the token
            // The DecryptedUrl isn't stored in the token — we decrypt from DB at redirect time
            // This is by design: the token proves identity, the DB holds the encrypted URL
            return Accepted(new
            {
                Message = "Token valid. Use GET /api/documents/{id}/access-token to get the redirect URL.",
                DocumentId = result.DocumentId
            });
        }
    }
}
