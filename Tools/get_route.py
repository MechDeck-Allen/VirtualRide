import json
import urllib.request
import urllib.parse
from pathlib import Path

# ============================================================
# VirtualRide - Alappuzha Route Generator
# ============================================================

# ------------------------------------------------------------
# TEMPORARY TEST ROUTE
# ------------------------------------------------------------
# Coordinates are in:
# (longitude, latitude)
#
# These are ONLY being used to verify that our
# Windows → Python → OSRM → GeoJSON pipeline works.
# We will replace them with the final ~10 km
# Alappuzha route later.
# ------------------------------------------------------------

START = (76.3388, 9.4981)
WAYPOINT = (76.3470, 9.5080)
FINISH = (76.3388, 9.4981)


# ------------------------------------------------------------
# Build OSRM coordinate string
# ------------------------------------------------------------

coordinates = ";".join([
    f"{lon},{lat}"
    for lon, lat in [
        START,
        WAYPOINT,
        FINISH
    ]
])


# ------------------------------------------------------------
# Build OSRM request
# ------------------------------------------------------------

base_url = (
    "https://router.project-osrm.org/"
    f"route/v1/bike/{coordinates}"
)

params = {
    "overview": "full",
    "geometries": "geojson",
    "steps": "true"
}

url = base_url + "?" + urllib.parse.urlencode(params)


print()
print("==========================================")
print(" VirtualRide - Route Generator")
print("==========================================")
print()
print("Requesting route...")
print(url)
print()


# ------------------------------------------------------------
# Request route
# ------------------------------------------------------------

try:

    with urllib.request.urlopen(url) as response:
        data = json.load(response)

except Exception as error:

    print("ERROR: Could not retrieve route.")
    print()
    print(error)
    print()

    raise SystemExit(1)


# ------------------------------------------------------------
# Check OSRM response
# ------------------------------------------------------------

if data.get("code") != "Ok":

    print("ERROR: Routing failed.")
    print()
    print(data)
    print()

    raise SystemExit(1)


# ------------------------------------------------------------
# Extract route
# ------------------------------------------------------------

route = data["routes"][0]

distance_m = route["distance"]
duration_s = route["duration"]

distance_km = distance_m / 1000
duration_min = duration_s / 60

geometry = route["geometry"]

points = geometry["coordinates"]


# ------------------------------------------------------------
# Display route information
# ------------------------------------------------------------

print("ROUTE GENERATED")
print("-------------------------")
print(f"Distance : {distance_km:.2f} km")
print(f"Duration : {duration_min:.1f} min")
print(f"Points   : {len(points)}")
print()


# ------------------------------------------------------------
# Determine project root
#
# get_route.py is located at:
#
# unity_virtualsim/
#       Tools/
#           get_route.py
#
# Therefore:
#
# parent       = Tools
# parent.parent = unity_virtualsim
# ------------------------------------------------------------

project_root = Path(__file__).resolve().parent.parent


# ------------------------------------------------------------
# Create output directory
# ------------------------------------------------------------

output_directory = (
    project_root
    / "routes"
    / "alappuzha"
)

output_directory.mkdir(
    parents=True,
    exist_ok=True
)


# ------------------------------------------------------------
# Output file
# ------------------------------------------------------------

output_path = (
    output_directory
    / "raw_route.json"
)


# ------------------------------------------------------------
# Prepare VirtualRide route data
# ------------------------------------------------------------

output = {

    "project": "VirtualRide",

    "routeName": "Alappuzha Test Route",

    "source": {
        "routing": "OSRM",
        "mapData": "OpenStreetMap"
    },

    "distanceMeters": distance_m,

    "durationSeconds": duration_s,

    "geometry": {
        "type": geometry["type"],
        "coordinates": points
    },

    "legs": route["legs"]

}


# ------------------------------------------------------------
# Save JSON
# ------------------------------------------------------------

try:

    with open(
        output_path,
        "w",
        encoding="utf-8"
    ) as file:

        json.dump(
            output,
            file,
            indent=2
        )

except Exception as error:

    print("ERROR: Could not save route file.")
    print()
    print(error)
    print()

    raise SystemExit(1)


# ------------------------------------------------------------
# Finished
# ------------------------------------------------------------

print("SUCCESS!")
print("-------------------------")
print(f"Saved to:")
print(output_path)
print()
print("Route pipeline completed successfully.")
print()