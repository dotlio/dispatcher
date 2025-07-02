using DotLio.Dispatcher.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Validation;

public class ValidationBehavior<TRequest, TResponse>(
    IServiceProvider serviceProvider,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>, IValidationBehavior
    where TRequest : IRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;

        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();

        if (validators.Count == 0)
        {
            _logger.LogDebug("No validators found for {RequestType}", requestType);
            return await next();
        }

        _logger.LogDebug("Validating {RequestType} using {ValidatorCount} validators", requestType, validators.Count);

        var validationTasks = validators.Select(validator => validator.ValidateAsync(request, cancellationToken));
        var validationResults = await Task.WhenAll(validationTasks);

        var allErrors = validationResults
            .Where(result => !result.IsValid)
            .SelectMany(result => result.Errors)
            .ToList();

        if (allErrors.Count > 0)
        {
            _logger.LogWarning("Validation failed for {RequestType} with {ErrorCount} errors", requestType, allErrors.Count);

            var combinedResult = ValidationResult.Failure(allErrors);
            throw new ValidationException(requestType, combinedResult);
        }

        _logger.LogDebug("Validation passed for {RequestType}", requestType);
        return await next();
    }
}

public class ValidationBehavior<TRequest>(
    IServiceProvider serviceProvider,
    ILogger<ValidationBehavior<TRequest>> logger)
    : IPipelineBehavior<TRequest>, IValidationBehavior
    where TRequest : IRequest
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;

        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();

        if (validators.Count == 0)
        {
            _logger.LogDebug("No validators found for {RequestType}", requestType);
            await next();
            return;
        }

        _logger.LogDebug("Validating {RequestType} using {ValidatorCount} validators", requestType, validators.Count);

        var validationTasks = validators.Select(validator => validator.ValidateAsync(request, cancellationToken));
        var validationResults = await Task.WhenAll(validationTasks);

        var allErrors = validationResults
            .Where(result => !result.IsValid)
            .SelectMany(result => result.Errors)
            .ToList();

        if (allErrors.Count > 0)
        {
            _logger.LogWarning("Validation failed for {RequestType} with {ErrorCount} errors", requestType, allErrors.Count);

            var combinedResult = ValidationResult.Failure(allErrors);
            throw new ValidationException(requestType, combinedResult);
        }

        _logger.LogDebug("Validation passed for {RequestType}", requestType);
        await next();
    }
}