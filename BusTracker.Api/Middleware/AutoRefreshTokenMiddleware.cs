using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Repository;
using BusTracker.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace BusTracker.Api.Middleware
{
    public class AutoRefreshTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;
        private readonly ILogger<AutoRefreshTokenMiddleware> _logger;

        // ── Skip-list — avoids unnecessary JWT reads and DB calls on non-API paths.
        private static readonly HashSet<string> _skipPaths =
            new(StringComparer.OrdinalIgnoreCase) { "/health", "/metrics", "/favicon.ico" };

        public AutoRefreshTokenMiddleware(
            RequestDelegate next,
            IConfiguration config,
            ILogger<AutoRefreshTokenMiddleware> logger)
        {
            _next = next;
            _config = config;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IAuthRepository authRepository,
            IIdentityService identityService,
            IJwtTokenGenerator jwtGenerator)
        {
            // ── Skip non-API paths ────────────────────────────────────────
            if (_skipPaths.Contains(context.Request.Path.Value ?? "") ||
                context.Request.Path.StartsWithSegments("/hubs"))
            {
                await _next(context);
                return;
            }

            var tokenStr = context.Request.Cookies["access_token"];
            var refreshTokenStr = context.Request.Cookies["refresh_token"];

            if (string.IsNullOrEmpty(tokenStr) && string.IsNullOrEmpty(refreshTokenStr))
            {
                await _next(context);
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            bool needsRefresh = false;

            // ── Proactive refresh window ──────────────────────────────────
            var earlyRefreshMinsStr = _config["Jwt:EarlyRefreshThresholdMinutes"] ?? throw new InvalidOperationException("Jwt:EarlyRefreshThresholdMinutes is missing.");
            if (!int.TryParse(earlyRefreshMinsStr, out var earlyRefreshMins)) earlyRefreshMins = 2;
            var expiryThreshold = TimeSpan.FromMinutes(earlyRefreshMins);

            if (!string.IsNullOrEmpty(tokenStr) && tokenHandler.CanReadToken(tokenStr))
            {
                var jwt = tokenHandler.ReadJwtToken(tokenStr);
                if (jwt.ValidTo <= DateTime.UtcNow.Add(expiryThreshold))
                    needsRefresh = true;
            }
            else if (string.IsNullOrEmpty(tokenStr) && !string.IsNullOrEmpty(refreshTokenStr))
            {
                needsRefresh = true;
            }

            if (needsRefresh && !string.IsNullOrEmpty(refreshTokenStr))
            {
                var hash = jwtGenerator.HashRefreshToken(refreshTokenStr);

                var storedToken = await authRepository.GetRefreshTokenByHashAsync(hash, context.RequestAborted);

                // ── Reuse attack detection ────────────────────────────────
                if (storedToken is not null && storedToken.IsRevoked)
                {
                    var allUserTokens = await authRepository.GetAllTokensForUserAsync(
                        storedToken.UserId, context.RequestAborted);

                    var activeTokens = allUserTokens.Where(t => !t.IsRevoked).ToList();
                    if (activeTokens.Count > 0)
                        await authRepository.RevokeTokensAsync(activeTokens, context.RequestAborted);

                    // TODO: Replace with a proper security audit service
                    // e.g., await _securityAuditService.LogTokenReuseAsync(storedToken.UserId, ip, userAgent, context.RequestAborted);
                    Console.WriteLine(
                        $"[SECURITY] Refresh token reuse detected. " +
                        $"UserId={storedToken.UserId} IP={context.Connection.RemoteIpAddress} " +
                        $"At={DateTime.UtcNow:O}");

                    ClearAuthCookies(context);
                    await _next(context); // let [Authorize] return the 401
                    return;
                }

                // Token not found in DB at all, or found but expired (not revoked)
                if (storedToken is null || !storedToken.IsActive)
                {
                    ClearAuthCookies(context);
                    await _next(context);
                    return;
                }

                // ── Normal rotation path ───────────────────────────────────────────
                var currentIp = context.Connection.RemoteIpAddress?.ToString();
                var currentUserAgent = context.Request.Headers.UserAgent.ToString();

                // ── IP/UserAgent mismatch — warn-only log ─────────────────
                if (storedToken.IpAddress is not null && storedToken.IpAddress != currentIp)
                {
                    // TODO: Implement geo-based or range-based IP comparison for smarter detection
                    // TODO: Consider escalating to reuse detection if IP range is completely foreign
                    _logger.LogWarning(
                        "[SECURITY-WARN] Refresh token IP mismatch. " +
                        "UserId={UserId} StoredIp={StoredIp} CurrentIp={CurrentIp}",
                        storedToken.UserId, storedToken.IpAddress, currentIp);
                }

                if (storedToken.UserAgent is not null && storedToken.UserAgent != currentUserAgent)
                {
                    // TODO: Implement device fingerprint / User-Agent fuzzy matching
                    // TODO: Consider flagging if browser family changes (e.g., Chrome → Firefox)
                    _logger.LogWarning(
                        "[SECURITY-WARN] Refresh token UserAgent mismatch. UserId={UserId}",
                        storedToken.UserId);
                }

                var user = await identityService.GetUserByIdAsync(storedToken.UserId);

                if (storedToken.SecurityStamp != user.SecurityStamp)
                {
                    storedToken.IsRevoked    = true;
                    storedToken.RevokedAtUtc = DateTime.UtcNow;
                    await authRepository.UpdateRefreshTokenAsync(storedToken, context.RequestAborted);

                    ClearAuthCookies(context);
                    await _next(context);
                    return;
                }

                var newAccessToken = jwtGenerator.GenerateAccessToken(user);
                var (newRawRefreshToken, newHash) = jwtGenerator.GenerateRefreshToken();

                var newEntity = new RefreshToken
                {
                    UserId          = user.Id,
                    TokenHash       = newHash,
                    ExpiresAtUtc    = DateTime.UtcNow.AddDays(7),
                    IpAddress       = currentIp,
                    UserAgent       = currentUserAgent,
                    SecurityStamp   = user.SecurityStamp
                };

                storedToken.IsRevoked           = true;
                storedToken.RevokedAtUtc        = DateTime.UtcNow;
                storedToken.ReplacedByTokenHash = newHash;

                await authRepository.RotateRefreshTokenAsync(storedToken, newEntity, context.RequestAborted);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure   = true,
                    SameSite = SameSiteMode.Strict,
                    Expires  = DateTime.UtcNow.AddDays(7)
                };

                context.Response.Cookies.Append("access_token", newAccessToken, cookieOptions);
                context.Response.Cookies.Append("refresh_token", newRawRefreshToken, cookieOptions);

                // Re-inject into the current request so [Authorize] sees the fresh token
                context.Request.Headers["Authorization"] = $"Bearer {newAccessToken}";
                context.Request.Cookies = new CookieCollectionWrapper(
                    context.Request.Cookies, "access_token", newAccessToken);
            }

            await _next(context);
        }

        private static void ClearAuthCookies(HttpContext context)
        {
            var expired = new CookieOptions
            {
                Expires  = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Strict
            };
            context.Response.Cookies.Append("access_token", "", expired);
            context.Response.Cookies.Append("refresh_token", "", expired);
        }
    }

    /// Since IRequestCookieCollection is read-only, so created a quick wrapper to overwrite the expired token
    /// with the newly generated token mid-flight before the standard Auth middleware runs.
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
            if (key == _overrideKey) { value = _overrideValue; return true; }
            return _inner.TryGetValue(key, out value);
        }
    }
}
