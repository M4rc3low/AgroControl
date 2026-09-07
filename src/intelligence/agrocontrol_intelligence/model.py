from dataclasses import dataclass
from datetime import UTC, datetime

import numpy as np
from sklearn.linear_model import Ridge

from .schemas import EvaluationMetrics, YieldPredictionRequest, YieldPredictionResponse

MODEL_VERSION = "yield-v1.0"
MIN_SUPERVISED_SAMPLES = 4


@dataclass(frozen=True)
class _Evaluation:
    baseline_mae: float
    baseline_rmse: float
    ridge_mae: float
    ridge_rmse: float


def _metrics(actual: np.ndarray, predicted: np.ndarray) -> tuple[float, float]:
    errors = actual - predicted
    mae = float(np.mean(np.abs(errors)))
    rmse = float(np.sqrt(np.mean(np.square(errors))))
    return mae, rmse


def _expected_fill(samples_expected: list[float | None], fallback: float) -> float:
    available = [value for value in samples_expected if value is not None]
    return float(np.mean(available)) if available else fallback


def _feature(area: float, expected: float | None, expected_fill: float) -> list[float]:
    return [float(area), float(expected if expected is not None else expected_fill)]


def _leave_one_out_evaluation(request: YieldPredictionRequest) -> _Evaluation:
    samples = request.historical_samples
    actual = np.array([sample.actual_yield_per_hectare for sample in samples], dtype=float)
    baseline_predictions: list[float] = []
    ridge_predictions: list[float] = []

    for index, sample in enumerate(samples):
        train = [item for item_index, item in enumerate(samples) if item_index != index]
        train_y = np.array([item.actual_yield_per_hectare for item in train], dtype=float)
        train_mean = float(np.mean(train_y))
        fill = _expected_fill([item.expected_yield_per_hectare for item in train], train_mean)
        train_x = np.array(
            [_feature(item.area_hectares, item.expected_yield_per_hectare, fill) for item in train],
            dtype=float,
        )
        target_x = np.array(
            [_feature(sample.area_hectares, sample.expected_yield_per_hectare, fill)], dtype=float
        )

        baseline_predictions.append(train_mean)
        model = Ridge(alpha=1.0)
        model.fit(train_x, train_y)
        ridge_predictions.append(float(model.predict(target_x)[0]))

    baseline_mae, baseline_rmse = _metrics(actual, np.array(baseline_predictions))
    ridge_mae, ridge_rmse = _metrics(actual, np.array(ridge_predictions))
    return _Evaluation(baseline_mae, baseline_rmse, ridge_mae, ridge_rmse)


def predict_yield(request: YieldPredictionRequest) -> YieldPredictionResponse:
    samples = request.historical_samples
    generated_at = datetime.now(UTC)

    if not samples:
        if request.expected_yield_per_hectare is None:
            return YieldPredictionResponse(
                status="insufficient_data",
                predicted_yield_per_hectare=None,
                baseline_yield_per_hectare=None,
                model_kind="none",
                model_version=MODEL_VERSION,
                sample_count=0,
                generated_at_utc=generated_at,
                warning=(
                    "No historical production samples or expected yield were provided. "
                    "A prediction would be speculative."
                ),
            )

        expected = float(request.expected_yield_per_hectare)
        return YieldPredictionResponse(
            status="limited",
            predicted_yield_per_hectare=expected,
            baseline_yield_per_hectare=expected,
            model_kind="expected-yield-baseline",
            model_version=MODEL_VERSION,
            sample_count=0,
            generated_at_utc=generated_at,
            warning=(
                "Prediction uses only the expected yield registered for the season because "
                "historical production samples are not available."
            ),
        )

    actual_values = np.array([sample.actual_yield_per_hectare for sample in samples], dtype=float)
    historical_mean = float(np.mean(actual_values))

    if len(samples) < MIN_SUPERVISED_SAMPLES:
        return YieldPredictionResponse(
            status="limited",
            predicted_yield_per_hectare=historical_mean,
            baseline_yield_per_hectare=historical_mean,
            model_kind="historical-mean-baseline",
            model_version=MODEL_VERSION,
            sample_count=len(samples),
            generated_at_utc=generated_at,
            warning=(
                f"At least {MIN_SUPERVISED_SAMPLES} historical samples are required to "
                "evaluate the supervised model."
            ),
        )

    evaluation = _leave_one_out_evaluation(request)
    use_ridge = evaluation.ridge_mae <= evaluation.baseline_mae

    if use_ridge:
        expected_fill = _expected_fill(
            [sample.expected_yield_per_hectare for sample in samples], historical_mean
        )
        features = np.array(
            [
                _feature(sample.area_hectares, sample.expected_yield_per_hectare, expected_fill)
                for sample in samples
            ],
            dtype=float,
        )
        target = np.array(
            [_feature(request.area_hectares, request.expected_yield_per_hectare, expected_fill)],
            dtype=float,
        )
        model = Ridge(alpha=1.0)
        model.fit(features, actual_values)
        predicted = max(0.0, float(model.predict(target)[0]))
        metrics = EvaluationMetrics(mae=evaluation.ridge_mae, rmse=evaluation.ridge_rmse)
        model_kind = "ridge-regression"
    else:
        predicted = historical_mean
        metrics = EvaluationMetrics(
            mae=evaluation.baseline_mae,
            rmse=evaluation.baseline_rmse,
        )
        model_kind = "historical-mean-baseline"

    return YieldPredictionResponse(
        status="ok",
        predicted_yield_per_hectare=predicted,
        baseline_yield_per_hectare=historical_mean,
        model_kind=model_kind,
        model_version=MODEL_VERSION,
        sample_count=len(samples),
        metrics=metrics,
        generated_at_utc=generated_at,
        warning=(
            "Decision-support estimate only. It is not an agronomic prescription and does not "
            "replace field assessment by a qualified professional."
        ),
    )
