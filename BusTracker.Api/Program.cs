using BusTracker.Api.Filters;
using BusTracker.Api.Hubs;
using BusTracker.Api.Middleware;
using BusTracker.Api.Services;
using BusTracker.Application;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Infrastructure;
using BusTracker.Infrastructure.Seeders;
using System.Threading.RateLimiting;

namespace BusTracker.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── MVC ───────────────────────────────────────────────────────────────
            builder.Services.AddControllers(options =>
            {
                // Wrap every controller response in ApiResponse<T> automatically
                options.Filters.Add<ApiResponseFilter>();
            });
            builder.Services.AddEndpointsApiExplorer();

            // ── Authorization ─────────────────────────────────────────────────────
            builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, BusTracker.Api.Authorization.PermissionPolicyProvider>();
            builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, BusTracker.Api.Authorization.PermissionAuthorizationHandler>();

            // ── Swagger ───────────────────────────────────────────────────────────
            builder.Services.AddSwaggerGen();


            // ── Application + Infrastructure ──────────────────────────────────────
            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // ── SignalR ───────────────────────────────────────────────────────────
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<ILiveTrackingBroadcaster, SignalRLiveTrackingBroadcaster>();

            // ── CORS — allow local HTML test page (file://) to reach the SignalR hub ──
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DevTestPolicy", policy =>
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()); // Required for SignalR + cookies
            });

            // ── Rate limiter ──────────────────────────────────────────────────────
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("TrackerPingPolicy", context =>
                {
                    var trackerId = context.Request.Headers["X-Tracker-Id"].ToString();
                    return RateLimitPartition.GetFixedWindowLimiter(trackerId, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 1,
                        Window = TimeSpan.FromSeconds(3),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 1
                    });
                });
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            var app = builder.Build();

            // ── Global Exception Handling ─────────────────────────────────────────
            app.UseMiddleware<GlobalExceptionMiddleware>();
            // ── Seed roles, permissions, and SuperAdmin user ───────────────────────
            using (var scope = app.Services.CreateScope())
            {
                await SuperAdminSeeder.SeedAsync(scope.ServiceProvider);
            }

            app.UseRateLimiter();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
            //}

            // Only redirect to HTTPS in non-development environments.
            // In dev with --launch-profile http, UseHttpsRedirection causes:
            //  - Signature failures (body stream consumed before redirect)
            //  - 404s (HTTPS port 7075 not bound on http profile)
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("DevTestPolicy");

            app.UseMiddleware<AutoRefreshTokenMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<TrackingHub>("/hubs/tracking");

            await app.RunAsync();
        }
    }
}
