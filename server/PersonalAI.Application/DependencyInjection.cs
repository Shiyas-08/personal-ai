using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using AutoMapper;

namespace PersonalAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        // Register FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
