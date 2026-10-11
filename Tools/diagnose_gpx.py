import xml.etree.ElementTree as ET
from pathlib import Path
import math


# ============================================================
# VirtualRide - GPX Diagnostic Tool
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

    R = 6371000.0

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
# Find tracks
# ------------------------------------------------------------

tracks = root.findall(
    f"./{namespace}trk"
)


print()
print("==========================================")
print(" VirtualRide - GPX Diagnostic")
print("==========================================")
print()

print(f"GPX file:")
print(GPX_FILE)
print()

print(f"Tracks found: {len(tracks)}")
print()


# ------------------------------------------------------------
# Diagnose each track
# ------------------------------------------------------------

grand_total = 0.0

all_points = []


for track_index, track in enumerate(tracks):

    name_element = track.find(
        f"{namespace}name"
    )

    if name_element is not None:
        track_name = name_element.text
    else:
        track_name = "(no name)"


    segments = track.findall(
        f"./{namespace}trkseg"
    )


    print("------------------------------------------")
    print(f"TRACK {track_index + 1}")
    print("------------------------------------------")

    print(f"Name: {track_name}")
    print(f"Segments: {len(segments)}")
    print()


    track_total = 0.0


    for segment_index, segment in enumerate(segments):

        points = segment.findall(
            f"./{namespace}trkpt"
        )


        segment_distance = 0.0


        previous = None


        for point in points:

            lat = float(point.attrib["lat"])
            lon = float(point.attrib["lon"])


            all_points.append(
                (lat, lon)
            )


            if previous is not None:

                segment_distance += distance_m(
                    previous[0],
                    previous[1],
                    lat,
                    lon
                )


            previous = (
                lat,
                lon
            )


        track_total += segment_distance


        print(
            f"Segment {segment_index + 1}: "
            f"{len(points)} points | "
            f"{segment_distance / 1000:.3f} km"
        )


    print()

    print(
        f"TRACK TOTAL: "
        f"{track_total / 1000:.3f} km"
    )

    print()

    grand_total += track_total


# ------------------------------------------------------------
# Overall information
# ------------------------------------------------------------

print("==========================================")
print(" OVERALL")
print("==========================================")
print()

print(
    f"Total points: {len(all_points)}"
)

print(
    f"Calculated track distance: "
    f"{grand_total / 1000:.3f} km"
)

print()


# ------------------------------------------------------------
# First and last points
# ------------------------------------------------------------

if all_points:

    first = all_points[0]
    last = all_points[-1]


    print("FIRST POINT")
    print("------------------------------------------")

    print(
        f"Latitude : {first[0]:.6f}"
    )

    print(
        f"Longitude: {first[1]:.6f}"
    )

    print()


    print("LAST POINT")
    print("------------------------------------------")

    print(
        f"Latitude : {last[0]:.6f}"
    )

    print(
        f"Longitude: {last[1]:.6f}"
    )

    print()


    straight_line = distance_m(
        first[0],
        first[1],
        last[0],
        last[1]
    )


    print(
        f"Straight-line distance: "
        f"{straight_line / 1000:.3f} km"
    )

    print()


# ------------------------------------------------------------
# Plausibility check
# ------------------------------------------------------------

if grand_total > 0:

    ratio = straight_line / grand_total

    print("PLAUSIBILITY CHECK")
    print("------------------------------------------")

    print(
        f"Straight-line / route ratio: "
        f"{ratio:.3f}"
    )

    if straight_line > grand_total:

        print()
        print(
            "WARNING:"
        )

        print(
            "The straight-line distance is greater "
            "than the calculated route distance."
        )

        print(
            "This indicates a GPX parsing/order issue."
        )

    else:

        print(
            "Distance calculation appears plausible."
        )


print()
print("Diagnostic complete.")
print()