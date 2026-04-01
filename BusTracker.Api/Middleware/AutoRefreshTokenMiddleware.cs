using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Repository;
using BusTracker.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace BusTracker.Api.Middleware
{
    public class AutoRefreshTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public AutoRefreshTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthRepository authRepository, IIdentityService identityService, IJwtTokenGenerator jwtGenerator)
        {
            var tokenStr = context.Request.Cookies["access_token"];
            var refreshTokenStr = context.Request.Cookies["refresh_token"];

            if (string.IsNullOrEmpty(tokenStr) && string.IsNullOrEmpty(refreshTokenStr))
            {
                await _next(context);
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            
            bool needsRefresh = false;

            if (!string.IsNullOrEmpty(tokenStr) && tokenHandler.CanReadToken(tokenStr))
            {
                var jwt = tokenHandler.ReadJwtToken(tokenStr);
                if (jwt.ValidTo <= DateTime.UtcNow)
                {
                    needsRefresh = true;
                }
            }
            else if (string.IsNullOrEmpty(tokenStr) && !string.IsNullOrEmpty(refreshTokenStr))
            {
                needsRefresh = true;
            }

            if (needsRefresh && !string.IsNullOrEmpty(refreshTokenStr))
            {
                var hash = jwtGenerator.HashRefreshToken(refreshTokenStr);
                var activeToken = await authRepository.GetActiveRefreshTokenAsync(hash, context.RequestAborted);

                if (activeToken != null)
                {
                    // Rotate
                    activeToken.IsRevoked = true;
                    activeToken.RevokedAtUtc = DateTime.UtcNow;

                    var user = await identityService.GetUserByIdAsync(activeToken.UserId);
                    var newAccessToken = jwtGenerator.GenerateAccessToken(user);
                    var (newRawRefreshToken, newHash) = jwtGenerator.GenerateRefreshToken();

                    var newEntity = new RefreshToken
                    {
                        UserId = user.Id,
                        TokenHash = newHash,
                        ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.Request.Headers.UserAgent.ToString(),
                        ReplacedByTokenHash = newHash
                    };

                    activeToken.ReplacedByTokenHash = newHash;

                    await authRepository.UpdateRefreshTokenAsync(activeToken, context.RequestAborted);
                    await authRepository.SaveRefreshTokenAsync(newEntity, context.RequestAborted);

                    // Add new tokens to the response
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    };

                    context.Response.Cookies.Append("access_token", newAccessToken, cookieOptions);
                    context.Response.Cookies.Append("refresh_token", newRawRefreshToken, cookieOptions);

                    // Re-inject the new token into the current request so [Authorize] works downstream seamlessly
                    context.Request.Headers["Authorization"] = $"Bearer {newAccessToken}";
                    // Also forcibly update the cookie value in the incoming request collection for JwtBearer middleware
                    context.Request.Cookies = new CookieCollectionWrapper(context.Request.Cookies, "access_token", newAccessToken);
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Since IRequestCookieCollection is read-only, we created a quick wrapper to overwrite the expired token 
    /// with the newly generated token mid-flight before the standard Auth middleware runs.
    /// </summary>
    internal class CookieCollectionWrapper : IRequestCookieCollection
    {
        private readonly IRequestCookieCollection _inner;
        private readonly string _overrideKey;
        private readonly string _overrideValue;

        public CookieCollectionWrapper(IRequestCookieCollection inner, string overrideKey, string overrideValue)
        {
            _inner = inner;
            _overrideKey = overrideKey;
            _overrideValue = overrideValue;
        }

        public string? this[string key] => key == _overrideKey ? _overrideValue : _inner[key];
        public int Count => _inner.Count;
        public ICollection<string> Keys => _inner.Keys.Contains(_overrideKey) ? _inner.Keys : _inner.Keys.Append(_overrideKey).ToList();
        public bool ContainsKey(string key) => key == _overrideKey || _inner.ContainsKey(key);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var kvp in _inner)
            {
                if (kvp.Key == _overrideKey) yield return new KeyValuePair<string, string>(_overrideKey, _overrideValue);
                else yield return kvp;
            }
            if (!_inner.ContainsKey(_overrideKey)) yield return new KeyValuePair<string, string>(_overrideKey, _overrideValue);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public bool TryGetValue(string key, out string? value)
        {
            if (key == _overrideKey)
            {
                value = _overrideValue;
                return true;
            }
            return _inner.TryGetValue(key, out value);
        }
    }
}
