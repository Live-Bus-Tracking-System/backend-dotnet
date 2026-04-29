using Amazon.S3;
using Amazon.S3.Model;
using BusTracker.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace BusTracker.Infrastructure.Services
{
    public class B2StorageService : IStorageService
    {
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public B2StorageService(IConfiguration config)
        {
            _config = config;
            _bucketName = _config["Storage:BucketName"] ?? throw new InvalidOperationException("Storage:BucketName is required.");

            var keyId = _config["Storage:B2KeyId"] ?? throw new InvalidOperationException("Storage:B2KeyId is required.");
            var appKey = _config["Storage:B2ApplicationKey"] ?? throw new InvalidOperationException("Storage:B2ApplicationKey is required.");
            var endpoint = _config["Storage:B2Endpoint"] ?? throw new InvalidOperationException("Storage:B2Endpoint is required.");

            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"https://{endpoint}",
                AuthenticationRegion = endpoint.Split('.')[1] // Extract region, e.g., us-west-004
            };

            _s3Client = new AmazonS3Client(keyId, appKey, s3Config);
        }

        public Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(string contentType, string extension)
        {
            var objectKey = $"{Guid.NewGuid()}{extension}";

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(5),
                ContentType = contentType
            };

            string url = _s3Client.GetPreSignedURL(request);

            return Task.FromResult(new PresignedUploadResult
            {
                UploadUrl = url,
                ObjectKey = objectKey
            });
        }

        public Task<string> GeneratePresignedDownloadUrlAsync(string objectKey)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(5)
            };

            string url = _s3Client.GetPreSignedURL(request);

            return Task.FromResult(url);
        }
    }
}
