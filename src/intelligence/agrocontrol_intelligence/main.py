from fastapi import FastAPI, HTTPException

from .model import MIN_SUPERVISED_SAMPLES, MODEL_VERSION, predict_yield
from .observability import configure_observability
from .raster import RasterProcessingError, RasterTarget, compute_zonal_statistics_many
from .schemas import (
    ModelInfoResponse,
    RasterMetadataResponse,
    RasterStatisticsResponse,
    RasterTargetResponse,
    RasterZonalStatisticsRequest,
    RasterZonalStatisticsResponse,
    YieldPredictionRequest,
    YieldPredictionResponse,
)

app = FastAPI(
    title="AgroControl Intelligence",
    version="0.2.0",
    description=(
        "Serviço de análise, previsão e processamento geoespacial do AgroControl. As respostas "
        "são apoio à decisão e não substituem avaliação agronômica profissional."
    ),
)

configure_observability(app)


@app.get("/health")
def health() -> dict[str, str]:
    return {"service": "AgroControl.Intelligence", "status": "healthy"}


@app.get("/health/live")
def liveness() -> dict[str, str]:
    return {"service": "AgroControl.Intelligence", "status": "alive"}


@app.get("/health/ready")
def readiness() -> dict[str, str]:
    return {"service": "AgroControl.Intelligence", "status": "ready"}


@app.get("/api/v1/model", response_model=ModelInfoResponse)
def model_info() -> ModelInfoResponse:
    return ModelInfoResponse(
        service="AgroControl.Intelligence",
        model_version=MODEL_VERSION,
        strategy=(
            "Expected-yield or historical-mean baseline; deterministic Ridge regression is "
            "selected only when leave-one-out MAE is not worse than the baseline."
        ),
        minimum_samples_for_supervised_model=MIN_SUPERVISED_SAMPLES,
    )


@app.post("/api/v1/yield/predict", response_model=YieldPredictionResponse)
def yield_prediction(request: YieldPredictionRequest) -> YieldPredictionResponse:
    return predict_yield(request)


@app.post("/api/v1/raster/zonal-statistics", response_model=RasterZonalStatisticsResponse)
def raster_zonal_statistics(request: RasterZonalStatisticsRequest) -> RasterZonalStatisticsResponse:
    try:
        metadata, results = compute_zonal_statistics_many(
            request.asset_reference,
            [RasterTarget(key=item.key, geometry=item.geometry) for item in request.targets],
            request.geometry_crs,
            request.band,
        )
    except RasterProcessingError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc

    return RasterZonalStatisticsResponse(
        metadata=RasterMetadataResponse(
            crs=metadata.crs,
            width=metadata.width,
            height=metadata.height,
            nodata=metadata.nodata,
            resolution_x=metadata.resolution_x,
            resolution_y=metadata.resolution_y,
        ),
        results=[
            RasterTargetResponse(
                key=item.key,
                statistics=RasterStatisticsResponse(
                    minimum=item.statistics.minimum,
                    maximum=item.statistics.maximum,
                    mean=item.statistics.mean,
                    median=item.statistics.median,
                    standard_deviation=item.statistics.standard_deviation,
                    valid_coverage_percent=item.statistics.valid_coverage_percent,
                    sample_count=item.statistics.sample_count,
                ),
            )
            for item in results
        ],
    )
