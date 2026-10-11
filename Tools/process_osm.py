import json
from pathlib import Path


# ============================================================
# VirtualRide - OSM -> World Data Processor
# ============================================================
#
# Input:
#   world_data/alappuzha/osm/chunk_*.json
#
# Output:
#   world_data/alappuzha/processed/chunk_*.json
#
# Existing data is preserved.
# Added roadside data:
#   - trees
#   - utilityPoles
#   - streetLamps
#   - barriers
#   - bridges
#
# ============================================================


PROJECT_ROOT = Path(__file__).resolve().parent.parent

INPUT_DIR = (
    PROJECT_ROOT
    / "world_data"
    / "alappuzha"
    / "osm"
)

OUTPUT_DIR = (
    PROJECT_ROOT
    / "world_data"
    / "alappuzha"
    / "processed"
)


HIGHWAY_TYPES = {
    "motorway",
    "motorway_link",
    "trunk",
    "trunk_link",
    "primary",
    "primary_link",
    "secondary",
    "secondary_link",
    "tertiary",
    "tertiary_link",
    "unclassified",
    "residential",
    "living_street",
    "service",
    "track",
    "path",
    "footway",
    "pedestrian",
    "cycleway",
}


WATERWAY_TYPES = {
    "river",
    "canal",
    "stream",
    "drain",
    "ditch",
}


LANDUSE_TYPES = {
    "residential",
    "commercial",
    "industrial",
    "farmland",
    "farmyard",
    "forest",
    "grass",
    "meadow",
    "recreation_ground",
    "cemetery",
    "religious",
    "retail",
    "orchard",
    "vineyard",
    "allotments",
    "greenfield",
    "brownfield",
}


NATURAL_TYPES = {
    "water",
    "wood",
    "wetland",
    "beach",
    "scrub",
    "grassland",
    "sand",
    "heath",
    "shrubbery",
}


BARRIER_TYPES = {
    "fence",
    "wall",
    "hedge",
    "guard_rail",
    "handrail",
    "retaining_wall",
    "ditch",
}


OUTPUT_DIR.mkdir(
    parents=True,
    exist_ok=True,
)


chunk_files = sorted(
    INPUT_DIR.glob("chunk_*.json")
)


if not chunk_files:
    print()
    print("ERROR: No OSM chunks found.")
    print(INPUT_DIR)
    print()
    raise SystemExit(1)


print()
print("==========================================")
print(" VirtualRide - OSM Processor")
print("==========================================")
print()
print(f"Input directory : {INPUT_DIR}")
print(f"Output directory: {OUTPUT_DIR}")
print(f"OSM chunks found: {len(chunk_files)}")
print()


total_roads = 0
total_waterways = 0
total_buildings = 0
total_landuse = 0
total_natural = 0
total_trees = 0
total_utility_poles = 0
total_street_lamps = 0
total_barriers = 0
total_bridges = 0


for chunk_file in chunk_files:

    print("------------------------------------------")
    print(f"Processing: {chunk_file.name}")

    try:
        with open(
            chunk_file,
            "r",
            encoding="utf-8",
        ) as file:
            chunk_data = json.load(file)
    except Exception as error:
        print(f"ERROR reading {chunk_file.name}: {error}")
        continue


    osm_data = chunk_data.get(
        "osm",
        {},
    )

    elements = osm_data.get(
        "elements",
        [],
    )

    if not isinstance(elements, list):
        print("WARNING: osm.elements is not a list.")
        elements = []


    nodes = {}

    for element in elements:
        if element.get("type") != "node":
            continue

        node_id = element.get("id")
        latitude = element.get("lat")
        longitude = element.get("lon")

        if (
            node_id is None
            or latitude is None
            or longitude is None
        ):
            continue

        nodes[node_id] = (
            latitude,
            longitude,
        )


    roads = []
    waterways = []
    buildings = []
    landuse = []
    natural_features = []
    trees = []
    utility_poles = []
    street_lamps = []
    barriers = []
    bridges = []


    # ========================================================
    # NODE FEATURES
    # ========================================================

    for element in elements:

        if element.get("type") != "node":
            continue

        node_id = element.get("id")

        if node_id not in nodes:
            continue

        tags = element.get(
            "tags",
            {},
        )

        if not isinstance(tags, dict):
            continue

        latitude, longitude = nodes[node_id]


        # ----------------------------------------------------
        # Tree
        # ----------------------------------------------------

        if tags.get("natural") == "tree":

            tree = {
                "type": "tree",
                "latitude": latitude,
                "longitude": longitude,
            }

            if "species" in tags:
                tree["species"] = tags["species"]

            if "genus" in tags:
                tree["genus"] = tags["genus"]

            if "height" in tags:
                tree["height"] = tags["height"]

            if "diameter_crown" in tags:
                tree["crownDiameter"] = tags["diameter_crown"]

            trees.append(tree)
            total_trees += 1


        # ----------------------------------------------------
        # Utility pole
        # ----------------------------------------------------

        if tags.get("power") == "pole":

            utility_poles.append(
                {
                    "type": "power_pole",
                    "latitude": latitude,
                    "longitude": longitude,
                }
            )

            total_utility_poles += 1


        # ----------------------------------------------------
        # Street lamp
        # ----------------------------------------------------

        if tags.get("highway") == "street_lamp":

            street_lamps.append(
                {
                    "type": "street_lamp",
                    "latitude": latitude,
                    "longitude": longitude,
                }
            )

            total_street_lamps += 1


    # ========================================================
    # WAY FEATURES
    # ========================================================

    for element in elements:

        if element.get("type") != "way":
            continue

        tags = element.get(
            "tags",
            {},
        )

        if not isinstance(tags, dict):
            tags = {}

        node_ids = element.get(
            "nodes",
            [],
        )

        if not isinstance(node_ids, list):
            continue

        coordinates = []

        for node_id in node_ids:

            if node_id not in nodes:
                continue

            latitude, longitude = nodes[node_id]

            coordinates.append(
                {
                    "latitude": latitude,
                    "longitude": longitude,
                }
            )

        if len(coordinates) < 2:
            continue


        # ----------------------------------------------------
        # Highway
        # ----------------------------------------------------

        highway_type = tags.get(
            "highway"
        )

        if highway_type in HIGHWAY_TYPES:

            road = {
                "type": highway_type,
                "points": coordinates,
            }

            # Preserve bridge information for roadside detail.
            if (
                tags.get("bridge") not in (None, "", "no")
            ):
                road["bridge"] = True

                bridges.append(
                    {
                        "type": "bridge",
                        "highway": highway_type,
                        "points": coordinates,
                    }
                )

                total_bridges += 1

            roads.append(road)
            total_roads += 1


        # ----------------------------------------------------
        # Waterway
        # ----------------------------------------------------

        waterway_type = tags.get(
            "waterway"
        )

        if waterway_type in WATERWAY_TYPES:

            waterways.append(
                {
                    "type": waterway_type,
                    "points": coordinates,
                }
            )

            total_waterways += 1


        # ----------------------------------------------------
        # Building
        # ----------------------------------------------------

        if "building" in tags:

            buildings.append(
                {
                    "type": tags.get(
                        "building",
                        "yes",
                    ),
                    "points": coordinates,
                }
            )

            total_buildings += 1


        # ----------------------------------------------------
        # Landuse
        # ----------------------------------------------------

        landuse_type = tags.get(
            "landuse"
        )

        if landuse_type in LANDUSE_TYPES:

            landuse.append(
                {
                    "type": landuse_type,
                    "points": coordinates,
                }
            )

            total_landuse += 1


        # ----------------------------------------------------
        # Natural
        # ----------------------------------------------------

        natural_type = tags.get(
            "natural"
        )

        if natural_type in NATURAL_TYPES:

            natural_features.append(
                {
                    "type": natural_type,
                    "points": coordinates,
                }
            )

            total_natural += 1


        # ----------------------------------------------------
        # Barrier / wall / fence
        # ----------------------------------------------------

        barrier_type = tags.get(
            "barrier"
        )

        if barrier_type in BARRIER_TYPES:

            barriers.append(
                {
                    "type": barrier_type,
                    "points": coordinates,
                }
            )

            total_barriers += 1


    processed = {
        "version": 4,

        "chunkIndex": chunk_data.get(
            "chunkIndex",
            -1,
        ),

        "routeStartKm": chunk_data.get(
            "routeStartKm",
            0,
        ),

        "routeEndKm": chunk_data.get(
            "routeEndKm",
            0,
        ),

        # Existing fields.
        "roads": roads,
        "waterways": waterways,
        "buildings": buildings,
        "landuse": landuse,

        # Keep BOTH names for compatibility with the existing
        # Unity OSMWorldGenerator. Older processed files used
        # "natural", while the current Unity generator reads
        # "naturalWater".
        "natural": natural_features,
        "naturalWater": natural_features,

        # New roadside fields.
        "trees": trees,
        "utilityPoles": utility_poles,
        "streetLamps": street_lamps,
        "barriers": barriers,
        "bridges": bridges,
    }


    output_file = (
        OUTPUT_DIR
        / chunk_file.name
    )


    try:
        with open(
            output_file,
            "w",
            encoding="utf-8",
        ) as file:

            json.dump(
                processed,
                file,
                separators=(
                    ",",
                    ":",
                ),
            )

    except Exception as error:
        print(
            f"ERROR writing {output_file}: {error}"
        )
        continue


    print(f"   Roads          : {len(roads)}")
    print(f"   Waterways      : {len(waterways)}")
    print(f"   Buildings      : {len(buildings)}")
    print(f"   Landuse        : {len(landuse)}")
    print(f"   Natural        : {len(natural_features)}")
    print(f"   Trees          : {len(trees)}")
    print(f"   Utility poles  : {len(utility_poles)}")
    print(f"   Street lamps   : {len(street_lamps)}")
    print(f"   Barriers       : {len(barriers)}")
    print(f"   Bridges        : {len(bridges)}")
    print(f"   Saved          : {output_file}")
    print()


print("==========================================")
print(" PROCESSING COMPLETE")
print("==========================================")
print()

print(f"Road features       : {total_roads}")
print(f"Waterway features   : {total_waterways}")
print(f"Building features   : {total_buildings}")
print(f"Landuse features    : {total_landuse}")
print(f"Natural features    : {total_natural}")
print(f"Tree features       : {total_trees}")
print(f"Utility poles       : {total_utility_poles}")
print(f"Street lamp nodes   : {total_street_lamps}")
print(f"Barrier features    : {total_barriers}")
print(f"Bridge features     : {total_bridges}")

print()
print("Processed data saved to:")
print(OUTPUT_DIR)
print()
