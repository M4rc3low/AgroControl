from fastapi import FastAPI

from .model import MIN_SUPERVISED_SAMPLES, MODEL_VERSION, predict_yield
from .observability import configure_observability
from .schemas import ModelInfoResponse, YieldPredictionRequest, YieldPredictionResponse

app = FastAPI(
    title="AgroControl Intelligence",
    version="0.1.0",
    description=(
        "Serviço de análise e previsão do AgroControl. As respostas são apoio à decisão e não "
        "substituem avaliação agronômica profissional."
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
