using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BusTracker.Infrastructure.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _config;

        public JwtTokenGenerator(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateAccessToken(UserAuthDto user)
        {
            var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing.");
            var expiryStr = _config["Jwt:ExpiryMinutes"] ?? throw new InvalidOperationException("Jwt:ExpiryMinutes is missing.");
            if (!int.TryParse(expiryStr, out var expiryMins)) expiryMins = 15;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(JwtRegisteredClaimNames.PhoneNumber, user.Phone)
            };

            // Vital for instantly invalidating tokens post-password reset
            if (!string.IsNullOrEmpty(user.SecurityStamp))
            {
                claims.Add(new Claim("AspNet.Identity.SecurityStamp", user.SecurityStamp));
            }

            if (!string.IsNullOrEmpty(user.OrganizationId))
            {
                claims.Add(new Claim("orgId", user.OrganizationId));
            }
            if (!string.IsNullOrEmpty(user.OrganizationType))
            {
                claims.Add(new Claim("orgType", user.OrganizationType));
            }

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim("role", role));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMins),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string RawToken, string TokenHash) GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            var rawToken = Convert.ToBase64String(randomNumber);

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
            var tokenHash = Convert.ToBase64String(hashBytes);

            return (rawToken, tokenHash);
        }

        public string HashRefreshToken(string rawToken)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
