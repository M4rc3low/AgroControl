from datetime import datetime
from typing import Any, Literal

from pydantic import BaseModel, Field


class HistoricalYieldSample(BaseModel):
    area_hectares: float = Field(gt=0)
    expected_yield_per_hectare: float | None = Field(default=None, ge=0)
    actual_yield_per_hectare: float = Field(ge=0)


class YieldPredictionRequest(BaseModel):
    contract_version: Literal["v1"] = "v1"
    crop_name: str = Field(min_length=1, max_length=120)
    crop_variety: str | None = Field(default=None, max_length=120)
    area_hectares: float = Field(gt=0)
    expected_yield_per_hectare: float | None = Field(default=None, ge=0)
    historical_samples: list[HistoricalYieldSample] = Field(default_factory=list, max_length=500)


class EvaluationMetrics(BaseModel):
    mae: float
    rmse: float


class YieldPredictionResponse(BaseModel):
    contract_version: Literal["v1"] = "v1"
    status: Literal["ok", "limited", "insufficient_data"]
    predicted_yield_per_hectare: float | None
    baseline_yield_per_hectare: float | None
    model_kind: str
    model_version: str
    sample_count: int
    metrics: EvaluationMetrics | None = None
    generated_at_utc: datetime
    warning: str | None = None


class ModelInfoResponse(BaseModel):
    service: str
    contract_version: Literal["v1"] = "v1"
    model_version: str
    strategy: str
    minimum_samples_for_supervised_model: int


class RasterTargetRequest(BaseModel):
    key: str = Field(min_length=1, max_length=160)
    geometry: dict[str, Any]


class RasterZonalStatisticsRequest(BaseModel):
    contract_version: Literal["v1"] = "v1"
    asset_reference: str = Field(min_length=1, max_length=4096)
    geometry_crs: str = Field(default="EPSG:4326", min_length=1, max_length=120)
    band: int = Field(default=1, ge=1, le=128)
    targets: list[RasterTargetRequest] = Field(min_length=1, max_length=250)


class RasterMetadataResponse(BaseModel):
    crs: str
    width: int
    height: int
    nodata: float | None
    resolution_x: float
    resolution_y: float


class RasterStatisticsResponse(BaseModel):
    minimum: float
    maximum: float
    mean: float
    median: float
    standard_deviation: float
    valid_coverage_percent: float
    sample_count: int


class RasterTargetResponse(BaseModel):
    key: str
    statistics: RasterStatisticsResponse


class RasterZonalStatisticsResponse(BaseModel):
    contract_version: Literal["v1"] = "v1"
    status: Literal["ok"] = "ok"
    metadata: RasterMetadataResponse
    results: list[RasterTargetResponse]
