namespace HapagPortal.Domain.Results;

public interface IValidationResult
{
    Error[] Errors { get; }
}
