using BusTracker.Application.Common.Behaviors;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Tracking.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
namespace BusTracker.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);
            });

            services.AddValidatorsFromAssembly(assembly);

            services.AddScoped<ILocationProcessorService, LocationProcessorService>();

            return services;
        }
    }
}