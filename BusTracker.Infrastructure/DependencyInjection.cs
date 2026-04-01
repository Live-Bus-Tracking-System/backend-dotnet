//using BusTracker.Domain.Entities;
//using BusTracker.Infrastructure.Persistence;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;

//namespace BusTracker.Infrastructure
//{
//    public static class DependencyInjection
//    {
//        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
//        {
//            services.AddDbContext<ApplicationDbContext>(options =>
//                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
//                    x => x.UseNetTopologySuite()));

//            services.AddIdentityCore<ApplicationUser>(options => {
//                options.User.RequireUniqueEmail = true;
//            })
//            .AddEntityFrameworkStores<ApplicationDbContext>();

//            return services;
//        }
//    }
//}



//=============================================================

using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Repository;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Domain.Entities;
using BusTracker.Infrastructure.BackgroundJobs;
using BusTracker.Infrastructure.Persistence;
using BusTracker.Infrastructure.Persistence.Repositories;
using BusTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using Quartz;
using BusTracker.Infrastructure.Settings;
using BusTracker.Infrastructure.Services.Messaging;

namespace BusTracker.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ── Database ─────────────────────────────────────────────────────────
            services.AddSingleton<BusTracker.Infrastructure.Persistence.Interceptors.ConvertDomainEventsToOutboxMessagesInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    builder =>
                    {
                        builder.UseNetTopologySuite();
                        builder.CommandTimeout(60);
                    })
                .AddInterceptors(sp.GetRequiredService<BusTracker.Infrastructure.Persistence.Interceptors.ConvertDomainEventsToOutboxMessagesInterceptor>()));

            // ── Identity (full, with roles) ───────────────────────────────────────
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // ── JWT Bearer (reads from HttpOnly cookie, not Authorization header) ──
            var jwtSecret = configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret is not configured. Set it in User Secrets.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = configuration["Jwt:Issuer"],
                    ValidAudience            = configuration["Jwt:Audience"],
                    IssuerSigningKey         = key,
                    ClockSkew                = TimeSpan.Zero
                };

                // Read token from the HttpOnly cookie instead of the Authorization header
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["access_token"];
                        return Task.CompletedTask;
                    }
                };
            });

            // ── Authorization policies (permission-based) ─────────────────────────
            services.AddAuthorizationBuilder()
                .AddPolicy("stop:create",         p => p.RequireClaim("permission", "stop:create"))
                .AddPolicy("route:create",        p => p.RequireClaim("permission", "route:create"))
                .AddPolicy("route:read",          p => p.RequireClaim("permission", "route:read"))
                .AddPolicy("vehicle:create",      p => p.RequireClaim("permission", "vehicle:create"))
                .AddPolicy("vehicle:manage",      p => p.RequireClaim("permission", "vehicle:manage"))
                .AddPolicy("vehicle:read",        p => p.RequireClaim("permission", "vehicle:read"))
                .AddPolicy("permit:request",      p => p.RequireClaim("permission", "permit:request"))
                .AddPolicy("permit:approve",      p => p.RequireClaim("permission", "permit:approve"))
                .AddPolicy("permit:read",         p => p.RequireClaim("permission", "permit:read"))
                .AddPolicy("org:manage:own",      p => p.RequireClaim("permission", "org:manage:own"))
                .AddPolicy("org:manage:all",      p => p.RequireClaim("permission", "org:manage:all"));

            // ── Redis ─────────────────────────────────────────────────────────────
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(configuration.GetConnectionString("RedisConnection")!));

            // ── Application services ──────────────────────────────────────────────
            services.AddScoped<IVehicleStateCache, RedisVehicleStateCache>();
            services.AddSingleton<ITrackerSecurityService, TrackerSecurityService>();
            services.AddScoped<ITrackingRepository, TrackingRepository>();
            services.AddSingleton<ITrackingEventChannel, TrackingEventChannel>();
            
            // ── Auth Services ─────────────────────────────────────────────────────
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IPhoneNumberService, PhoneNumberService>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IEventService, EventService>();

            // ── Messaging & Templates ─────────────────────────────────────────────
            services.Configure<SendGridSettings>(configuration.GetSection(SendGridSettings.SectionName));
            services.Configure<TwilioSettings>(configuration.GetSection(TwilioSettings.SectionName));

            services.AddScoped<IEmailService, SendGridEmailService>();
            services.AddScoped<ISmsService, TwilioSmsService>();
            services.AddScoped<ITemplateService, HandlebarsTemplateService>();

            services.AddHostedService<TrackingEventWorker>();
            services.AddHostedService<VehicleWatchdogWorker>();

            // ── Background Jobs ───────────────────────────────────────────────────
            services.AddQuartz(configure =>
            {
                var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));

                configure.AddJob<ProcessOutboxMessagesJob>(jobKey)
                         .AddTrigger(trigger =>
                             trigger.ForJob(jobKey)
                                    .WithSimpleSchedule(schedule =>
                                        schedule.WithIntervalInSeconds(120) // Poll every 120 seconds
                                                .RepeatForever()));
            });

            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            return services;
        }
    }
}
