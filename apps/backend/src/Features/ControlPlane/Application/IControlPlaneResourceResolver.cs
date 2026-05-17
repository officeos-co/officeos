using OffceOs.Features.ControlPlane.Domain;

namespace OffceOs.Features.ControlPlane.Application;

public interface IControlPlaneResourceResolver
{
    string Kind { get; }
    Task<IReadOnlyList<ControlPlaneResourceRecord>> ListAsync(ControlPlaneResourceScope scope, CancellationToken ct = default);
    Task<ControlPlaneResourceRecord?> DescribeAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default);
}

public interface IDeletableControlPlaneResourceResolver : IControlPlaneResourceResolver
{
    Task<bool> DeleteAsync(ControlPlaneResourceScope scope, string name, CancellationToken ct = default);
}

public interface IMessageControlPlaneResourceResolver : IControlPlaneResourceResolver
{
    Task<ControlPlaneMessageResult> SendMessageAsync(
        ControlPlaneResourceScope scope,
        string name,
        ControlPlaneMessageRequest request,
        CancellationToken ct = default);
}

public interface IAuthenticatableControlPlaneResourceResolver : IControlPlaneResourceResolver
{
    Task<ControlPlaneAuthenticationResult> AuthenticateAsync(
        ControlPlaneResourceScope scope,
        string name,
        ControlPlaneAuthenticationRequest request,
        CancellationToken ct = default);
}
