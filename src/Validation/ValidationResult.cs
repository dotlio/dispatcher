using System.Collections.ObjectModel;

namespace DotLio.Dispatcher.Validation;

public class ValidationResult
{
    private readonly List<ValidationError> _errors;
    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationResult()
    {
        _errors = new List<ValidationError>();
        Errors = new ReadOnlyCollection<ValidationError>(_errors);
    }

    public ValidationResult(IEnumerable<ValidationError> errors)
    {
        _errors = new List<ValidationError>(errors ?? throw new ArgumentNullException(nameof(errors)));
        Errors = new ReadOnlyCollection<ValidationError>(_errors);
    }

    public void AddError(string propertyName, string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        _errors.Add(new ValidationError(propertyName, errorMessage));
    }

    public void AddError(ValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _errors.Add(error);
    }

    public static ValidationResult Success() => new();

    public static ValidationResult Failure(string propertyName, string errorMessage) =>
        new([new ValidationError(propertyName, errorMessage)]);

    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new(errors);
}

public sealed record ValidationError(string PropertyName, string ErrorMessage)
{
    public string PropertyName { get; } = PropertyName ?? throw new ArgumentNullException(nameof(PropertyName));
    public string ErrorMessage { get; } = ErrorMessage ?? throw new ArgumentNullException(nameof(ErrorMessage));
    public override string ToString() => $"{PropertyName}: {ErrorMessage}";
}