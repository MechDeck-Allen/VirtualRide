import json
import math
import urllib.request
import urllib.parse
import time
from pathlib import Path


# ============================================================
# VirtualRide - Retry Missing OSM Chunks
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
# Settings
# ============================================================

CHUNK_LENGTH_KM = 1.0

CORRIDOR_METERS = 300.0

MAX_RETRIES = 4

DELAY_BETWEEN_CHUNKS = 15

DELAY_AFTER_ERROR = 60


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

with open(
    ROUTE_FILE,
    "r",
    encoding="utf-8"
) as file:

    route_data = json.load(file)


route_points = route_data["points"]

total_route_km = route_data["distanceKm"]


# ============================================================
# Determine chunks
# ============================================================

total_chunks = math.ceil(
    total_route_km / CHUNK_LENGTH_KM
)


missing_chunks = []


for chunk_index in range(total_chunks):

    output_file = (
        OUTPUT_DIR
        / f"chunk_{chunk_index:03d}.json"
    )

    if not output_file.exists():

        missing_chunks.append(
            chunk_index
        )


# ============================================================
# Header
# ============================================================

print()
print("==========================================")
print(" VirtualRide - OSM Retry Tool")
print("==========================================")
print()

print(
    f"Total chunks : {total_chunks}"
)

print(
    f"Missing      : {len(missing_chunks)}"
)

print()

if not missing_chunks:

    print(
        "All chunks already exist."
    )

    print()

    raise SystemExit(0)


print(
    "Missing chunks:"
)

print(
    ", ".join(
        f"{x:03d}"
        for x in missing_chunks
    )
)

print()


# ============================================================
# Download function
# ============================================================

def download_chunk(chunk_index):

    chunk_start = (
        chunk_index
        * CHUNK_LENGTH_KM
    )

    chunk_end = min(
        chunk_start + CHUNK_LENGTH_KM,
        total_route_km
    )


    # --------------------------------------------------------
    # Route points in this chunk
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
            "       Not enough route points."
        )

        return False


    # --------------------------------------------------------
    # Bounding box
    # --------------------------------------------------------

    latitudes = [
        p["latitude"]
        for p in chunk_points
    ]

    longitudes = [
        p["longitude"]
        for p in chunk_points
    ]


    min_lat = min(latitudes)
    max_lat = max(latitudes)

    min_lon = min(longitudes)
    max_lon = max(longitudes)


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


    bbox = (
        f"{south},"
        f"{west},"
        f"{north},"
        f"{east}"
    )


    # --------------------------------------------------------
    # OSM query
    # --------------------------------------------------------

    query = f"""
[out:json][timeout:180];

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


    encoded_query = urllib.parse.urlencode(
        {
            "data": query
        }
    ).encode("utf-8")


    request = urllib.request.Request(
        OVERPASS_URL,
        data=encoded_query,
        method="POST",
        headers={
            "User-Agent":
                "VirtualRide/1.0"
        }
    )


    # --------------------------------------------------------
    # Retry
    # --------------------------------------------------------

    for attempt in range(
        1,
        MAX_RETRIES + 1
    ):

        try:

            print(
                f"       Attempt "
                f"{attempt}/{MAX_RETRIES}"
            )


            with urllib.request.urlopen(
                request,
                timeout=240
            ) as response:

                response_data = (
                    response.read()
                )


            osm_data = json.loads(
                response_data
            )


            elements = osm_data.get(
                "elements",
                []
            )


            output_file = (
                OUTPUT_DIR
                / f"chunk_{chunk_index:03d}.json"
            )


            metadata = {

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
                    metadata,
                    file,
                    indent=2
                )


            size_mb = (
                output_file.stat().st_size
                / (1024 * 1024)
            )


            print(
                f"       SUCCESS"
            )

            print(
                f"       Elements: "
                f"{len(elements)}"
            )

            print(
                f"       Size: "
                f"{size_mb:.2f} MB"
            )


            return True


        except Exception as error:

            print(
                f"       ERROR: {error}"
            )


            if attempt < MAX_RETRIES:

                print(
                    f"       Waiting "
                    f"{DELAY_AFTER_ERROR}s..."
                )

                time.sleep(
                    DELAY_AFTER_ERROR
                )


    return False


# ============================================================
# Process missing chunks
# ============================================================

successful = []
failed = []


for number, chunk_index in enumerate(
    missing_chunks,
    start=1
):

    print()
    print(
        "------------------------------------------"
    )

    print(
        f"Missing chunk "
        f"{number}/{len(missing_chunks)}"
    )

    print(
        f"Chunk: {chunk_index:03d}"
    )

    print(
        f"Route: "
        f"{chunk_index:.1f} → "
        f"{min(chunk_index + 1, total_route_km):.1f} km"
    )

    print()


    success = download_chunk(
        chunk_index
    )


    if success:

        successful.append(
            chunk_index
        )

    else:

        failed.append(
            chunk_index
        )


    if number < len(missing_chunks):

        print(
            f"Waiting "
            f"{DELAY_BETWEEN_CHUNKS}s "
            f"before next request..."
        )

        time.sleep(
            DELAY_BETWEEN_CHUNKS
        )


# ============================================================
# Summary
# ============================================================

print()
print("==========================================")
print(" RETRY COMPLETE")
print("==========================================")
print()

print(
    f"Successful : {len(successful)}"
)

print(
    f"Still failed: {len(failed)}"
)

print()


if failed:

    print(
        "Still missing:"
    )

    print(
        ", ".join(
            f"{x:03d}"
            for x in failed
        )
    )

else:

    print(
        "ALL OSM CHUNKS ARE PRESENT."
    )


print()