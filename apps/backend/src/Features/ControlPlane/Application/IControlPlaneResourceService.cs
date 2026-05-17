using OffceOs.Features.ControlPlane.Domain;

namespace OffceOs.Features.ControlPlane.Application;

public interface IControlPlaneResourceService
{
    IReadOnlyList<ControlPlaneResourceDescriptor> ListDefinitions();
    ControlPlaneResourceDescriptor? FindDefinition(string kindOrAlias);
    Task<IReadOnlyList<ControlPlaneResourceRecord>?> ListAsync(string kindOrAlias, ControlPlaneResourceScope scope, CancellationToken ct = default);
    Task<ControlPlaneResourceRecord?> DescribeAsync(string kindOrAlias, string name, ControlPlaneResourceScope scope, CancellationToken ct = default);
    Task<ControlPlaneResourceDeleteResult> DeleteAsync(string kindOrAlias, string name, ControlPlaneResourceScope scope, CancellationToken ct = default);
    Task<ControlPlaneMessageResult> SendMessageAsync(string kindOrAlias, string name, ControlPlaneMessageRequest request, ControlPlaneResourceScope scope, CancellationToken ct = default);
    Task<ControlPlaneAuthenticationResult> AuthenticateAsync(string kindOrAlias, string name, ControlPlaneAuthenticationRequest request, ControlPlaneResourceScope scope, CancellationToken ct = default);
}
