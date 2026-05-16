namespace OffceOs.Application.Features.ControlPlane;

public interface IControlPlaneResourceCatalogService
{
    IReadOnlyList<ControlPlaneResourceDescriptor> List();
    ControlPlaneResourceDescriptor? Find(string kindOrAlias);
}
