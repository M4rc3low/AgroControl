from datetime import datetime
from typing import Literal

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
