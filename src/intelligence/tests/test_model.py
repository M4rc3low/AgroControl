from agrocontrol_intelligence.model import MIN_SUPERVISED_SAMPLES, predict_yield
from agrocontrol_intelligence.schemas import HistoricalYieldSample, YieldPredictionRequest


def test_returns_insufficient_data_without_history_or_expected_yield() -> None:
    response = predict_yield(
        YieldPredictionRequest(crop_name="Soja", area_hectares=100)
    )

    assert response.status == "insufficient_data"
    assert response.predicted_yield_per_hectare is None
    assert response.sample_count == 0


def test_uses_expected_yield_as_limited_baseline_without_history() -> None:
    response = predict_yield(
        YieldPredictionRequest(
            crop_name="Soja",
            area_hectares=100,
            expected_yield_per_hectare=62,
        )
    )

    assert response.status == "limited"
    assert response.model_kind == "expected-yield-baseline"
    assert response.predicted_yield_per_hectare == 62


def test_uses_historical_mean_when_sample_is_too_small() -> None:
    response = predict_yield(
        YieldPredictionRequest(
            crop_name="Milho",
            area_hectares=80,
            historical_samples=[
                HistoricalYieldSample(
                    area_hectares=70,
                    expected_yield_per_hectare=100,
                    actual_yield_per_hectare=96,
                ),
                HistoricalYieldSample(
                    area_hectares=85,
                    expected_yield_per_hectare=105,
                    actual_yield_per_hectare=102,
                ),
            ],
        )
    )

    assert response.status == "limited"
    assert response.model_kind == "historical-mean-baseline"
    assert response.predicted_yield_per_hectare == 99


def test_evaluates_supervised_model_deterministically_when_history_is_sufficient() -> None:
    samples = [
        HistoricalYieldSample(
            area_hectares=50 + index * 10,
            expected_yield_per_hectare=55 + index * 2,
            actual_yield_per_hectare=54 + index * 2.1,
        )
        for index in range(MIN_SUPERVISED_SAMPLES + 2)
    ]
    request = YieldPredictionRequest(
        crop_name="Soja",
        area_hectares=95,
        expected_yield_per_hectare=64,
        historical_samples=samples,
    )

    first = predict_yield(request)
    second = predict_yield(request)

    assert first.status == "ok"
    assert first.metrics is not None
    assert first.predicted_yield_per_hectare is not None
    assert first.predicted_yield_per_hectare == second.predicted_yield_per_hectare
    assert first.model_kind in {"ridge-regression", "historical-mean-baseline"}
