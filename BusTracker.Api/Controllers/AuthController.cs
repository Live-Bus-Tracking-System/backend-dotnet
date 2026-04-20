using BusTracker.Application.Features.Auth.Commands.ChangePassword;
using BusTracker.Application.Features.Auth.Commands.ForgotPassword;
using BusTracker.Application.Features.Auth.Commands.Login;
using BusTracker.Application.Features.Auth.Commands.Logout;
using BusTracker.Application.Features.Auth.Commands.Register;
using BusTracker.Application.Features.Auth.Commands.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusTracker.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            // Enrich the command with HTTP context info before dispatching
            var enrichedCommand = command with { IpAddress = ipAddress, UserAgent = userAgent };

            var result = await _mediator.Send(enrichedCommand);

            SetTokenCookies(result.AccessToken, result.RefreshToken);

            return Ok(result.User);
        }

        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Signup([FromBody] RegisterCommand command)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            var enrichedCommand = command with { IpAddress = ipAddress, UserAgent = userAgent };

            var result = await _mediator.Send(enrichedCommand);

            SetTokenCookies(result.AccessToken, result.RefreshToken);

            return Ok(result.User);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            var rawRefreshToken = Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(rawRefreshToken))
            {
                await _mediator.Send(new LogoutCommand(rawRefreshToken));
            }

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");

            return Ok(new { Message = "Successfully logged out." });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            var token = await _mediator.Send(command);

            // In MVP: returning token to client. In PROD: Do NOT return the token, email it.
            return Ok(new { Message = "Reset token generated.", Token = token });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            await _mediator.Send(command);
            return Ok(new { Message = "Password has been successfully reset." });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            await _mediator.Send(command);

            return Ok(new { Message = "Password successfully changed. Other sessions will be terminated." });
        }

        private void SetTokenCookies(string accessToken, string refreshToken)
        {
            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.None,
                Expires  = DateTime.UtcNow.AddMinutes(15)
            };

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.None,
                Expires  = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("access_token", accessToken, accessTokenOptions);
            Response.Cookies.Append("refresh_token", refreshToken, refreshTokenOptions);
        }
    }

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
