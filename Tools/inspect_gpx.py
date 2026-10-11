import xml.etree.ElementTree as ET
from pathlib import Path
import math


# ============================================================
# VirtualRide - GPX Inspector
# ============================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent

GPX_FILE = (
    PROJECT_ROOT
    / "routes"
    / "alappuzha"
    / "alappuzha_source.gpx"
)


# ------------------------------------------------------------
# Haversine distance
# ------------------------------------------------------------

def distance_m(lat1, lon1, lat2, lon2):

    R = 6371000

    lat1 = math.radians(lat1)
    lat2 = math.radians(lat2)

    dlat = math.radians(lat2 - lat1)
    dlon = math.radians(lon2 - lon1)

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


# ------------------------------------------------------------
# Load GPX
# ------------------------------------------------------------

if not GPX_FILE.exists():

    print()
    print("ERROR: GPX file not found.")
    print()
    print("Expected:")
    print(GPX_FILE)
    print()

    raise SystemExit(1)


print()
print("==========================================")
print(" VirtualRide - GPX Inspector")
print("==========================================")
print()

print("Loading:")
print(GPX_FILE)
print()


tree = ET.parse(GPX_FILE)
root = tree.getroot()


# ------------------------------------------------------------
# Handle GPX namespaces
# ------------------------------------------------------------

namespace = ""

if root.tag.startswith("{"):

    namespace = root.tag.split("}")[0] + "}"


# ------------------------------------------------------------
# Extract track points
# ------------------------------------------------------------

trackpoints = root.findall(
    f".//{namespace}trkpt"
)


if not trackpoints:

    print("ERROR: No track points found in GPX.")
    raise SystemExit(1)


points = []


for point in trackpoints:

    lat = float(point.attrib["lat"])
    lon = float(point.attrib["lon"])

    elevation_element = point.find(
        f"{namespace}ele"
    )

    if elevation_element is not None:

        try:
            elevation = float(
                elevation_element.text
            )

        except (TypeError, ValueError):

            elevation = None

    else:

        elevation = None


    points.append({
        "lat": lat,
        "lon": lon,
        "elevation": elevation
    })


# ------------------------------------------------------------
# Calculate distance
# ------------------------------------------------------------

total_distance = 0.0


for i in range(1, len(points)):

    previous = points[i - 1]
    current = points[i]

    total_distance += distance_m(
        previous["lat"],
        previous["lon"],
        current["lat"],
        current["lon"]
    )


# ------------------------------------------------------------
# Elevation information
# ------------------------------------------------------------

elevations = [
    p["elevation"]
    for p in points
    if p["elevation"] is not None
]


print("TRACK INFORMATION")
print("-------------------------")

print(
    f"Track points : {len(points)}"
)

print(
    f"Distance     : {total_distance / 1000:.2f} km"
)

print()

print("START")
print("-------------------------")

print(
    f"Latitude  : {points[0]['lat']:.6f}"
)

print(
    f"Longitude : {points[0]['lon']:.6f}"
)

print()

print("FINISH")
print("-------------------------")

print(
    f"Latitude  : {points[-1]['lat']:.6f}"
)

print(
    f"Longitude : {points[-1]['lon']:.6f}"
)

print()


if elevations:

    print("ELEVATION")
    print("-------------------------")

    print(
        f"Minimum : {min(elevations):.1f} m"
    )

    print(
        f"Maximum : {max(elevations):.1f} m"
    )

    print(
        f"Range   : "
        f"{max(elevations) - min(elevations):.1f} m"
    )

else:

    print("ELEVATION")
    print("-------------------------")
    print("No elevation data found.")


print()
print("GPX inspection complete.")
print()