using System.Reflection;
using DotLio.Dispatcher.Interfaces;
using DotLio.Dispatcher.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace DotLio.Dispatcher.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDispatcher(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IMediator, Mediator>();
        assemblies = assemblies.Length > 0 ? assemblies : [Assembly.GetEntryAssembly()!];
        foreach (var assembly in assemblies)
        {
            var handlers = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract &&
                               (type.GetInterfaces().Any(i =>
                                   i.IsGenericType &&
                                   (i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                                    i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                               ))
                .ToList();

            foreach (var handler in handlers)
            {
                var implementedInterfaces = handler.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

                foreach (var implementedInterface in implementedInterfaces)
                {
                    services.AddTransient(implementedInterface, handler);
                }
            }
        }

        return services;
    }
}