namespace CardDuel.ServerApi.Contracts;

public sealed record ErrorResponse(
    string Code,
    string Message,
    string? Details = null,
    string? CorrelationId = null,
    DateTimeOffset Timestamp = default)
{
    public static ErrorResponse ValidationError(string message, string? details = null, string? correlationId = null) =>
        new("VALIDATION_ERROR", message, details, correlationId);

    public static ErrorResponse NotFound(string resource, string? correlationId = null) =>
        new("NOT_FOUND", $"{resource} not found", null, correlationId);

    public static ErrorResponse Unauthorized(string? correlationId = null) =>
        new("UNAUTHORIZED", "Authentication required", null, correlationId);

    public static ErrorResponse Forbidden(string? correlationId = null) =>
        new("FORBIDDEN", "Access denied", null, correlationId);

    public static ErrorResponse ConflictError(string message, string? details = null, string? correlationId = null) =>
        new("CONFLICT", message, details, correlationId);

    public static ErrorResponse ServerError(string message, string? details = null, string? correlationId = null) =>
        new("INTERNAL_ERROR", message, details, correlationId);
}
