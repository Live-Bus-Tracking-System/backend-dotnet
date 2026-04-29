using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Infrastructure.Services;
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
        private readonly ISender _sender;
        private readonly IStorageService _storageService;

        public DocumentsController(ISender sender, IStorageService storageService)
        {
            _sender = sender;
            _storageService = storageService;
        }

        /// <summary>
        /// Generates a direct-to-cloud presigned upload URL for the frontend.
        /// </summary>
        [HttpGet("upload-url")]
        public async Task<IActionResult> GetUploadUrl([FromQuery] string contentType, [FromQuery] string extension)
        {
            return Ok(await _sender.Send(new BusTracker.Application.Features.Documents.Queries.GetUploadUrl.GetUploadUrlQuery(contentType, extension)));
        }

        /// <summary>
        /// Generates a time-limited (5-minute) direct-to-cloud presigned URL to view a compliance document.
        /// </summary>
        [HttpGet("{documentId}/view")]
        public async Task<IActionResult> ViewDocument(Guid documentId)
        {
            var presignedUrl = await _sender.Send(new BusTracker.Application.Features.Documents.Queries.GetDocumentViewUrl.GetDocumentViewUrlQuery(documentId));
            return Redirect(presignedUrl);
        }

        /// <summary>
        /// TESTING ONLY: Generates a presigned URL directly from an ObjectKey.
        /// </summary>
        [HttpGet("test-view")]
        [AllowAnonymous]
        public async Task<IActionResult> TestViewDocumentByObjectKey([FromQuery] string objectKey)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                return BadRequest("ObjectKey is required.");

            var presignedUrl = await _storageService.GeneratePresignedDownloadUrlAsync(objectKey);
            return Redirect(presignedUrl);
        }
    }
}
