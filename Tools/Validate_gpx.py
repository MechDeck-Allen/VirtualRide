import xml.etree.ElementTree as ET
from pathlib import Path
import math


# ============================================================
# VirtualRide - GPX Validation
# ============================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent

GPX_FILE = (
    PROJECT_ROOT
    / "routes"
    / "alappuzha"
    / "alappuzha_source.gpx"
)


# ------------------------------------------------------------
# Simple geographic distance
# ------------------------------------------------------------

def distance_km(lat1, lon1, lat2, lon2):

    # Approximate conversion at Kerala's latitude.
    #
    # 1 degree latitude  ≈ 111.2 km
    # 1 degree longitude ≈ 109.5 km around Kerala

    lat_km = (lat2 - lat1) * 111.2

    average_lat = math.radians(
        (lat1 + lat2) / 2
    )

    lon_km = (
        (lon2 - lon1)
        * 111.2
        * math.cos(average_lat)
    )

    return math.sqrt(
        lat_km ** 2 +
        lon_km ** 2
    )


# ------------------------------------------------------------
# Load GPX
# ------------------------------------------------------------

if not GPX_FILE.exists():

    print("ERROR: GPX file not found:")
    print(GPX_FILE)

    raise SystemExit(1)


tree = ET.parse(GPX_FILE)
root = tree.getroot()


# ------------------------------------------------------------
# Namespace
# ------------------------------------------------------------

namespace = ""

if root.tag.startswith("{"):
    namespace = root.tag.split("}")[0] + "}"


# ------------------------------------------------------------
# Get track points
# ------------------------------------------------------------

trackpoints = root.findall(
    f".//{namespace}trkpt"
)


if not trackpoints:

    print("ERROR: No GPX track points found.")

    raise SystemExit(1)


points = []

for point in trackpoints:

    lat = float(point.attrib["lat"])
    lon = float(point.attrib["lon"])

    points.append(
        (lat, lon)
    )


# ------------------------------------------------------------
# Calculate total track distance
# ------------------------------------------------------------

total_km = 0.0

largest_jump = 0.0
largest_jump_index = 0


for i in range(1, len(points)):

    previous = points[i - 1]
    current = points[i]

    segment_km = distance_km(
        previous[0],
        previous[1],
        current[0],
        current[1]
    )

    total_km += segment_km

    if segment_km > largest_jump:

        largest_jump = segment_km
        largest_jump_index = i


# ------------------------------------------------------------
# Start / finish
# ------------------------------------------------------------

start = points[0]
finish = points[-1]


straight_line_km = distance_km(
    start[0],
    start[1],
    finish[0],
    finish[1]
)


# ------------------------------------------------------------
# Bounding box
# ------------------------------------------------------------

latitudes = [
    p[0] for p in points
]

longitudes = [
    p[1] for p in points
]


min_lat = min(latitudes)
max_lat = max(latitudes)

min_lon = min(longitudes)
max_lon = max(longitudes)


# ------------------------------------------------------------
# Output
# ------------------------------------------------------------

print()
print("==========================================")
print(" VirtualRide - GPX Validation")
print("==========================================")
print()

print("TRACK")
print("------------------------------------------")
print(f"Points              : {len(points)}")
print(f"Calculated distance : {total_km:.3f} km")
print()


print("START")
print("------------------------------------------")
print(f"Latitude  : {start[0]:.6f}")
print(f"Longitude : {start[1]:.6f}")
print()


print("FINISH")
print("------------------------------------------")
print(f"Latitude  : {finish[0]:.6f}")
print(f"Longitude : {finish[1]:.6f}")
print()


print("START → FINISH")
print("------------------------------------------")
print(
    f"Straight-line distance : "
    f"{straight_line_km:.3f} km"
)
print()


print("BOUNDING BOX")
print("------------------------------------------")
print(f"Latitude  : {min_lat:.6f} → {max_lat:.6f}")
print(f"Longitude : {min_lon:.6f} → {max_lon:.6f}")
print()


print("LARGEST SINGLE GPS JUMP")
print("------------------------------------------")
print(
    f"Distance : {largest_jump:.3f} km"
)
print(
    f"Between points: "
    f"{largest_jump_index - 1} → "
    f"{largest_jump_index}"
)
print()


print("PLAUSIBILITY")
print("------------------------------------------")

if total_km >= straight_line_km:

    print(
        "PASS: Route distance is greater than "
        "straight-line distance."
    )

else:

    print(
        "FAIL: Route distance is smaller than "
        "straight-line distance."
    )


print()
print("Validation complete.")
print()