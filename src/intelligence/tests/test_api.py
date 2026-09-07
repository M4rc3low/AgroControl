from fastapi.testclient import TestClient

from agrocontrol_intelligence.main import app

client = TestClient(app)


def test_health_endpoint() -> None:
    response = client.get("/health")

    assert response.status_code == 200
    assert response.json() == {
        "service": "AgroControl.Intelligence",
        "status": "healthy",
    }


def test_model_info_endpoint() -> None:
    response = client.get("/api/v1/model")

    assert response.status_code == 200
    payload = response.json()
    assert payload["contract_version"] == "v1"
    assert payload["model_version"] == "yield-v1.0"


def test_prediction_endpoint_validates_payload_and_returns_versioned_contract() -> None:
    response = client.post(
        "/api/v1/yield/predict",
        json={
            "contract_version": "v1",
            "crop_name": "Soja",
            "area_hectares": 120,
            "expected_yield_per_hectare": 61.5,
            "historical_samples": [],
        },
    )

    assert response.status_code == 200
    payload = response.json()
    assert payload["contract_version"] == "v1"
    assert payload["status"] == "limited"
    assert payload["predicted_yield_per_hectare"] == 61.5


def test_prediction_endpoint_rejects_invalid_area() -> None:
    response = client.post(
        "/api/v1/yield/predict",
        json={"crop_name": "Soja", "area_hectares": 0},
    )

    assert response.status_code == 422
