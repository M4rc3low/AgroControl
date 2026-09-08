namespace AgroControl.Domain.Modules.PrecisionAgriculture;

public enum RasterProductType
{
    NDVI,
    NDRE,
    EVI,
    Custom
}

public enum RasterProcessingStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed
}

public sealed class RasterProcessingState
{
    private RasterProcessingState(RasterProcessingStatus status) => Status = status;

    public RasterProcessingStatus Status { get; private set; }

    public static RasterProcessingState CreatePending() => new(RasterProcessingStatus.Pending);

    public void Start()
    {
        if (Status != RasterProcessingStatus.Pending)
            throw new InvalidOperationException("Only pending raster processing can be started.");
        Status = RasterProcessingStatus.Processing;
    }

    public void Succeed()
    {
        if (Status != RasterProcessingStatus.Processing)
            throw new InvalidOperationException("Only processing raster work can succeed.");
        Status = RasterProcessingStatus.Succeeded;
    }

    public void Fail()
    {
        if (Status is not (RasterProcessingStatus.Pending or RasterProcessingStatus.Processing))
            throw new InvalidOperationException("Only pending or processing raster work can fail.");
        Status = RasterProcessingStatus.Failed;
    }
}
