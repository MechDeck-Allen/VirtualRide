import xml.etree.ElementTree as ET
from pathlib import Path
import json
import math


# ============================================================
# VirtualRide - GPX → RouteData Processor
# ============================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent

INPUT_FILE = (
    PROJECT_ROOT
    / "routes"
    / "alappuzha"
    / "alappuzha_source.gpx"
)

OUTPUT_FILE = (
    PROJECT_ROOT
    / "routes"
    / "alappuzha"
    / "route_data.json"
)


# ------------------------------------------------------------
# Geographic distance
# ------------------------------------------------------------

def distance_m(lat1, lon1, lat2, lon2):

    R = 6371000.0

    lat1_rad = math.radians(lat1)
    lat2_rad = math.radians(lat2)

    dlat = math.radians(lat2 - lat1)
    dlon = math.radians(lon2 - lon1)

    a = (
        math.sin(dlat / 2) ** 2
        +
        math.cos(lat1_rad)
        * math.cos(lat2_rad)
        * math.sin(dlon / 2) ** 2
    )

    c = 2 * math.atan2(
        math.sqrt(a),
        math.sqrt(1 - a)
    )

    return R * c


# ------------------------------------------------------------
# Load GPX
# ------------------------------------------------------------

if not INPUT_FILE.exists():

    print("ERROR: GPX file not found:")
    print(INPUT_FILE)

    raise SystemExit(1)


print()
print("==========================================")
print(" VirtualRide - GPX Processor")
print("==========================================")
print()

print("Input:")
print(INPUT_FILE)
print()


tree = ET.parse(INPUT_FILE)
root = tree.getroot()


# ------------------------------------------------------------
# GPX namespace
# ------------------------------------------------------------

namespace = ""

if root.tag.startswith("{"):

    namespace = root.tag.split("}")[0] + "}"


# ------------------------------------------------------------
# Find track points
# ------------------------------------------------------------

trackpoints = root.findall(
    f".//{namespace}trkpt"
)


if not trackpoints:

    print("ERROR: No track points found.")

    raise SystemExit(1)


# ------------------------------------------------------------
# Extract points
# ------------------------------------------------------------

points = []


for trackpoint in trackpoints:

    latitude = float(
        trackpoint.attrib["lat"]
    )

    longitude = float(
        trackpoint.attrib["lon"]
    )


    elevation_element = trackpoint.find(
        f"{namespace}ele"
    )


    if elevation_element is not None:

        try:

            elevation = float(
                elevation_element.text
            )

        except (TypeError, ValueError):

            elevation = 0.0

    else:

        elevation = 0.0


    points.append({

        "latitude": latitude,

        "longitude": longitude,

        "elevation": elevation,

        "distanceFromStart": 0.0

    })


# ------------------------------------------------------------
# Calculate cumulative distance
# ------------------------------------------------------------

total_distance_m = 0.0


for i in range(1, len(points)):

    previous = points[i - 1]
    current = points[i]


    segment_distance = distance_m(

        previous["latitude"],
        previous["longitude"],

        current["latitude"],
        current["longitude"]

    )


    total_distance_m += segment_distance


    current["distanceFromStart"] = (
        total_distance_m / 1000.0
    )


# ------------------------------------------------------------
# Calculate statistics
# ------------------------------------------------------------

elevations = [

    point["elevation"]

    for point in points

]


minimum_elevation = min(
    elevations
)

maximum_elevation = max(
    elevations
)


# ------------------------------------------------------------
# Build RouteData
# ------------------------------------------------------------

route_data = {

    "version": 1,

    "routeName":
        "Kochi - Alappuzha - Kuttanad",

    "source":
        "Recorded GPX cycling track",

    "distanceKm":
        total_distance_m / 1000.0,

    "pointCount":
        len(points),

    "elevation": {

        "minimumMeters":
            minimum_elevation,

        "maximumMeters":
            maximum_elevation

    },

    "points":
        points

}


# ------------------------------------------------------------
# Save
# ------------------------------------------------------------

OUTPUT_FILE.parent.mkdir(
    parents=True,
    exist_ok=True
)


with open(
    OUTPUT_FILE,
    "w",
    encoding="utf-8"
) as file:

    json.dump(
        route_data,
        file,
        indent=2
    )


# ------------------------------------------------------------
# Output summary
# ------------------------------------------------------------

print("PROCESSING COMPLETE")
print("-------------------------")

print(
    f"Points       : {len(points)}"
)

print(
    f"Distance     : "
    f"{total_distance_m / 1000:.3f} km"
)

print(
    f"Elevation    : "
    f"{minimum_elevation:.1f} → "
    f"{maximum_elevation:.1f} m"
)

print()

print("Output:")
print(OUTPUT_FILE)

print()
print("RouteData.json created successfully.")
print()