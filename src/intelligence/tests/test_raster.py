from pathlib import Path

import numpy as np
import rasterio
from rasterio.transform import from_origin

from agrocontrol_intelligence.raster import (
    RasterProcessingError,
    RasterTarget,
    compute_zonal_statistics_many,
)


def _write_raster(path: Path) -> None:
    data = np.array(
        [
            [0.1, 0.2, 0.3, 0.4],
            [0.2, 0.3, 0.4, 0.5],
            [0.3, 0.4, -9999.0, 0.6],
            [0.4, 0.5, 0.6, 0.7],
        ],
        dtype="float32",
    )
    with rasterio.open(
        path,
        "w",
        driver="GTiff",
        width=4,
        height=4,
        count=1,
        dtype="float32",
        crs="EPSG:4326",
        transform=from_origin(0, 4, 1, 1),
        nodata=-9999.0,
    ) as dataset:
        dataset.write(data, 1)


def test_zonal_statistics_respect_geometry_and_nodata(tmp_path: Path) -> None:
    raster = tmp_path / "ndvi.tif"
    _write_raster(raster)
    target = RasterTarget(
        key="field",
        geometry={
            "type": "Polygon",
            "coordinates": [[[0, 0], [2, 0], [2, 4], [0, 4], [0, 0]]],
        },
    )

    metadata, results = compute_zonal_statistics_many(str(raster), [target])

    assert metadata.crs == "EPSG:4326"
    assert metadata.width == 4
    assert metadata.height == 4
    assert results[0].key == "field"
    assert results[0].statistics.sample_count == 8
    assert results[0].statistics.valid_coverage_percent == 100.0
    assert round(results[0].statistics.mean, 6) == 0.3


def test_duplicate_target_keys_are_rejected(tmp_path: Path) -> None:
    raster = tmp_path / "ndvi.tif"
    _write_raster(raster)
    geometry = {"type": "Polygon", "coordinates": [[[0, 0], [1, 0], [1, 1], [0, 1], [0, 0]]]}
    targets = [RasterTarget("same", geometry), RasterTarget("same", geometry)]

    try:
        compute_zonal_statistics_many(str(raster), targets)
    except RasterProcessingError as exc:
        assert "unique" in str(exc).lower()
    else:
        raise AssertionError("Expected duplicate target keys to be rejected")
