using System.Reflection;
using DotLio.Dispatcher.Interfaces;
using DotLio.Dispatcher.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace DotLio.Dispatcher.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly HashSet<Type> HandlerTypes =
    [
        typeof(IRequestHandler<>),
        typeof(IRequestHandler<,>),
        typeof(INotificationHandler<>)
    ];

    public static IServiceCollection AddDispatcher(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IMediator, Mediator>();

        assemblies = assemblies.Length == 0
            ? [Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Unable to determine entry assembly and no assemblies provided.")]
            : assemblies;

        RegisterHandlers(services, assemblies);
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var handlerRegistrations = assemblies
            .SelectMany(GetTypesFromAssembly)
            .Where(IsConcreteClass)
            .SelectMany(type => GetHandlerInterfaces(type)
                .Select(handlerInterface => new { Type = type, Interface = handlerInterface }));

        foreach (var registration in handlerRegistrations)
        {
            services.AddTransient(registration.Interface, registration.Type);
        }
    }

    private static Type[] GetTypesFromAssembly(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).ToArray()!;
        }
    }

    private static bool IsConcreteClass(Type type) =>
        type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false };

    private static IEnumerable<Type> GetHandlerInterfaces(Type type) =>
        type.GetInterfaces()
            .Where(i => i.IsGenericType && HandlerTypes.Contains(i.GetGenericTypeDefinition()));
}