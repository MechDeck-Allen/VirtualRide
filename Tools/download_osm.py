import json
import math
import urllib.request
import urllib.parse
from pathlib import Path


# ============================================================
# VirtualRide - OpenStreetMap Environment Downloader
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

OUTPUT_FILE = (
    OUTPUT_DIR
    / "alappuzha_osm_test.json"
)


# ============================================================
# SETTINGS
# ============================================================

# IMPORTANT:
# Start with only the first 5 km.
#
# Once we verify that the returned data is useful,
# we will increase this to the complete route.

TEST_DISTANCE_KM = 5.0

# Environment corridor around the cycling route.
#
# 300 metres on either side for the first test.

CORRIDOR_METERS = 300.0

# Public Overpass endpoint.

OVERPASS_URL = (
    "https://overpass-api.de/api/interpreter"
)


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


def distance_meters(
    lat1,
    lon1,
    lat2,
    lon2
):

    R = 6371000.0

    lat1 = math.radians(lat1)
    lat2 = math.radians(lat2)

    dlat = math.radians(
        lat2 - lat1
    )

    dlon = math.radians(
        lon2 - lon1
    )

    a = (
        math.sin(dlat / 2) ** 2
        +
        math.cos(lat1)
        * math.cos(lat2)
        * math.sin(dlon / 2) ** 2
    )

    c = 2 * math.atan2(
        math.sqrt(a),
        math.sqrt(1 - a)
    )

    return R * c


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


# ============================================================
# Select first section of route
# ============================================================

selected_points = []


for point in route_points:

    selected_points.append(point)

    if (
        point["distanceFromStart"]
        >= TEST_DISTANCE_KM
    ):
        break


if len(selected_points) < 2:

    print(
        "ERROR: Not enough route points."
    )

    raise SystemExit(1)


actual_distance = selected_points[-1][
    "distanceFromStart"
]


print()
print("==========================================")
print(" VirtualRide - OSM Downloader")
print("==========================================")
print()

print(
    f"Route: {route_data['routeName']}"
)

print(
    f"Route section: "
    f"0 → {actual_distance:.2f} km"
)

print(
    f"Corridor: "
    f"{CORRIDOR_METERS:.0f} m"
)

print()


# ============================================================
# Create bounding boxes along route
# ============================================================

boxes = []


# We don't create one enormous bbox.
# Instead, divide the route into sections.

STEP_KM = 1.0


current_distance = 0.0


while current_distance <= actual_distance:

    closest = min(
        selected_points,
        key=lambda p:
        abs(
            p["distanceFromStart"]
            - current_distance
        )
    )

    lat = closest["latitude"]
    lon = closest["longitude"]


    lat_delta = (
        CORRIDOR_METERS
        / meters_per_degree_lat()
    )

    lon_delta = (
        CORRIDOR_METERS
        / meters_per_degree_lon(lat)
    )


    south = lat - lat_delta
    west = lon - lon_delta
    north = lat + lat_delta
    east = lon + lon_delta


    boxes.append(
        (
            south,
            west,
            north,
            east
        )
    )


    current_distance += STEP_KM


# ============================================================
# Merge overlapping boxes
# ============================================================

min_lat = min(
    box[0]
    for box in boxes
)

min_lon = min(
    box[1]
    for box in boxes
)

max_lat = max(
    box[2]
    for box in boxes
)

max_lon = max(
    box[3]
    for box in boxes
)


print("TEST BOUNDING AREA")
print("-------------------------")

print(
    f"South : {min_lat:.6f}"
)

print(
    f"West  : {min_lon:.6f}"
)

print(
    f"North : {max_lat:.6f}"
)

print(
    f"East  : {max_lon:.6f}"
)

print()


# ============================================================
# Build Overpass query
# ============================================================

bbox = (
    f"{min_lat},"
    f"{min_lon},"
    f"{max_lat},"
    f"{max_lon}"
)


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


print("REQUEST")
print("-------------------------")

print(
    "Querying OpenStreetMap through Overpass..."
)

print()


# ============================================================
# Send request
# ============================================================

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

        response_data = response.read()


except Exception as error:

    print()
    print("ERROR contacting Overpass.")
    print(error)
    print()

    raise SystemExit(1)


# ============================================================
# Parse response
# ============================================================

try:

    osm_data = json.loads(
        response_data
    )

except json.JSONDecodeError:

    print(
        "ERROR: Overpass returned "
        "invalid JSON."
    )

    raise SystemExit(1)


elements = osm_data.get(
    "elements",
    []
)


# ============================================================
# Count features
# ============================================================

node_count = 0
way_count = 0

highway_count = 0
waterway_count = 0
building_count = 0
landuse_count = 0
natural_count = 0
leisure_count = 0


for element in elements:

    tags = element.get(
        "tags",
        {}
    )

    if element["type"] == "node":

        node_count += 1

    elif element["type"] == "way":

        way_count += 1


    if "highway" in tags:
        highway_count += 1

    if "waterway" in tags:
        waterway_count += 1

    if "building" in tags:
        building_count += 1

    if "landuse" in tags:
        landuse_count += 1

    if "natural" in tags:
        natural_count += 1

    if "leisure" in tags:
        leisure_count += 1


# ============================================================
# Save raw OSM data
# ============================================================

OUTPUT_DIR.mkdir(
    parents=True,
    exist_ok=True
)


with open(
    OUTPUT_FILE,
    "w",
    encoding="utf-8"
) as file:

    json.dump(
        osm_data,
        file,
        indent=2
    )


# ============================================================
# Results
# ============================================================

file_size_mb = (
    OUTPUT_FILE.stat().st_size
    / (1024 * 1024)
)


print()
print("==========================================")
print(" OSM DOWNLOAD COMPLETE")
print("==========================================")
print()

print(
    f"OSM elements : "
    f"{len(elements)}"
)

print(
    f"Nodes        : "
    f"{node_count}"
)

print(
    f"Ways         : "
    f"{way_count}"
)

print()

print("FEATURES")
print("-------------------------")

print(
    f"Highways     : "
    f"{highway_count}"
)

print(
    f"Waterways    : "
    f"{waterway_count}"
)

print(
    f"Buildings    : "
    f"{building_count}"
)

print(
    f"Landuse      : "
    f"{landuse_count}"
)

print(
    f"Natural      : "
    f"{natural_count}"
)

print(
    f"Leisure      : "
    f"{leisure_count}"
)

print()

print(
    f"File size    : "
    f"{file_size_mb:.2f} MB"
)

print()

print("Saved:")
print(OUTPUT_FILE)

print()