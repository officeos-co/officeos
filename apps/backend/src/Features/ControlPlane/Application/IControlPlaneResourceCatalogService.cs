using OffceOs.Features.ControlPlane.Domain;

namespace OffceOs.Features.ControlPlane.Application;

public interface IControlPlaneResourceCatalogService
{
    IReadOnlyList<ControlPlaneResourceDescriptor> List();
    ControlPlaneResourceDescriptor? Find(string kindOrAlias);
}
