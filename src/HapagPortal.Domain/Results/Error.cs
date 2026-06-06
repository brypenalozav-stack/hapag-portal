namespace HapagPortal.Domain.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");
    public static readonly Error Unauthorized = new("Error.Unauthorized", "You are not authorized to perform this action.");
    public static readonly Error Forbidden = new("Error.Forbidden", "You do not have permission to access this resource.");

    public static Error NotFound(string entityName) =>
        new($"{entityName}.NotFound", $"The {entityName} was not found.");

    public static Error Exists(string entityName) =>
        new($"{entityName}.Exists", $"The {entityName} already exists.");

    public static Error HasData(string entityName) =>
        new($"{entityName}.HasData", $"The {entityName} has associated data and cannot be removed.");

    public static Error Validation(string message) =>
        new("Error.Validation", message);
}
