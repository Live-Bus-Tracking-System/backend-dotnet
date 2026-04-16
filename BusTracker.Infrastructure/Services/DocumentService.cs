using BusTracker.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BusTracker.Infrastructure.Services
{
    /// Implements secure document URL handling using AES-256-CBC encryption at rest
    /// and short-lived HMAC-signed JWT proxy tokens for time-limited document access.
    ///
    /// Required configuration keys:
    ///   Documents:EncryptionKey  — 32-character string (AES-256)
    ///   Documents:SigningSecret  — 32+ character string (JWT HMAC-SHA256)
    ///   Documents:AccessTokenExpiryMinutes — int (default: 5)
    public class DocumentService : IDocumentService
    {
        private readonly IConfiguration _config;

        public DocumentService(IConfiguration config)
        {
            _config = config;
        }

        // ── AES-256 Encryption ────────────────────────────────────────────────────

        public string EncryptUrl(string rawUrl)
        {
            var key = GetEncryptionKey();

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV(); // Fresh IV per encryption for semantic security

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(rawUrl);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV to ciphertext so it can recover it during decryption
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string DecryptUrl(string encryptedUrl)
        {
            var key = GetEncryptionKey();
            var allBytes = Convert.FromBase64String(encryptedUrl);

            using var aes = Aes.Create();
            aes.Key = key;

            // IV is the first 16 bytes
            var iv = new byte[16];
            var cipherBytes = new byte[allBytes.Length - 16];
            Buffer.BlockCopy(allBytes, 0, iv, 0, 16);
            Buffer.BlockCopy(allBytes, 16, cipherBytes, 0, cipherBytes.Length);

            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        // ── JWT Proxy Access Tokens ───────────────────────────────────────────────

        public string GenerateAccessToken(Guid documentId, string requestorUserId, TimeSpan? expiresIn = null)
        {
            var secret = GetSigningSecret();
            var expiry = expiresIn ?? TimeSpan.FromMinutes(
                int.TryParse(_config["Documents:AccessTokenExpiryMinutes"], out var mins) ? mins : 5);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("doc_id",   documentId.ToString()),
                new Claim("req_by",   requestorUserId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer:   "bustracker-docs",
                audience: "bustracker-docs",
                claims:   claims,
                expires:  DateTime.UtcNow.Add(expiry),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DocumentAccessResult ValidateAccessToken(string token)
        {
            try
            {
                var secret = GetSigningSecret();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidateIssuer           = true,
                    ValidIssuer              = "bustracker-docs",
                    ValidateAudience         = true,
                    ValidAudience            = "bustracker-docs",
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero,
                }, out _);

                var docIdClaim = principal.FindFirst("doc_id")?.Value;
                if (!Guid.TryParse(docIdClaim, out var documentId))
                    return new(false, Guid.Empty, null, "Invalid document ID in token.");

                return new(true, documentId, null, null);
            }
            catch (Exception ex)
            {
                return new(false, Guid.Empty, null, ex.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private byte[] GetEncryptionKey()
        {
            var keyStr = _config["Documents:EncryptionKey"]
                ?? throw new InvalidOperationException("Documents:EncryptionKey is not configured.");

            // Pad or truncate to exactly 32 bytes for AES-256
            var keyBytes = new byte[32];
            var src = Encoding.UTF8.GetBytes(keyStr);
            Buffer.BlockCopy(src, 0, keyBytes, 0, Math.Min(src.Length, 32));
            return keyBytes;
        }

        private string GetSigningSecret()
        {
            return _config["Documents:SigningSecret"]
                ?? throw new InvalidOperationException("Documents:SigningSecret is not configured.");
        }
    }
}
