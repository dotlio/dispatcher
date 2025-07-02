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

    private static readonly HashSet<Type> BehaviorTypes =
    [
        typeof(IPipelineBehavior<,>)
    ];

    public static IServiceCollection AddDispatcher(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMediator, Mediator>();

        assemblies = assemblies.Length == 0
            ? [Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Unable to determine entry assembly and no assemblies provided.")]
            : assemblies;

        RegisterTypes(services, assemblies, HandlerTypes);
        RegisterTypes(services, assemblies, BehaviorTypes);

        return services;
    }

    private static void RegisterTypes(IServiceCollection services, Assembly[] assemblies, HashSet<Type> targetTypes)
    {
        var registrations = assemblies
            .SelectMany(GetTypesFromAssembly)
            .Where(IsConcreteClass)
            .SelectMany(type => GetTargetInterfaces(type, targetTypes)
                .Select(targetInterface => new { Type = type, Interface = targetInterface }));

        foreach (var registration in registrations)
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

    private static IEnumerable<Type> GetTargetInterfaces(Type type, HashSet<Type> genericInterfaceDefinitions) =>
        type.GetInterfaces()
            .Where(i => i.IsGenericType && genericInterfaceDefinitions.Contains(i.GetGenericTypeDefinition()));
}