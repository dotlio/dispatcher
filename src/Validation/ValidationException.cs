namespace DotLio.Dispatcher.Validation;

public class ValidationException : Exception
{
    public ValidationResult ValidationResult { get; }
    public string RequestType { get; }

    public ValidationException(string requestType, ValidationResult validationResult)
        : base(CreateMessage(requestType, validationResult))
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    }

    public ValidationException(string requestType, ValidationResult validationResult, Exception innerException)
        : base(CreateMessage(requestType, validationResult), innerException)
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    }

    private static string CreateMessage(string requestType, ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(validationResult);

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ToString()));
        return $"Validation failed for {requestType}. Errors: {errors}";
    }
}