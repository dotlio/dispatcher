using DotLio.Dispatcher.Validation;

namespace DotLio.Dispatcher.Interfaces;

public interface IValidator<in TRequest> where TRequest : IRequest
{
    Task<ValidationResult> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
}