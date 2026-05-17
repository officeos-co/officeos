namespace OffceOs.Features.ControlPlane.Application;

public sealed record ControlPlaneResourceScope(Guid UserId, Guid WorkspaceId);

public sealed record ControlPlaneResourceDeleteResult(
    bool Deleted,
    bool Unsupported,
    string? Error)
{
    public static ControlPlaneResourceDeleteResult DeletedResult() => new(true, false, null);
    public static ControlPlaneResourceDeleteResult NotFound(string resource) => new(false, false, $"{resource} was not found.");
    public static ControlPlaneResourceDeleteResult UnsupportedResult(string resource) => new(false, true, $"{resource} does not support delete.");
}

public sealed record ControlPlaneMessageRequest(
    string Message,
    string? Purpose);

public sealed record ControlPlaneMessageResult(
    bool Succeeded,
    bool Unsupported,
    bool NotFound,
    int? StatusCode,
    object? Payload,
    string? Error)
{
    public static ControlPlaneMessageResult Sent(object payload) => new(true, false, false, null, payload, null);
    public static ControlPlaneMessageResult BadRequest(string error) => new(false, false, false, 400, null, error);
    public static ControlPlaneMessageResult NotFoundResult(string resource) => new(false, false, true, null, null, $"{resource} was not found.");
    public static ControlPlaneMessageResult UnsupportedResult(string resource) => new(false, true, false, 405, null, $"{resource} does not support messages.");
}

public sealed record ControlPlaneAuthenticationRequest(
    string AccessToken,
    string RefreshToken,
    DateTime? ExpiresAt,
    string? AccountEmail,
    string? AccountId,
    string? ClientId,
    string? TokenUrl,
    IReadOnlyList<string>? Scopes);

public sealed record ControlPlaneAuthenticationResult(
    bool Succeeded,
    bool Unsupported,
    bool NotFound,
    int? StatusCode,
    object? Payload,
    string? Error)
{
    public static ControlPlaneAuthenticationResult Authenticated(object payload) => new(true, false, false, null, payload, null);
    public static ControlPlaneAuthenticationResult BadRequest(string error) => new(false, false, false, 400, null, error);
    public static ControlPlaneAuthenticationResult NotFoundResult(string resource) => new(false, false, true, null, null, $"{resource} was not found.");
    public static ControlPlaneAuthenticationResult UnsupportedResult(string resource) => new(false, true, false, 405, null, $"{resource} does not support authentication.");
}
