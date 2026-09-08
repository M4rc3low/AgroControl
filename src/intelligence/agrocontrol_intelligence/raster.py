from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any

import numpy as np
import rasterio
from rasterio.features import geometry_mask
from rasterio.warp import transform_geom

MAX_RASTER_PIXELS = 20_000_000


class RasterProcessingError(RuntimeError):
    pass


@dataclass(frozen=True)
class RasterStatistics:
    minimum: float
    maximum: float
    mean: float
    median: float
    standard_deviation: float
    valid_coverage_percent: float
    sample_count: int
    crs: str
    width: int
    height: int
    nodata: float | None
    resolution_x: float
    resolution_y: float


def _validate_asset_reference(asset_reference: str) -> str:
    value = asset_reference.strip()
    if not value:
        raise RasterProcessingError("Raster asset reference is required.")
    if value.startswith(("http://", "https://", "s3://", "gs://")):
        return value
    path = Path(value)
    if not path.exists() or not path.is_file():
        raise RasterProcessingError("Raster asset is not accessible in the Intelligence environment.")
    return str(path)


def compute_zonal_statistics(
    asset_reference: str,
    geometry: dict[str, Any],
    geometry_crs: str = "EPSG:4326",
    band: int = 1,
) -> RasterStatistics:
    if band < 1:
        raise RasterProcessingError("Raster band must be greater than zero.")
    if not geometry or geometry.get("type") not in {"Polygon", "MultiPolygon"}:
        raise RasterProcessingError("A Polygon or MultiPolygon geometry is required.")

    asset = _validate_asset_reference(asset_reference)
    try:
        with rasterio.open(asset) as dataset:
            if dataset.crs is None:
                raise RasterProcessingError("Raster CRS is required.")
            if band > dataset.count:
                raise RasterProcessingError("Requested band does not exist in the raster.")
            if dataset.width * dataset.height > MAX_RASTER_PIXELS:
                raise RasterProcessingError("Raster exceeds the MVP pixel limit.")

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

            coverage = float(valid.size / candidate_count * 100.0)
            return RasterStatistics(
                minimum=float(np.min(valid)),
                maximum=float(np.max(valid)),
                mean=float(np.mean(valid)),
                median=float(np.median(valid)),
                standard_deviation=float(np.std(valid, ddof=0)),
                valid_coverage_percent=coverage,
                sample_count=int(valid.size),
                crs=dataset.crs.to_string(),
                width=dataset.width,
                height=dataset.height,
                nodata=None if dataset.nodata is None else float(dataset.nodata),
                resolution_x=float(abs(dataset.res[0])),
                resolution_y=float(abs(dataset.res[1])),
            )
    except RasterProcessingError:
        raise
    except Exception as exc:
        raise RasterProcessingError("Raster could not be opened or processed.") from exc
