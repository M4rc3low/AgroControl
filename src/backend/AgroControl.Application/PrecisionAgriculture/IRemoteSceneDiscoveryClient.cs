namespace AgroControl.Application.PrecisionAgriculture;

public interface IRemoteSceneDiscoveryClient
{
    IReadOnlyList<RemoteSceneDiscoveryProviderDto> GetProviders();

    Task<RemoteSceneDiscoveryCallResult> SearchAsync(
        RemoteSceneDiscoverySearchRequest request,
        CancellationToken cancellationToken = default);
}
