import json
import math
from pathlib import Path

# ============================================================
# VirtualRide - GPX Route -> OSM Road Snapper
# ============================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent

ROUTE_FILE = (
    PROJECT_ROOT / "routes" / "alappuzha" / "route_data.json"
)

PROCESSED_OSM_DIR = (
    PROJECT_ROOT / "world_data" / "alappuzha" / "processed"
)

OUTPUT_FILE = (
    PROJECT_ROOT / "routes" / "alappuzha" / "snapped_route_data.json"
)

# ============================================================
# SETTINGS
# ============================================================

MAX_SNAP_DISTANCE_METERS = 20.0
SEARCH_RADIUS_METERS = 30.0
GRID_CELL_METERS = 50.0

ALLOWED_HIGHWAY_TYPES = {
    "motorway",
    "trunk",
    "primary",
    "secondary",
    "tertiary",
    "unclassified",
    "residential",
    "living_street",
    "service",
    "track",
    "cycleway",
}

EARTH_RADIUS_METERS = 6371000.0


# ============================================================
# GEO HELPERS
# ============================================================

def meters_per_degree_lat():
    return 111320.0


def meters_per_degree_lon(latitude):
    return 111320.0 * math.cos(math.radians(latitude))


def geo_to_local_meters(
    latitude,
    longitude,
    origin_latitude,
    origin_longitude,
):
    x = (
        (longitude - origin_longitude)
        * meters_per_degree_lon(origin_latitude)
    )

    y = (
        (latitude - origin_latitude)
        * meters_per_degree_lat()
    )

    return x, y


def local_meters_to_geo(
    x,
    y,
    origin_latitude,
    origin_longitude,
):
    latitude = (
        origin_latitude
        + y / meters_per_degree_lat()
    )

    longitude = (
        origin_longitude
        + x / meters_per_degree_lon(origin_latitude)
    )

    return latitude, longitude


def distance_squared(ax, ay, bx, by):
    dx = ax - bx
    dy = ay - by
    return dx * dx + dy * dy


def project_point_to_segment(px, py, ax, ay, bx, by):
    abx = bx - ax
    aby = by - ay

    ab_squared = abx * abx + aby * aby

    if ab_squared <= 1e-12:
        return ax, ay, 0.0

    apx = px - ax
    apy = py - ay

    t = (
        apx * abx + apy * aby
    ) / ab_squared

    t = max(0.0, min(1.0, t))

    return (
        ax + abx * t,
        ay + aby * t,
        t,
    )


# ============================================================
# SPATIAL GRID
# ============================================================

class SpatialGrid:
    def __init__(self, cell_size):
        self.cell_size = cell_size
        self.cells = {}

    def cell_coords(self, x, y):
        return (
            math.floor(x / self.cell_size),
            math.floor(y / self.cell_size),
        )

    def insert_segment(
        self,
        segment_index,
        min_x,
        max_x,
        min_y,
        max_y,
    ):
        min_cell_x, min_cell_y = self.cell_coords(
            min_x,
            min_y,
        )

        max_cell_x, max_cell_y = self.cell_coords(
            max_x,
            max_y,
        )

        for cell_x in range(
            min_cell_x,
            max_cell_x + 1,
        ):
            for cell_y in range(
                min_cell_y,
                max_cell_y + 1,
            ):
                key = (cell_x, cell_y)

                self.cells.setdefault(
                    key,
                    [],
                ).append(segment_index)

    def query(self, x, y, radius):
        min_cell_x, min_cell_y = self.cell_coords(
            x - radius,
            y - radius,
        )

        max_cell_x, max_cell_y = self.cell_coords(
            x + radius,
            y + radius,
        )

        results = set()

        for cell_x in range(
            min_cell_x,
            max_cell_x + 1,
        ):
            for cell_y in range(
                min_cell_y,
                max_cell_y + 1,
            ):
                results.update(
                    self.cells.get(
                        (cell_x, cell_y),
                        [],
                    )
                )

        return results


# ============================================================
# LOAD ROUTE
# ============================================================

if not ROUTE_FILE.exists():
    raise SystemExit(
        f"ERROR: Route file not found:\n{ROUTE_FILE}"
    )

with open(
    ROUTE_FILE,
    "r",
    encoding="utf-8",
) as file:
    route_data = json.load(file)

route_points = route_data.get("points", [])

if len(route_points) < 2:
    raise SystemExit(
        "ERROR: Route contains fewer than 2 points."
    )

origin_latitude = route_points[0]["latitude"]
origin_longitude = route_points[0]["longitude"]

print()
print("==========================================")
print(" VirtualRide - Route -> OSM Snapper")
print("==========================================")
print()
print(f"Route points       : {len(route_points)}")
print(f"Max snap distance  : {MAX_SNAP_DISTANCE_METERS:.1f} m")
print(f"Search radius      : {SEARCH_RADIUS_METERS:.1f} m")
print()


# ============================================================
# LOAD PROCESSED OSM ROAD SEGMENTS
# ============================================================

osm_files = sorted(
    PROCESSED_OSM_DIR.glob("chunk_*.json")
)

if not osm_files:
    raise SystemExit(
        f"ERROR: No processed OSM chunks found:\n{PROCESSED_OSM_DIR}"
    )

segments = []

for chunk_file in osm_files:
    with open(
        chunk_file,
        "r",
        encoding="utf-8",
    ) as file:
        chunk_data = json.load(file)

    roads = chunk_data.get("roads", [])

    for road in roads:
        highway_type = road.get("type", "unknown")

        if highway_type not in ALLOWED_HIGHWAY_TYPES:
            continue

        points = road.get("points", [])

        if len(points) < 2:
            continue

        previous = None

        for point in points:
            latitude = point.get("latitude")
            longitude = point.get("longitude")

            if latitude is None or longitude is None:
                previous = None
                continue

            current = geo_to_local_meters(
                latitude,
                longitude,
                origin_latitude,
                origin_longitude,
            )

            if previous is not None:
                segments.append(
                    {
                        "ax": previous[0],
                        "ay": previous[1],
                        "bx": current[0],
                        "by": current[1],
                        "highway": highway_type,
                        "chunk": chunk_data.get(
                            "chunkIndex",
                            -1,
                        ),
                    }
                )

            previous = current

if not segments:
    raise SystemExit(
        "ERROR: No usable OSM road segments found."
    )

print(f"Processed OSM chunks: {len(osm_files)}")
print(f"OSM road segments   : {len(segments)}")
print()


# ============================================================
# BUILD SPATIAL INDEX
# ============================================================

grid = SpatialGrid(GRID_CELL_METERS)

for index, segment in enumerate(segments):
    min_x = min(segment["ax"], segment["bx"])
    max_x = max(segment["ax"], segment["bx"])
    min_y = min(segment["ay"], segment["by"])
    max_y = max(segment["ay"], segment["by"])

    grid.insert_segment(
        index,
        min_x,
        max_x,
        min_y,
        max_y,
    )

print(f"Spatial grid cells  : {len(grid.cells)}")
print()


# ============================================================
# SNAP FUNCTION
# ============================================================

def find_best_osm_point(
    latitude,
    longitude,
    previous_osm_xy=None,
):
    px, py = geo_to_local_meters(
        latitude,
        longitude,
        origin_latitude,
        origin_longitude,
    )

    candidate_indices = grid.query(
        px,
        py,
        SEARCH_RADIUS_METERS,
    )

    if not candidate_indices:
        candidate_indices = range(len(segments))

    best = None

    for index in candidate_indices:
        segment = segments[index]

        projected_x, projected_y, t = (
            project_point_to_segment(
                px,
                py,
                segment["ax"],
                segment["ay"],
                segment["bx"],
                segment["by"],
            )
        )

        snap_distance_sq = distance_squared(
            px,
            py,
            projected_x,
            projected_y,
        )

        continuity_penalty = 0.0

        if previous_osm_xy is not None:
            continuity_distance_sq = distance_squared(
                previous_osm_xy[0],
                previous_osm_xy[1],
                projected_x,
                projected_y,
            )

            continuity_penalty = (
                continuity_distance_sq * 0.02
            )

        score = snap_distance_sq + continuity_penalty

        if best is None or score < best["score"]:
            best = {
                "x": projected_x,
                "y": projected_y,
                "distanceSq": snap_distance_sq,
                "score": score,
                "highway": segment["highway"],
                "chunk": segment["chunk"],
            }

    if best is None:
        return None

    snap_distance = math.sqrt(best["distanceSq"])

    if snap_distance > MAX_SNAP_DISTANCE_METERS:
        return None

    return best


# ============================================================
# SNAP ROUTE
# ============================================================

snapped_points = []

successful_snaps = 0
failed_snaps = 0

total_snap_distance = 0.0
max_snap_distance = 0.0

previous_osm_xy = None

for index, point in enumerate(route_points):
    latitude = point.get("latitude")
    longitude = point.get("longitude")

    if latitude is None or longitude is None:
        raise SystemExit(
            f"ERROR: Invalid coordinates at route point {index}."
        )

    result = find_best_osm_point(
        latitude,
        longitude,
        previous_osm_xy,
    )

    if result is None:
        # Preserve original point rather than snapping to a
        # potentially incorrect distant road.
        snapped_points.append(
            {
                "latitude": latitude,
                "longitude": longitude,
                "elevation": point.get("elevation", 0.0),
                "distanceFromStart": point.get(
                    "distanceFromStart",
                    0.0,
                ),
                "snapped": False,
                "snapDistanceMeters": None,
                "osmHighway": None,
                "osmChunk": None,
            }
        )

        failed_snaps += 1
        previous_osm_xy = None

        print(
            f"Point {index:04d}: UNSNAPPED"
        )

        continue

    snapped_latitude, snapped_longitude = (
        local_meters_to_geo(
            result["x"],
            result["y"],
            origin_latitude,
            origin_longitude,
        )
    )

    snap_distance = math.sqrt(
        result["distanceSq"]
    )

    snapped_points.append(
        {
            "latitude": snapped_latitude,
            "longitude": snapped_longitude,
            "elevation": point.get(
                "elevation",
                0.0,
            ),
            "distanceFromStart": point.get(
                "distanceFromStart",
                0.0,
            ),
            "snapped": True,
            "snapDistanceMeters": snap_distance,
            "osmHighway": result["highway"],
            "osmChunk": result["chunk"],
        }
    )

    successful_snaps += 1
    total_snap_distance += snap_distance
    max_snap_distance = max(
        max_snap_distance,
        snap_distance,
    )

    previous_osm_xy = (
        result["x"],
        result["y"],
    )

    if (
        index % 100 == 0
        or index == len(route_points) - 1
    ):
        print(
            f"Point {index:04d}/"
            f"{len(route_points) - 1:04d} -> "
            f"{snap_distance:.2f} m "
            f"({result['highway']})"
        )


# ============================================================
# RECALCULATE DISTANCE ALONG SNAPPED ROUTE
# ============================================================

distance_from_start_meters = 0.0

for index, point in enumerate(snapped_points):
    if index > 0:
        previous = snapped_points[index - 1]

        ax, ay = geo_to_local_meters(
            previous["latitude"],
            previous["longitude"],
            origin_latitude,
            origin_longitude,
        )

        bx, by = geo_to_local_meters(
            point["latitude"],
            point["longitude"],
            origin_latitude,
            origin_longitude,
        )

        distance_from_start_meters += math.sqrt(
            distance_squared(
                ax,
                ay,
                bx,
                by,
            )
        )

    point["distanceFromStart"] = (
        distance_from_start_meters
    )


# ============================================================
# BUILD OUTPUT
# ============================================================

output_route = dict(route_data)

output_route["points"] = snapped_points

output_route["distanceKm"] = (
    distance_from_start_meters / 1000.0
)

output_route["snapMetadata"] = {
    "maxSnapDistanceMeters":
        MAX_SNAP_DISTANCE_METERS,

    "searchRadiusMeters":
        SEARCH_RADIUS_METERS,

    "roadSegmentCount":
        len(segments),

    "successfulSnaps":
        successful_snaps,

    "failedSnaps":
        failed_snaps,

    "averageSnapDistanceMeters": (
        total_snap_distance / successful_snaps
        if successful_snaps > 0
        else 0.0
    ),

    "maximumSnapDistanceMeters":
        max_snap_distance,
}


OUTPUT_FILE.parent.mkdir(
    parents=True,
    exist_ok=True,
)

with open(
    OUTPUT_FILE,
    "w",
    encoding="utf-8",
) as file:
    json.dump(
        output_route,
        file,
        indent=2,
    )


# ============================================================
# SUMMARY
# ============================================================

print()
print("==========================================")
print(" SNAP COMPLETE")
print("==========================================")
print()
print(f"Original points      : {len(route_points)}")
print(f"Successfully snapped : {successful_snaps}")
print(f"Preserved unsnapped  : {failed_snaps}")
print(
    "Average snap error   : "
    f"{output_route['snapMetadata']['averageSnapDistanceMeters']:.2f} m"
)
print(
    "Maximum snap error   : "
    f"{max_snap_distance:.2f} m"
)
print(
    "New route distance   : "
    f"{output_route['distanceKm']:.3f} km"
)
print()
print("Saved:")
print(OUTPUT_FILE)
print()

if failed_snaps:
    print(
        "WARNING: Some points could not be snapped. "
        "Those points were preserved."
    )
else:
    print(
        "ALL ROUTE POINTS SUCCESSFULLY SNAPPED."
    )
