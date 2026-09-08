from __future__ import annotations

import ipaddress
import os
from dataclasses import dataclass
from pathlib import Path
from typing import Any
from urllib.parse import urlparse

import numpy as np
import rasterio
from rasterio.features import geometry_mask
from rasterio.warp import transform_geom

MAX_RASTER_PIXELS = 20_000_000
MAX_TARGETS = 250


class RasterProcessingError(RuntimeError):
    pass


@dataclass(frozen=True)
class RasterMetadata:
    crs: str
    width: int
    height: int
    nodata: float | None
    resolution_x: float
    resolution_y: float


@dataclass(frozen=True)
class RasterStatistics:
    minimum: float
    maximum: float
    mean: float
    median: float
    standard_deviation: float
    valid_coverage_percent: float
    sample_count: int


@dataclass(frozen=True)
class RasterTarget:
    key: str
    geometry: dict[str, Any]


@dataclass(frozen=True)
class RasterTargetResult:
    key: str
    statistics: RasterStatistics


def _remote_assets_enabled() -> bool:
    return os.getenv("AGROCONTROL_RASTER_ALLOW_REMOTE", "false").strip().lower() in {
        "1",
        "true",
        "yes",
    }


def _validate_remote_host(value: str) -> None:
    parsed = urlparse(value)
    if parsed.username or parsed.password:
        raise RasterProcessingError("Credentials must not be embedded in raster URLs.")
    if not parsed.hostname:
        return
    try:
        address = ipaddress.ip_address(parsed.hostname)
    except ValueError:
        return
    if address.is_private or address.is_loopback or address.is_link_local or address.is_reserved:
        raise RasterProcessingError("Private or loopback raster URL hosts are not allowed.")


def validate_asset_reference(asset_reference: str) -> str:
    value = asset_reference.strip()
    if not value:
        raise RasterProcessingError("Raster asset reference is required.")
    if value.startswith(("http://", "https://", "s3://", "gs://")):
        if not _remote_assets_enabled():
            raise RasterProcessingError(
                "Remote raster assets are disabled. Enable them explicitly in the Intelligence environment."
            )
        if value.startswith(("http://", "https://")):
            _validate_remote_host(value)
        return value
    path = Path(value).expanduser().resolve()
    if not path.exists() or not path.is_file():
        raise RasterProcessingError("Raster asset is not accessible in the Intelligence environment.")
    return str(path)


def _statistics_for_geometry(dataset: Any, geometry: dict[str, Any], geometry_crs: str, band: int) -> RasterStatistics:
    if not geometry or geometry.get("type") not in {"Polygon", "MultiPolygon"}:
        raise RasterProcessingError("A Polygon or MultiPolygon geometry is required.")

    transformed = transform_geom(geometry_crs, dataset.crs, geometry, precision=9)
    inside = geometry_mask(
        [transformed],
        out_shape=(dataset.height, dataset.width),
        transform=dataset.transform,
        invert=True,
        all_touched=False,
    )
    candidate_count = int(np.count_nonzero(inside))
    if candidate_count == 0:
        raise RasterProcessingError("Geometry does not intersect raster pixels.")

    values = dataset.read(band, masked=True)
    invalid = np.ma.getmaskarray(values)
    valid_mask = inside & ~invalid
    valid = np.asarray(values.data[valid_mask], dtype=np.float64)
    valid = valid[np.isfinite(valid)]
    if valid.size == 0:
        raise RasterProcessingError("Geometry contains no valid raster samples.")

    return RasterStatistics(
        minimum=float(np.min(valid)),
        maximum=float(np.max(valid)),
        mean=float(np.mean(valid)),
        median=float(np.median(valid)),
        standard_deviation=float(np.std(valid, ddof=0)),
        valid_coverage_percent=float(valid.size / candidate_count * 100.0),
        sample_count=int(valid.size),
    )


def compute_zonal_statistics_many(
    asset_reference: str,
    targets: list[RasterTarget],
    geometry_crs: str = "EPSG:4326",
    band: int = 1,
) -> tuple[RasterMetadata, list[RasterTargetResult]]:
    if band < 1:
        raise RasterProcessingError("Raster band must be greater than zero.")
    if not targets:
        raise RasterProcessingError("At least one target geometry is required.")
    if len(targets) > MAX_TARGETS:
        raise RasterProcessingError(f"At most {MAX_TARGETS} target geometries may be processed at once.")
    if len({target.key for target in targets}) != len(targets):
        raise RasterProcessingError("Target keys must be unique within a processing request.")

    asset = validate_asset_reference(asset_reference)
    try:
        with rasterio.open(asset) as dataset:
            if dataset.crs is None:
                raise RasterProcessingError("Raster CRS is required.")
            if band > dataset.count:
                raise RasterProcessingError("Requested band does not exist in the raster.")
            if dataset.width * dataset.height > MAX_RASTER_PIXELS:
                raise RasterProcessingError("Raster exceeds the MVP pixel limit.")

            metadata = RasterMetadata(
                crs=dataset.crs.to_string(),
                width=dataset.width,
                height=dataset.height,
                nodata=None if dataset.nodata is None or not np.isfinite(dataset.nodata) else float(dataset.nodata),
                resolution_x=float(abs(dataset.res[0])),
                resolution_y=float(abs(dataset.res[1])),
            )
            results = [
                RasterTargetResult(
                    key=target.key,
                    statistics=_statistics_for_geometry(dataset, target.geometry, geometry_crs, band),
                )
                for target in targets
            ]
            return metadata, results
    except RasterProcessingError:
        raise
    except Exception as exc:
        raise RasterProcessingError("Raster could not be opened or processed.") from exc
