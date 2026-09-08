namespace AgroControl.Application.PrecisionAgriculture;

public interface IRemoteSceneDiscoveryClient
{
    IReadOnlyList<RemoteSceneDiscoveryProviderDto> GetProviders();

    Task<RemoteSceneDiscoveryCallResult> SearchAsync(
        RemoteSceneDiscoverySearchRequest request,
        CancellationToken cancellationToken = default);

    Task<RemoteSceneDiscoveryItemCallResult> GetItemAsync(
        string provider,
        string collection,
        string externalId,
        CancellationToken cancellationToken = default);
}
