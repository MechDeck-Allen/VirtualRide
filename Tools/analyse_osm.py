import json
import math
import urllib.request
import urllib.parse
import time
from pathlib import Path


# ============================================================
# VirtualRide - Full Route OSM Chunk Downloader
# ============================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent

ROUTE_FILE = (
    PROJECT_ROOT
    / "routes"
    / "alappuzha"
    / "route_data.json"
)

OUTPUT_DIR = (
    PROJECT_ROOT
    / "world_data"
    / "alappuzha"
    / "osm"
)

OVERPASS_URL = (
    "https://overpass-api.de/api/interpreter"
)


# ============================================================
# SETTINGS
# ============================================================

# Approximately 1 km of route per chunk.

CHUNK_LENGTH_KM = 1.0

# Environment corridor around the route.

CORRIDOR_METERS = 300.0

# Small delay between Overpass requests.

REQUEST_DELAY_SECONDS = 2.0


# ============================================================
# Geographic helpers
# ============================================================

def meters_per_degree_lat():

    return 111320.0


def meters_per_degree_lon(latitude):

    return (
        111320.0
        * math.cos(
            math.radians(latitude)
        )
    )


# ============================================================
# Load route
# ============================================================

if not ROUTE_FILE.exists():

    print()
    print("ERROR: RouteData file not found.")
    print(ROUTE_FILE)
    print()

    raise SystemExit(1)


with open(
    ROUTE_FILE,
    "r",
    encoding="utf-8"
) as file:

    route_data = json.load(file)


route_points = route_data["points"]


total_route_km = route_data[
    "distanceKm"
]


# ============================================================
# Build chunks
# ============================================================

chunk_starts = []

current_distance = 0.0


while current_distance < total_route_km:

    chunk_starts.append(
        current_distance
    )

    current_distance += (
        CHUNK_LENGTH_KM
    )


# ============================================================
# Output header
# ============================================================

print()
print("==========================================")
print(" VirtualRide - Full OSM Downloader")
print("==========================================")
print()

print(
    f"Route: {route_data['routeName']}"
)

print(
    f"Total route: {total_route_km:.3f} km"
)

print(
    f"Chunk length: "
    f"{CHUNK_LENGTH_KM:.1f} km"
)

print(
    f"Corridor: "
    f"{CORRIDOR_METERS:.0f} m"
)

print(
    f"Chunks: {len(chunk_starts)}"
)

print()


# ============================================================
# Prepare output directory
# ============================================================

OUTPUT_DIR.mkdir(
    parents=True,
    exist_ok=True
)


# ============================================================
# Process each chunk
# ============================================================

for chunk_index, chunk_start in enumerate(
    chunk_starts
):

    chunk_end = min(
        chunk_start + CHUNK_LENGTH_KM,
        total_route_km
    )


    # --------------------------------------------------------
    # Find points belonging to this chunk
    # --------------------------------------------------------

    chunk_points = [

        point

        for point in route_points

        if (
            chunk_start
            <= point["distanceFromStart"]
            <= chunk_end
        )

    ]


    if len(chunk_points) < 2:

        print(
            f"[{chunk_index:03d}] "
            f"Skipping - insufficient points."
        )

        continue


    # --------------------------------------------------------
    # Find bounding box
    # --------------------------------------------------------

    latitudes = [

        point["latitude"]

        for point in chunk_points

    ]

    longitudes = [

        point["longitude"]

        for point in chunk_points

    ]


    min_lat = min(latitudes)
    max_lat = max(latitudes)

    min_lon = min(longitudes)
    max_lon = max(longitudes)


    # --------------------------------------------------------
    # Add corridor around chunk
    # --------------------------------------------------------

    center_lat = (
        min_lat + max_lat
    ) / 2.0


    lat_margin = (
        CORRIDOR_METERS
        / meters_per_degree_lat()
    )


    lon_margin = (
        CORRIDOR_METERS
        / meters_per_degree_lon(
            center_lat
        )
    )


    south = min_lat - lat_margin
    west = min_lon - lon_margin
    north = max_lat + lat_margin
    east = max_lon + lon_margin


    # --------------------------------------------------------
    # Overpass bounding box
    # --------------------------------------------------------

    bbox = (
        f"{south},"
        f"{west},"
        f"{north},"
        f"{east}"
    )


    # --------------------------------------------------------
    # Query
    # --------------------------------------------------------

    query = f"""
[out:json][timeout:120];

(
    way["highway"]({bbox});

    way["waterway"]({bbox});

    way["building"]({bbox});

    way["landuse"]({bbox});

    way["natural"]({bbox});

    way["leisure"]({bbox});

    node["natural"]({bbox});

    node["place"]({bbox});

    node["tourism"]({bbox});
);

out body;

>;
out skel qt;
"""


    print(
        f"[{chunk_index:03d}] "
        f"{chunk_start:.1f} → "
        f"{chunk_end:.1f} km"
    )


    # --------------------------------------------------------
    # Request
    # --------------------------------------------------------

    encoded_query = urllib.parse.urlencode(
        {
            "data": query
        }
    ).encode(
        "utf-8"
    )


    request = urllib.request.Request(
        OVERPASS_URL,
        data=encoded_query,
        method="POST",
        headers={
            "User-Agent":
                "VirtualRide/1.0"
        }
    )


    try:

        with urllib.request.urlopen(
            request,
            timeout=180
        ) as response:

            response_data = (
                response.read()
            )


    except Exception as error:

        print(
            f"       ERROR: {error}"
        )

        print(
            "       Continuing to next chunk..."
        )

        continue


    # --------------------------------------------------------
    # Parse
    # --------------------------------------------------------

    try:

        osm_data = json.loads(
            response_data
        )

    except json.JSONDecodeError:

        print(
            "       ERROR: Invalid JSON."
        )

        continue


    elements = osm_data.get(
        "elements",
        []
    )


    # --------------------------------------------------------
    # Save
    # --------------------------------------------------------

    output_file = (
        OUTPUT_DIR
        / f"chunk_{chunk_index:03d}.json"
    )


    chunk_metadata = {

        "routeName":
            route_data["routeName"],

        "chunkIndex":
            chunk_index,

        "routeStartKm":
            chunk_start,

        "routeEndKm":
            chunk_end,

        "corridorMeters":
            CORRIDOR_METERS,

        "boundingBox": {

            "south": south,
            "west": west,
            "north": north,
            "east": east

        },

        "osm": osm_data

    }


    with open(
        output_file,
        "w",
        encoding="utf-8"
    ) as file:

        json.dump(
            chunk_metadata,
            file,
            indent=2
        )


    # --------------------------------------------------------
    # Statistics
    # --------------------------------------------------------

    size_mb = (
        output_file.stat().st_size
        / (1024 * 1024)
    )


    print(
        f"       OSM elements: "
        f"{len(elements)}"
    )

    print(
        f"       File size: "
        f"{size_mb:.2f} MB"
    )

    print()


    # --------------------------------------------------------
    # Be polite to Overpass
    # --------------------------------------------------------

    if (
        chunk_index
        < len(chunk_starts) - 1
    ):

        time.sleep(
            REQUEST_DELAY_SECONDS
        )


# ============================================================
# Finished
# ============================================================

print()
print("==========================================")
print(" Download process complete")
print("==========================================")
print()

print(
    "OSM chunks saved to:"
)

print(
    OUTPUT_DIR
)

print()