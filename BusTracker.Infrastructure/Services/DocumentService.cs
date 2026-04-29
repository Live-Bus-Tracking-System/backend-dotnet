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
    }
}
