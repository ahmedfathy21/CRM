namespace CRM.Common.Wrappers;

public sealed record Error(string Code, string Description, int StatusCode)
{
    public static Error NotFound(string name, object key) =>
        new("NOT_FOUND", $"{name} with identifier {key} was not found.", 404);

    public static Error Forbidden(string message = "You do not have permission to perform this action.") =>
        new("FORBIDDEN", message, 403);

    public static Error Conflict(string message) =>
        new("CONFLICT", message, 409);

    public static Error Validation(string message) =>
        new("VALIDATION", message, 422);

    public static Error BadRequest(string message) =>
        new("BAD_REQUEST", message, 400);

    public static Error Unauthorized(string message = "Authentication is required.") =>
        new("UNAUTHORIZED", message, 401);

    public static Error Internal(string message = "An unexpected error occurred.") =>
        new("INTERNAL", message, 500);
}
