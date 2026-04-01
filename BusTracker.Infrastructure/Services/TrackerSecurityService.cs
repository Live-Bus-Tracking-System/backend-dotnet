using BusTracker.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace BusTracker.Infrastructure.Services
{
    public class TrackerSecurityService : ITrackerSecurityService
    {
        private readonly string _masterKey;

        public TrackerSecurityService(IConfiguration configuration)
        {
            // In production, this comes from Azure Key Vault or Environment Variables
            _masterKey = configuration["Tracking:MasterKey"]
                         ?? throw new InvalidOperationException("Master tracking key is missing!");
        }

        public bool IsSignatureValid(string trackerId, string rawPayload, string providedSignature)
        {
            try
            {
                // 1. Derive the unique Device Secret: HMAC256(MasterKey, TrackerId)
                using var masterHmac = new HMACSHA256(Encoding.UTF8.GetBytes(_masterKey));
                var deviceKeyBytes = masterHmac.ComputeHash(Encoding.UTF8.GetBytes(trackerId));

                // 2. Hash the raw JSON payload using the derived Device Secret
                using var payloadHmac = new HMACSHA256(deviceKeyBytes);
                var computedHashBytes = payloadHmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload));

                // 3. Convert to Base64 and compare (using fixed-time comparison to prevent timing attacks)
                var computedSignature = Convert.ToBase64String(computedHashBytes);
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedSignature),
                    Encoding.UTF8.GetBytes(providedSignature)
                );
            }
            catch
            {
                return false;
            }
        }
    }
}