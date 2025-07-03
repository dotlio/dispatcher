using System.Reflection;
using DotLio.Dispatcher.Behaviors;
using DotLio.Dispatcher.Configuration;
using DotLio.Dispatcher.Diagnostics;
using DotLio.Dispatcher.Interfaces;
using DotLio.Dispatcher.Internals;
using DotLio.Dispatcher.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace DotLio.Dispatcher.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly HashSet<Type> HandlerTypes =
    [
        typeof(IRequestHandler<>),
        typeof(IRequestHandler<,>),
        typeof(INotificationHandler<>),
        typeof(IRequestPreProcessor<>),
        typeof(IRequestPostProcessor<,>),
        typeof(IRequestPostProcessor<>),
        typeof(IValidator<>)
    ];

    private static readonly HashSet<Type> BehaviorTypes =
    [
        typeof(IPipelineBehavior<,>),
        typeof(IPipelineBehavior<>)
    ];

    public static IServiceCollection AddDispatcher(this IServiceCollection services, Action<DispatcherOptions>? configureOptions = null, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DispatcherOptions();
        configureOptions?.Invoke(options);
        options.Validate();
        
        services.AddSingleton(options);

        if (options.EnableHandlerCache)
        {
            services.AddSingleton<IHandlerCache, HandlerCache>();
        }
        else
        {
            services.AddSingleton<IHandlerCache, NoOpHandlerCache>();
        }

        services.AddSingleton<IMediator, Mediator>();

        assemblies = assemblies.Length == 0
            ? [Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Unable to determine entry assembly and no assemblies provided.")]
            : assemblies;

        RegisterTypes(services, assemblies, HandlerTypes);
        RegisterTypes(services, assemblies, BehaviorTypes);

        if (options.EnableValidation)
        {
            services.AddDispatcherValidation();
        }

        if (options.EnableTimeouts)
        {
            services.AddDispatcherTimeouts();
        }

        return services;
    }

    public static IServiceCollection AddDispatcher(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddDispatcher(null, assemblies);
    }

    public static IServiceCollection AddDispatcherValidation(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<>), typeof(ValidationBehavior<>));
        return services;
    }

    public static IServiceCollection AddDispatcherTimeouts(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TimeoutBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<>), typeof(TimeoutBehavior<>));
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

    private class NoOpHandlerCache(IServiceProvider serviceProvider) : IHandlerCache
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        public T GetHandler<T>() => _serviceProvider.GetRequiredService<T>();

        public IEnumerable<T> GetHandlers<T>() => _serviceProvider.GetServices<T>();

        public void ClearCache()
        {
        }

        public CacheStatistics GetStatistics() => new()
        {
            CacheHits = 0,
            CacheMisses = 0,
            HitRate = 0,
            SingleHandlerCount = 0,
            MultiHandlerCount = 0,
            TotalCachedItems = 0
        };
    }
}