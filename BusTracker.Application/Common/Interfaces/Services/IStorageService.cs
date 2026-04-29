namespace BusTracker.Application.Common.Interfaces.Services
{
    public class PresignedUploadResult
    {
        public string UploadUrl { get; set; } = string.Empty;
        public string ObjectKey { get; set; } = string.Empty;
    }

    public interface IStorageService
    {
        /// <summary>
        /// Generates a time-limited presigned URL for direct-to-cloud file uploads.
        /// </summary>
        /// <param name="contentType">The MIME type of the file (e.g., application/pdf)</param>
        /// <param name="extension">The file extension (e.g., .pdf)</param>
        /// <returns>A securely generated URL and its unique object key.</returns>
        Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(string contentType, string extension);

        /// <summary>
        /// Generates a time-limited presigned URL for direct-to-cloud file downloads (viewing).
        /// </summary>
        /// <param name="objectKey">The unique object key of the file in the bucket.</param>
        /// <returns>A securely generated GET URL.</returns>
        Task<string> GeneratePresignedDownloadUrlAsync(string objectKey);
    }
}
