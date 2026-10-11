import xml.etree.ElementTree as ET
from pathlib import Path
import json
import webbrowser


# ============================================================
# VirtualRide - Offline GPX Route Viewer
# ============================================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent

GPX_FILE = (
    PROJECT_ROOT
    / "routes"
    / "alappuzha"
    / "alappuzha_source.gpx"
)

OUTPUT_FILE = (
    PROJECT_ROOT
    / "routes"
    / "alappuzha"
    / "gpx_preview.html"
)


# ------------------------------------------------------------
# Check GPX
# ------------------------------------------------------------

if not GPX_FILE.exists():

    print("ERROR: GPX file not found.")
    print(GPX_FILE)

    raise SystemExit(1)


# ------------------------------------------------------------
# Parse GPX
# ------------------------------------------------------------

tree = ET.parse(GPX_FILE)
root = tree.getroot()

namespace = ""

if root.tag.startswith("{"):
    namespace = root.tag.split("}")[0] + "}"


trackpoints = root.findall(
    f".//{namespace}trkpt"
)


if not trackpoints:

    print("ERROR: No track points found.")

    raise SystemExit(1)


points = []

for point in trackpoints:

    lat = float(point.attrib["lat"])
    lon = float(point.attrib["lon"])

    points.append({
        "lat": lat,
        "lon": lon
    })


# ------------------------------------------------------------
# Map center
# ------------------------------------------------------------

center_lat = sum(
    p["lat"] for p in points
) / len(points)

center_lon = sum(
    p["lon"] for p in points
) / len(points)


# ------------------------------------------------------------
# Convert points to JavaScript
# ------------------------------------------------------------

js_points = json.dumps([
    [p["lat"], p["lon"]]
    for p in points
])


# ------------------------------------------------------------
# Create HTML
# ------------------------------------------------------------

html = f"""
<!DOCTYPE html>

<html>

<head>

<meta charset="UTF-8">

<title>VirtualRide - Alappuzha GPX</title>

<link
rel="stylesheet"
href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"
/>

<style>

html, body {{
    height: 100%;
    margin: 0;
}}

#map {{
    width: 100%;
    height: 100%;
}}

</style>

</head>

<body>

<div id="map"></div>

<script
src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js">
</script>

<script>

const points = {js_points};

const map = L.map('map');

L.tileLayer(
    'https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png',
    {{
        attribution: '&copy; OpenStreetMap contributors'
    }}
).addTo(map);


const route = L.polyline(
    points,
    {{
        weight: 5
    }}
).addTo(map);


L.marker(points[0])
    .addTo(map)
    .bindPopup("START")
    .openPopup();


L.marker(points[points.length - 1])
    .addTo(map)
    .bindPopup("FINISH");


map.fitBounds(route.getBounds());

</script>

</body>

</html>
"""


# ------------------------------------------------------------
# Save
# ------------------------------------------------------------

with open(
    OUTPUT_FILE,
    "w",
    encoding="utf-8"
) as file:

    file.write(html)


print()
print("==========================================")
print(" VirtualRide - GPX Viewer")
print("==========================================")
print()

print(f"Points : {len(points)}")

print()
print("Created:")
print(OUTPUT_FILE)

print()
print("Opening map in browser...")


# ------------------------------------------------------------
# Open browser
# ------------------------------------------------------------

webbrowser.open(
    OUTPUT_FILE.resolve().as_uri()
)