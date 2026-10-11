using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OSMWorldGenerator : MonoBehaviour
{
    [Header("OSM Data")]
    public TextAsset osmData;

    [Header("Coordinate Origin")]
    public double originLatitude = 9.9642475;
    public double originLongitude = 76.2454182;

    [Header("World Scale")]
    public float worldScale = 0.01f;

    [Header("Road Settings")]
    public float roadWidth = 3f;
    public float roadHeight = 0.05f;

    [Header("Waterway Settings")]
    public float waterWidth = 4f;
    public float waterHeight = 0.02f;

    [Header("Natural Water Settings")]
    [Tooltip("Surface height in real-world metres above the local ground plane.")]
    public float naturalWaterHeight = 0.10f;

    [Header("Building Settings")]
    public float buildingHeight = 8f;
    public float buildingBaseHeight = 0.02f;

    [Header("Building Variation")]
    public bool useBuildingTypeHeights = true;

    [Header("Window / Roof Clearance")]
    [Tooltip("Keeps the top of windows visibly below the roof line.")]
    public float windowRoofClearanceMeters = 0.65f;

    [Header("Cycling Route Building Setback")]
    [Tooltip("Optional snapped cycling route used to keep nearby OSM buildings away from the cycling road.")]
    public TextAsset routeJson;
    public float roadsideBuildingSetbackMeters = 2.0f;
    public float roadsideBuildingInfluenceMeters = 35f;

    [Header("Roof Settings")]
    public bool generateRoofs = true;
    public float roofHeight = 2.5f;
    public float churchRoofHeight = 5f;

    [Header("Facade Settings")]
    public bool generateFacades = true;
    public float windowWidth = 1.2f;
    public float windowHeight = 1.2f;
    public float windowHorizontalSpacing = 3f;
    public float windowVerticalSpacing = 3f;
    public float doorWidth = 1.1f;
    public float doorHeight = 2.2f;
    public float facadeOffset = 0.03f;
    public int maxWindowsPerWall = 4;
    public int maxWindowRows = 4;

    [Header("Ground Settings")]
    public float groundHeight = -0.02f;
    public float groundMarginMeters = 25f;

    [Header("Materials")]
    public Material roadMaterial;
    public Material waterMaterial;
    public Material buildingMaterial;
    public Material roofMaterial;
    public Material groundMaterial;
    public Material windowMaterial;
    public Material doorMaterial;

    [Header("Generation")]
    public bool generateOnStart = true;

    // =========================================================
    // OSM DATA CLASSES
    // =========================================================

    [Serializable]
    public class OSMPoint
    {
        public double latitude;
        public double longitude;
    }

    [Serializable]
    public class OSMRoad
    {
        public string type;
        public OSMPoint[] points;
    }

    [Serializable]
    public class OSMWaterway
    {
        public string type;
        public OSMPoint[] points;
    }

    [Serializable]
    public class OSMBuilding
    {
        public string type;
        public OSMPoint[] points;
    }

    [Serializable]
    public class OSMNaturalWater
    {
        // In processed JSON this is usually "pond", "lake", etc.
        public string type;
        public OSMPoint[] points;
    }

    // Backward-compatible support for older processed files that
    // stored natural features in a generic "natural" array.
    [Serializable]
    public class OSMNatural
    {
        public string type;
        public OSMPoint[] points;
    }

    [Serializable]
    public class OSMData
    {
        public int version;
        public int chunkIndex;
        public float routeStartKm;
        public float routeEndKm;

        public OSMRoad[] roads;
        public OSMWaterway[] waterways;
        public OSMBuilding[] buildings;

        // New processed format.
        public OSMNaturalWater[] naturalWater;

        // Legacy fallback.
        public OSMNatural[] natural;
    }

    // =========================================================
    // GENERATED PARENTS
    // =========================================================

    private GameObject roadsParent;
    private GameObject waterwaysParent;
    private GameObject naturalWaterParent;
    private GameObject buildingsParent;

    private readonly List<Vector3> routePoints = new List<Vector3>();
    private GameObject roofsParent;
    private GameObject facadesParent;
    private GameObject groundParent;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateWorld();
        }
    }

    // =========================================================
    // MAIN GENERATOR
    // =========================================================

    public void GenerateWorld()
    {
        if (osmData == null)
        {
            Debug.LogError(
                "OSMWorldGenerator: OSM Data is not assigned."
            );
            return;
        }

        if (worldScale <= 0f)
        {
            Debug.LogError(
                "OSMWorldGenerator: World Scale must be greater than 0."
            );
            return;
        }

        OSMData data;

        try
        {
            data = JsonUtility.FromJson<OSMData>(osmData.text);
        }
        catch (Exception e)
        {
            Debug.LogError(
                "OSMWorldGenerator: JSON parsing failed.\n" +
                e.Message
            );
            return;
        }

        if (data == null)
        {
            Debug.LogError(
                "OSMWorldGenerator: Parsed data is NULL."
            );
            return;
        }

        ClearWorld();
        CreateParents();

        LoadRouteForBuildingSetback();

        Debug.Log("==========================================");
        Debug.Log(
            $"VirtualRide: Generating OSM Chunk {data.chunkIndex}"
        );
        Debug.Log(
            $"Route: {data.routeStartKm:F2} -> {data.routeEndKm:F2} km"
        );
        Debug.Log(
            $"Origin: {originLatitude:F7}, {originLongitude:F7}"
        );
        Debug.Log(
            $"World Scale: {worldScale}"
        );

        int roadCount = data.roads != null
            ? CountValidLineFeatures(data.roads)
            : 0;

        int waterwayCount = data.waterways != null
            ? CountValidLineFeatures(data.waterways)
            : 0;

        int naturalWaterCount = data.naturalWater != null
            ? CountValidPolygonFeatures(data.naturalWater)
            : 0;

        // Fallback for any old processed files.
        if (naturalWaterCount == 0 && data.natural != null)
        {
            naturalWaterCount = CountWaterFromLegacyNatural(data.natural);
        }

        int buildingCount = data.buildings != null
            ? CountValidPolygonFeatures(data.buildings)
            : 0;

        Debug.Log(
            $"OSM arrays: roads={data.roads?.Length ?? 0}, " +
            $"waterways={data.waterways?.Length ?? 0}, " +
            $"naturalWater={data.naturalWater?.Length ?? 0}, " +
            $"natural={data.natural?.Length ?? 0}, " +
            $"buildings={data.buildings?.Length ?? 0}"
        );

        if (data.roads != null)
        {
            CreateCombinedRoadMesh(data.roads);
        }

        if (data.waterways != null)
        {
            CreateCombinedWaterMesh(data.waterways);
        }

        if (data.naturalWater != null && data.naturalWater.Length > 0)
        {
            CreateCombinedNaturalWaterMesh(data.naturalWater);
        }
        else if (data.natural != null)
        {
            CreateNaturalWaterFromLegacy(data.natural);
        }

        if (data.buildings != null)
        {
            CreateCombinedBuildingMesh(data.buildings);

            if (generateRoofs)
            {
                CreateCombinedRoofMesh(data.buildings);
            }

            if (generateFacades)
            {
                CreateCombinedFacadeMeshes(data.buildings);
            }
        }

        CreateGroundMesh(data);

        Debug.Log(
            $"OSM Chunk {data.chunkIndex}: " +
            $"Roads={roadCount}, " +
            $"Waterways={waterwayCount}, " +
            $"NaturalWater={naturalWaterCount}, " +
            $"Buildings={buildingCount}"
        );

        Debug.Log(
            "VirtualRide: OSM WORLD GENERATION COMPLETE"
        );
        Debug.Log("==========================================");
    }

    // =========================================================
    // PARENTS
    // =========================================================

    private void CreateParents()
    {
        roadsParent = CreateParent("Roads");
        waterwaysParent = CreateParent("Waterways");
        naturalWaterParent = CreateParent("NaturalWater");
        buildingsParent = CreateParent("Buildings");
        roofsParent = CreateParent("Roofs");
        facadesParent = CreateParent("Facades");
        groundParent = CreateParent("Ground");
    }

    private GameObject CreateParent(string parentName)
    {
        GameObject parent = new GameObject(parentName);
        parent.transform.SetParent(transform, false);
        parent.transform.localPosition = Vector3.zero;
        parent.transform.localRotation = Quaternion.identity;
        parent.transform.localScale = Vector3.one;
        return parent;
    }

    // =========================================================
    // GEO -> UNITY
    // =========================================================

    private Vector3 GeoToUnity(double latitude, double longitude)
    {
        double deltaLatitude = latitude - originLatitude;
        double deltaLongitude = longitude - originLongitude;

        const double metersPerDegreeLatitude = 111320.0;

        double metersPerDegreeLongitude =
            111320.0 *
            Math.Cos(originLatitude * Mathf.Deg2Rad);

        float x = (float)(
            deltaLongitude *
            metersPerDegreeLongitude *
            worldScale
        );

        float z = (float)(
            deltaLatitude *
            metersPerDegreeLatitude *
            worldScale
        );

        return new Vector3(x, 0f, z);
    }

    // =========================================================
    // COUNTERS
    // =========================================================

    private int CountValidLineFeatures(OSMRoad[] roads)
    {
        int count = 0;

        foreach (OSMRoad road in roads)
        {
            if (road != null && road.points != null && road.points.Length >= 2)
            {
                count++;
            }
        }

        return count;
    }

    private int CountValidLineFeatures(OSMWaterway[] waterways)
    {
        int count = 0;

        foreach (OSMWaterway waterway in waterways)
        {
            if (waterway != null && waterway.points != null && waterway.points.Length >= 2)
            {
                count++;
            }
        }

        return count;
    }

    private int CountValidPolygonFeatures(OSMBuilding[] buildings)
    {
        int count = 0;

        foreach (OSMBuilding building in buildings)
        {
            if (building != null && building.points != null && building.points.Length >= 3)
            {
                count++;
            }
        }

        return count;
    }

    private int CountValidPolygonFeatures(OSMNaturalWater[] waters)
    {
        int count = 0;

        foreach (OSMNaturalWater water in waters)
        {
            if (water != null && water.points != null && water.points.Length >= 3)
            {
                count++;
            }
        }

        return count;
    }

    private int CountWaterFromLegacyNatural(OSMNatural[] natural)
    {
        int count = 0;

        foreach (OSMNatural feature in natural)
        {
            if (
                feature != null &&
                feature.type != null &&
                feature.type.Equals("water", StringComparison.OrdinalIgnoreCase) &&
                feature.points != null &&
                feature.points.Length >= 3
            )
            {
                count++;
            }
        }

        return count;
    }

    // =========================================================
    // ROAD MESH
    // =========================================================

    private void CreateCombinedRoadMesh(OSMRoad[] roads)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float width = roadWidth * worldScale;
        float height = roadHeight * worldScale;

        foreach (OSMRoad road in roads)
        {
            if (road == null || road.points == null || road.points.Length < 2)
            {
                continue;
            }

            int startVertex = vertices.Count;
            List<Vector3> points = new List<Vector3>();

            foreach (OSMPoint point in road.points)
            {
                points.Add(GeoToUnity(point.latitude, point.longitude));
            }

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 forward;

                if (i == 0)
                {
                    forward = points[1] - points[0];
                }
                else if (i == points.Count - 1)
                {
                    forward = points[i] - points[i - 1];
                }
                else
                {
                    forward = points[i + 1] - points[i - 1];
                }

                forward.y = 0f;

                if (forward.sqrMagnitude < 0.000001f)
                {
                    forward = Vector3.forward;
                }

                forward.Normalize();

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                Vector3 leftPoint = points[i] - right * (width * 0.5f);
                Vector3 rightPoint = points[i] + right * (width * 0.5f);

                leftPoint.y = groundHeight + height;
                rightPoint.y = groundHeight + height;

                vertices.Add(leftPoint);
                vertices.Add(rightPoint);

                float v = (float)i / Mathf.Max(1, points.Count - 1);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int index = startVertex + i * 2;

                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);

                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index + 3);
            }
        }

        CreateMeshObject(
            "RoadMesh",
            vertices,
            triangles,
            uvs,
            roadMaterial,
            roadsParent
        );
    }

    // =========================================================
    // WATERWAY MESH
    // =========================================================

    private void CreateCombinedWaterMesh(OSMWaterway[] waterways)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float width = waterWidth * worldScale;
        float height = waterHeight * worldScale;

        foreach (OSMWaterway waterway in waterways)
        {
            if (waterway == null || waterway.points == null || waterway.points.Length < 2)
            {
                continue;
            }

            int startVertex = vertices.Count;
            List<Vector3> points = new List<Vector3>();

            foreach (OSMPoint point in waterway.points)
            {
                points.Add(GeoToUnity(point.latitude, point.longitude));
            }

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 forward;

                if (i == 0)
                {
                    forward = points[1] - points[0];
                }
                else if (i == points.Count - 1)
                {
                    forward = points[i] - points[i - 1];
                }
                else
                {
                    forward = points[i + 1] - points[i - 1];
                }

                forward.y = 0f;

                if (forward.sqrMagnitude < 0.000001f)
                {
                    forward = Vector3.forward;
                }

                forward.Normalize();

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                Vector3 leftPoint = points[i] - right * (width * 0.5f);
                Vector3 rightPoint = points[i] + right * (width * 0.5f);

                leftPoint.y = groundHeight + height;
                rightPoint.y = groundHeight + height;

                vertices.Add(leftPoint);
                vertices.Add(rightPoint);

                float v = (float)i / Mathf.Max(1, points.Count - 1);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int index = startVertex + i * 2;

                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);

                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index + 3);
            }
        }

        CreateMeshObject(
            "WaterMesh",
            vertices,
            triangles,
            uvs,
            waterMaterial,
            waterwaysParent
        );
    }

    // =========================================================
    // NATURAL WATER - NEW PROCESSED FORMAT
    // =========================================================

    private void CreateCombinedNaturalWaterMesh(
        OSMNaturalWater[] waters
    )
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float height = naturalWaterHeight * worldScale;
        int generated = 0;

        foreach (OSMNaturalWater water in waters)
        {
            if (water == null || water.points == null || water.points.Length < 3)
            {
                continue;
            }

            List<Vector3> polygon = ConvertPolygon(water.points);

            if (polygon.Count < 3)
            {
                continue;
            }

            List<int> polygonTriangles = BuildSimplePolygonTriangles(polygon);

            if (polygonTriangles.Count < 3)
            {
                Debug.LogWarning(
                    $"OSMWorldGenerator: Failed to triangulate natural water '{water.type}'."
                );
                continue;
            }

            int startVertex = vertices.Count;

            foreach (Vector3 point in polygon)
            {
                Vector3 p = point;
                p.y = groundHeight + height;

                vertices.Add(p);
                uvs.Add(new Vector2(p.x, p.z));
            }

            // Add BOTH windings. This works even if the water
            // material is set to Render Face = Front.
            for (int i = 0; i < polygonTriangles.Count; i += 3)
            {
                int a = startVertex + polygonTriangles[i];
                int b = startVertex + polygonTriangles[i + 1];
                int c = startVertex + polygonTriangles[i + 2];

                // Up-facing side
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                // Down-facing side
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            generated++;
        }

        Debug.Log($"Natural water polygons generated: {generated}");
        Debug.Log($"Natural water vertices: {vertices.Count}");
        Debug.Log($"Natural water triangles: {triangles.Count / 3}");

        CreateMeshObject(
            "NaturalWaterMesh",
            vertices,
            triangles,
            uvs,
            waterMaterial,
            naturalWaterParent
        );
    }

    // =========================================================
    // ROOF MESH
    // =========================================================

    private void CreateCombinedRoofMesh(
        OSMBuilding[] buildings
    )
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int generated = 0;

        foreach (OSMBuilding building in buildings)
        {
            if (building == null || building.points == null || building.points.Length < 3)
            {
                continue;
            }

            List<Vector3> polygon = ConvertPolygon(building.points);

            if (polygon.Count < 3 || !ShouldGeneratePitchedRoof(building.type))
            {
                continue;
            }

            ApplyRoadsideBuildingSetback(polygon);

            // The current hip/pyramid roof uses a center point.
            // That construction is only geometrically valid for
            // convex footprints, so skip pitched roofs on concave
            // buildings rather than generating broken triangles.
            if (!IsConvexPolygonXZ(polygon))
            {
                continue;
            }

            float realBuildingHeight = useBuildingTypeHeights
                ? GetBuildingHeight(building.type)
                : buildingHeight;

            float realRoofHeight = GetRoofHeight(building.type);

            float roofBaseY =
                groundHeight +
                realBuildingHeight * worldScale;

            float roofTopY =
                roofBaseY + realRoofHeight * worldScale;

            Vector3 center = GetPolygonCenter(polygon);
            center.y = roofTopY;

            int startVertex = vertices.Count;
            int count = polygon.Count;

            for (int i = 0; i < count; i++)
            {
                Vector3 p = polygon[i];
                p.y = roofBaseY;
                vertices.Add(p);
                uvs.Add(Vector2.zero);
            }

            int centerIndex = vertices.Count;
            vertices.Add(center);
            uvs.Add(Vector2.zero);

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;

                triangles.Add(startVertex + i);
                triangles.Add(centerIndex);
                triangles.Add(startVertex + next);

                triangles.Add(startVertex + i);
                triangles.Add(startVertex + next);
                triangles.Add(centerIndex);
            }

            generated++;
        }

        Debug.Log($"Roofs generated: {generated}");

        CreateMeshObject(
            "RoofMesh",
            vertices,
            triangles,
            uvs,
            roofMaterial,
            roofsParent
        );
    }

    private bool ShouldGeneratePitchedRoof(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return true;
        }

        switch (type.ToLowerInvariant())
        {
            case "commercial":
            case "retail":
            case "office":
            case "apartments":
            case "hospital":
                return false;
            default:
                return true;
        }
    }

    private float GetRoofHeight(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return roofHeight;
        }

        switch (type.ToLowerInvariant())
        {
            case "church":
                return churchRoofHeight;
            case "school":
                return 3f;
            case "house":
            case "detached":
            case "semidetached_house":
            case "residential":
                return 2.5f;
            case "industrial":
                return 2f;
            case "warehouse":
                return 1.5f;
            default:
                return roofHeight;
        }
    }

    private Vector3 GetPolygonCenter(List<Vector3> polygon)
    {
        Vector3 center = Vector3.zero;

        if (polygon == null || polygon.Count == 0)
        {
            return center;
        }

        foreach (Vector3 point in polygon)
        {
            center += point;
        }

        return center / polygon.Count;
    }

    // =========================================================
    // FACADE MESHES
    // =========================================================

    private void CreateCombinedFacadeMeshes(OSMBuilding[] buildings)
    {
        List<Vector3> windowVertices = new List<Vector3>();
        List<int> windowTriangles = new List<int>();
        List<Vector2> windowUVs = new List<Vector2>();

        List<Vector3> doorVertices = new List<Vector3>();
        List<int> doorTriangles = new List<int>();
        List<Vector2> doorUVs = new List<Vector2>();

        if (buildings == null)
        {
            return;
        }

        int buildingCount = 0;
        int windowCount = 0;
        int doorCount = 0;

        foreach (OSMBuilding building in buildings)
        {
            if (building == null || building.points == null || building.points.Length < 3)
            {
                continue;
            }

            List<Vector3> polygon = ConvertPolygon(building.points);

            if (polygon.Count < 3)
            {
                continue;
            }

            ApplyRoadsideBuildingSetback(polygon);

            // Ensure CCW ordering so our right-hand normal points outward.
            if (SignedAreaXZ(polygon) < 0f)
            {
                polygon.Reverse();
            }

            float realHeight = useBuildingTypeHeights
                ? GetBuildingHeight(building.type)
                : buildingHeight;

            if (realHeight < 3f)
            {
                continue;
            }

            int rows = Mathf.Clamp(
                GetFacadeRows(building.type, realHeight),
                1,
                Mathf.Max(1, maxWindowRows)
            );

            int longestEdge = FindLongestEdge(polygon);

            for (int edgeIndex = 0; edgeIndex < polygon.Count; edgeIndex++)
            {
                Vector3 a = polygon[edgeIndex];
                Vector3 b = polygon[(edgeIndex + 1) % polygon.Count];

                Vector3 edge = b - a;
                edge.y = 0f;

                float edgeLength = edge.magnitude;

                if (edgeLength < 0.25f)
                {
                    continue;
                }

                edge.Normalize();

                // For a CCW polygon, this is the outward horizontal normal.
                Vector3 outward = new Vector3(edge.z, 0f, -edge.x).normalized;

                float offset = facadeOffset * worldScale;

                int windowsOnWall = GetWindowsOnWall(
                    edgeLength,
                    building.type
                );

                float maximumWindowTop =
                    realHeight -
                    (
                        ShouldGeneratePitchedRoof(
                            building.type
                        )
                            ? Mathf.Max(
                                0.65f,
                                windowRoofClearanceMeters
                            )
                            : 0.35f
                    );

                for (int row = 0; row < rows; row++)
                {
                    float realBottom =
                        1.4f + row * windowVerticalSpacing;

                    // Never let a window enter the roof volume.
                    // Break rather than continue because higher rows
                    // cannot become valid again.
                    if (
                        realBottom + windowHeight >
                        maximumWindowTop
                    )
                    {
                        break;
                    }

                    for (int w = 0; w < windowsOnWall; w++)
                    {
                        float t =
                            (w + 1f) /
                            (windowsOnWall + 1f);

                        Vector3 center =
                            Vector3.Lerp(a, b, t) + outward * offset;

                        float bottomY =
                            (buildingBaseHeight + realBottom) * worldScale;

                        AddFacadeQuad(
                            windowVertices,
                            windowTriangles,
                            windowUVs,
                            center,
                            edge,
                            bottomY,
                            windowWidth * worldScale,
                            windowHeight * worldScale
                        );

                        windowCount++;
                    }
                }

                // One door on the longest wall.
                if (edgeIndex == longestEdge)
                {
                    float maximumDoorTop =
                        realHeight -
                        (
                            ShouldGeneratePitchedRoof(
                                building.type
                            )
                                ? Mathf.Max(
                                    0.45f,
                                    windowRoofClearanceMeters * 0.70f
                                )
                                : 0.25f
                        );

                    float realDoorHeight = Mathf.Min(
                        doorHeight,
                        Mathf.Max(
                            1f,
                            maximumDoorTop
                        )
                    );

                    float realDoorWidth = Mathf.Min(
                        doorWidth,
                        Mathf.Max(0.5f, edgeLength * 0.25f)
                    );

                    Vector3 center =
                        Vector3.Lerp(a, b, 0.5f) + outward * offset;

                    float bottomY =
                        groundHeight;

                    AddFacadeQuad(
                        doorVertices,
                        doorTriangles,
                        doorUVs,
                        center,
                        edge,
                        bottomY,
                        realDoorWidth * worldScale,
                        realDoorHeight * worldScale
                    );

                    doorCount++;
                }
            }

            buildingCount++;
        }

        Debug.Log(
            $"Facades generated: Buildings={buildingCount}, " +
            $"Windows={windowCount}, Doors={doorCount}"
        );

        CreateMeshObject(
            "WindowMesh",
            windowVertices,
            windowTriangles,
            windowUVs,
            windowMaterial,
            facadesParent
        );

        CreateMeshObject(
            "DoorMesh",
            doorVertices,
            doorTriangles,
            doorUVs,
            doorMaterial,
            facadesParent
        );
    }

    private int GetFacadeRows(string buildingType, float realHeight)
    {
        if (string.IsNullOrEmpty(buildingType))
        {
            return Mathf.Clamp(
                Mathf.FloorToInt(realHeight / 3f),
                1,
                Mathf.Max(1, maxWindowRows)
            );
        }

        switch (buildingType.ToLowerInvariant())
        {
            case "house":
            case "detached":
            case "semidetached_house":
                return 1;

            case "school":
                return Mathf.Min(2, Mathf.Max(1, maxWindowRows));

            case "church":
                return 1;

            case "industrial":
            case "warehouse":
                return 1;

            case "apartments":
            case "office":
            case "hotel":
            case "hospital":
                return Mathf.Clamp(
                    Mathf.FloorToInt(realHeight / 3f),
                    2,
                    Mathf.Max(2, maxWindowRows)
                );

            default:
                return Mathf.Clamp(
                    Mathf.FloorToInt(realHeight / 3f),
                    1,
                    Mathf.Max(1, maxWindowRows)
                );
        }
    }

    private int GetWindowsOnWall(float edgeLength, string buildingType)
    {
        float spacing = Mathf.Max(0.5f, windowHorizontalSpacing) * worldScale;

        int count = Mathf.FloorToInt(edgeLength / spacing);

        count = Mathf.Clamp(
            count,
            1,
            Mathf.Max(1, maxWindowsPerWall)
        );

        if (IsIndustrialType(buildingType))
        {
            count = Mathf.Min(count, 2);
        }

        return count;
    }

    private bool IsIndustrialType(string buildingType)
    {
        if (string.IsNullOrEmpty(buildingType))
        {
            return false;
        }

        string type = buildingType.ToLowerInvariant();

        return type == "industrial" || type == "warehouse";
    }

    private int FindLongestEdge(List<Vector3> polygon)
    {
        int longestIndex = 0;
        float longestLength = -1f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3 a = polygon[i];
            Vector3 b = polygon[(i + 1) % polygon.Count];

            float length = Vector3.Distance(a, b);

            if (length > longestLength)
            {
                longestLength = length;
                longestIndex = i;
            }
        }

        return longestIndex;
    }

    private void AddFacadeQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        Vector3 center,
        Vector3 edgeDirection,
        float bottomY,
        float width,
        float height
    )
    {
        Vector3 halfWidth =
            edgeDirection * (width * 0.5f);

        Vector3 bottomCenter = center;
        bottomCenter.y = bottomY;

        Vector3 topCenter =
            bottomCenter + Vector3.up * height;

        int start = vertices.Count;

        vertices.Add(bottomCenter - halfWidth);
        vertices.Add(bottomCenter + halfWidth);
        vertices.Add(topCenter + halfWidth);
        vertices.Add(topCenter - halfWidth);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(0f, 1f));

        // Front
        triangles.Add(start + 0);
        triangles.Add(start + 1);
        triangles.Add(start + 2);

        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 3);

        // Back
        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 1);

        triangles.Add(start + 0);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }

    // =========================================================
    // GROUND MESH
    // =========================================================

    private void CreateGroundMesh(OSMData data)
    {
        bool hasBounds = false;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        if (data.roads != null)
        {
            foreach (OSMRoad road in data.roads)
            {
                if (road == null || road.points == null) continue;
                foreach (OSMPoint point in road.points)
                {
                    UpdateGroundBounds(GeoToUnity(point.latitude, point.longitude), ref hasBounds, ref minX, ref maxX, ref minZ, ref maxZ);
                }
            }
        }

        if (data.buildings != null)
        {
            foreach (OSMBuilding building in data.buildings)
            {
                if (building == null || building.points == null) continue;
                foreach (OSMPoint point in building.points)
                {
                    UpdateGroundBounds(GeoToUnity(point.latitude, point.longitude), ref hasBounds, ref minX, ref maxX, ref minZ, ref maxZ);
                }
            }
        }

        if (data.waterways != null)
        {
            foreach (OSMWaterway waterway in data.waterways)
            {
                if (waterway == null || waterway.points == null) continue;
                foreach (OSMPoint point in waterway.points)
                {
                    UpdateGroundBounds(GeoToUnity(point.latitude, point.longitude), ref hasBounds, ref minX, ref maxX, ref minZ, ref maxZ);
                }
            }
        }

        if (data.naturalWater != null)
        {
            foreach (OSMNaturalWater water in data.naturalWater)
            {
                if (water == null || water.points == null) continue;
                foreach (OSMPoint point in water.points)
                {
                    UpdateGroundBounds(GeoToUnity(point.latitude, point.longitude), ref hasBounds, ref minX, ref maxX, ref minZ, ref maxZ);
                }
            }
        }

        if (!hasBounds)
        {
            Debug.LogWarning("OSMWorldGenerator: Could not determine ground bounds.");
            return;
        }

        float margin = groundMarginMeters * worldScale;

        minX -= margin;
        maxX += margin;
        minZ -= margin;
        maxZ += margin;

        List<Vector3> vertices = new List<Vector3>
        {
            new Vector3(minX, groundHeight, minZ),
            new Vector3(maxX, groundHeight, minZ),
            new Vector3(maxX, groundHeight, maxZ),
            new Vector3(minX, groundHeight, maxZ)
        };

        List<int> triangles = new List<int>
        {
            0, 2, 1,
            0, 3, 2
        };

        List<Vector2> uvs = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        CreateMeshObject("GroundMesh", vertices, triangles, uvs, groundMaterial, groundParent);

        Debug.Log(
            $"Ground bounds: X {minX:F2} -> {maxX:F2}, Z {minZ:F2} -> {maxZ:F2}"
        );
    }

    private void UpdateGroundBounds(
        Vector3 point,
        ref bool hasBounds,
        ref float minX,
        ref float maxX,
        ref float minZ,
        ref float maxZ
    )
    {
        hasBounds = true;
        minX = Mathf.Min(minX, point.x);
        maxX = Mathf.Max(maxX, point.x);
        minZ = Mathf.Min(minZ, point.z);
        maxZ = Mathf.Max(maxZ, point.z);
    }

    // =========================================================
    // NATURAL WATER - LEGACY FALLBACK
    // =========================================================

    private void CreateNaturalWaterFromLegacy(
        OSMNatural[] natural
    )
    {
        List<OSMNaturalWater> waters = new List<OSMNaturalWater>();

        foreach (OSMNatural feature in natural)
        {
            if (
                feature == null ||
                feature.type == null ||
                !feature.type.Equals("water", StringComparison.OrdinalIgnoreCase) ||
                feature.points == null ||
                feature.points.Length < 3
            )
            {
                continue;
            }

            waters.Add(
                new OSMNaturalWater
                {
                    type = "water",
                    points = feature.points
                }
            );
        }

        if (waters.Count > 0)
        {
            CreateCombinedNaturalWaterMesh(waters.ToArray());
        }
    }

    // =========================================================
    // CONVERT POLYGON
    // =========================================================

    private List<Vector3> ConvertPolygon(
        OSMPoint[] points
    )
    {
        List<Vector3> polygon = new List<Vector3>();

        foreach (OSMPoint point in points)
        {
            polygon.Add(
                GeoToUnity(
                    point.latitude,
                    point.longitude
                )
            );
        }

        RemoveDuplicateClosingPoint(polygon);
        RemoveDuplicatePoints(polygon);

        return polygon;
    }

    // =========================================================
    // SIMPLE POLYGON TRIANGLES
    // =========================================================
    // For now we deliberately use a stable fan triangulation.
    // This is reliable for the small/simple OSM water polygons
    // we are currently validating.
    // =========================================================

    private List<int> BuildSimplePolygonTriangles(
        List<Vector3> polygon
    )
    {
        List<int> result = new List<int>();

        if (polygon == null || polygon.Count < 3)
        {
            return result;
        }

        // Ensure the polygon is counter-clockwise in XZ.
        if (SignedAreaXZ(polygon) < 0f)
        {
            polygon.Reverse();
        }

        for (int i = 1; i < polygon.Count - 1; i++)
        {
            result.Add(0);
            result.Add(i);
            result.Add(i + 1);
        }

        return result;
    }

    // =========================================================
    // ROBUST POLYGON TRIANGULATION
    // =========================================================
    // Ear-clipping triangulation for building footprints. Unlike a
    // center fan, this works for concave OSM footprints without
    // creating triangles that spill outside the building.
    // =========================================================

    private List<int> BuildRobustPolygonTriangles(
        List<Vector3> polygon
    )
    {
        List<int> result = new List<int>();

        if (polygon == null || polygon.Count < 3)
        {
            return result;
        }

        List<int> remaining = new List<int>();

        // Work from a CCW polygon in XZ.
        if (SignedAreaXZ(polygon) >= 0f)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                remaining.Add(i);
            }
        }
        else
        {
            for (int i = polygon.Count - 1; i >= 0; i--)
            {
                remaining.Add(i);
            }
        }

        int guard = 0;
        int maxIterations = polygon.Count * polygon.Count;

        while (remaining.Count > 2 && guard < maxIterations)
        {
            bool earFound = false;

            for (int i = 0; i < remaining.Count; i++)
            {
                int prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                int curr = remaining[i];
                int next = remaining[(i + 1) % remaining.Count];

                Vector3 a = polygon[prev];
                Vector3 b = polygon[curr];
                Vector3 c = polygon[next];

                if (CrossXZ(a, b, c) <= 0.000001f)
                {
                    continue;
                }

                bool containsOtherPoint = false;

                for (int j = 0; j < remaining.Count; j++)
                {
                    int testIndex = remaining[j];

                    if (testIndex == prev || testIndex == curr || testIndex == next)
                    {
                        continue;
                    }

                    if (PointInTriangleXZ(polygon[testIndex], a, b, c))
                    {
                        containsOtherPoint = true;
                        break;
                    }
                }

                if (containsOtherPoint)
                {
                    continue;
                }

                result.Add(prev);
                result.Add(curr);
                result.Add(next);

                remaining.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                // Degenerate/self-intersecting polygons should not
                // crash a bake. Refuse to generate unreliable faces.
                result.Clear();
                return result;
            }

            guard++;
        }

        return result;
    }

    private float CrossXZ(
        Vector3 a,
        Vector3 b,
        Vector3 c
    )
    {
        float abx = b.x - a.x;
        float abz = b.z - a.z;
        float acx = c.x - a.x;
        float acz = c.z - a.z;

        return abx * acz - abz * acx;
    }

    private bool PointInTriangleXZ(
        Vector3 p,
        Vector3 a,
        Vector3 b,
        Vector3 c
    )
    {
        float c1 = CrossXZ(a, b, p);
        float c2 = CrossXZ(b, c, p);
        float c3 = CrossXZ(c, a, p);

        const float epsilon = 0.000001f;

        bool hasNegative =
            c1 < -epsilon ||
            c2 < -epsilon ||
            c3 < -epsilon;

        bool hasPositive =
            c1 > epsilon ||
            c2 > epsilon ||
            c3 > epsilon;

        return !(hasNegative && hasPositive);
    }

    private bool IsConvexPolygonXZ(
        List<Vector3> polygon
    )
    {
        if (polygon == null || polygon.Count < 3)
        {
            return false;
        }

        float sign = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3 a = polygon[i];
            Vector3 b = polygon[(i + 1) % polygon.Count];
            Vector3 c = polygon[(i + 2) % polygon.Count];

            float cross = CrossXZ(a, b, c);

            if (Mathf.Abs(cross) < 0.000001f)
            {
                continue;
            }

            if (Mathf.Abs(sign) < 0.000001f)
            {
                sign = Mathf.Sign(cross);
            }
            else if (Mathf.Sign(cross) != Mathf.Sign(sign))
            {
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // BUILDINGS
    // =========================================================


    // =========================================================
    // CYCLING ROUTE / BUILDING SETBACK
    // =========================================================

    [Serializable]
    private class RouteDataForSetback
    {
        public OSMPoint[] points;
    }


    private void LoadRouteForBuildingSetback()
    {
        routePoints.Clear();

        if (routeJson == null)
        {
            return;
        }

        try
        {
            RouteDataForSetback route =
                JsonUtility.FromJson<RouteDataForSetback>(
                    routeJson.text
                );

            if (
                route != null &&
                route.points != null
            )
            {
                foreach (OSMPoint point in route.points)
                {
                    routePoints.Add(
                        GeoToUnity(
                            point.latitude,
                            point.longitude
                        )
                    );
                }
            }
        }
        catch
        {
            routePoints.Clear();
        }
    }


    private void ApplyRoadsideBuildingSetback(
        List<Vector3> polygon
    )
    {
        if (
            routePoints.Count < 2 ||
            polygon == null ||
            polygon.Count < 3
        )
        {
            return;
        }

        float nearestDistanceMeters =
            float.MaxValue;

        Vector3 nearestBuildingPoint =
            Vector3.zero;

        Vector3 nearestRoutePoint =
            Vector3.zero;

        for (
            int i = 0;
            i < polygon.Count;
            i++
        )
        {
            Vector3 a =
                polygon[i];

            Vector3 b =
                polygon[
                    (i + 1) %
                    polygon.Count
                ];

            for (
                int s = 0;
                s <= 4;
                s++
            )
            {
                float t =
                    s / 4f;

                Vector3 candidate =
                    Vector3.Lerp(
                        a,
                        b,
                        t
                    );

                Vector3 routePoint =
                    ClosestRoutePointForSetback(
                        candidate
                    );

                float distanceMeters =
                    Vector3.Distance(
                        candidate,
                        routePoint
                    ) /
                    Mathf.Max(
                        worldScale,
                        0.000001f
                    );

                if (
                    distanceMeters <
                    nearestDistanceMeters
                )
                {
                    nearestDistanceMeters =
                        distanceMeters;

                    nearestBuildingPoint =
                        candidate;

                    nearestRoutePoint =
                        routePoint;
                }
            }
        }

        if (
            nearestDistanceMeters ==
            float.MaxValue ||
            nearestDistanceMeters >
            roadsideBuildingInfluenceMeters
        )
        {
            return;
        }

        // Cycling road = 5 m wide, shoulder = 0.75 m each side.
        float desiredEdgeDistance =
            2.5f +
            0.75f +
            Mathf.Max(
                0f,
                roadsideBuildingSetbackMeters
            );

        float shiftMeters =
            desiredEdgeDistance -
            nearestDistanceMeters;

        if (shiftMeters <= 0f)
        {
            return;
        }

        Vector3 awayFromRoad =
            nearestBuildingPoint -
            nearestRoutePoint;

        awayFromRoad.y = 0f;

        if (
            awayFromRoad.sqrMagnitude <
            0.000001f
        )
        {
            return;
        }

        awayFromRoad.Normalize();

        Vector3 shift =
            awayFromRoad *
            shiftMeters *
            worldScale;

        for (
            int i = 0;
            i < polygon.Count;
            i++
        )
        {
            polygon[i] += shift;
        }
    }


    private Vector3 ClosestRoutePointForSetback(
        Vector3 position
    )
    {
        float bestDistance =
            float.MaxValue;

        Vector3 bestPoint =
            routePoints[0];

        for (
            int i = 0;
            i < routePoints.Count - 1;
            i++
        )
        {
            Vector3 a =
                routePoints[i];

            Vector3 b =
                routePoints[i + 1];

            Vector3 segment =
                b - a;

            segment.y =
                0f;

            float lengthSq =
                segment.sqrMagnitude;

            if (
                lengthSq <
                0.000001f
            )
            {
                continue;
            }

            float t =
                Vector3.Dot(
                    position - a,
                    segment
                ) /
                lengthSq;

            t =
                Mathf.Clamp01(t);

            Vector3 closest =
                a +
                segment *
                t;

            float distance =
                Vector3.Distance(
                    position,
                    closest
                );

            if (
                distance <
                bestDistance
            )
            {
                bestDistance =
                    distance;

                bestPoint =
                    closest;
            }
        }

        return bestPoint;
    }


    private void CreateCombinedBuildingMesh(
        OSMBuilding[] buildings
    )
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Match the exact elevation of GroundMesh.
        float baseHeight = groundHeight;

        int generated = 0;

        foreach (OSMBuilding building in buildings)
        {
            if (building == null || building.points == null || building.points.Length < 3)
            {
                continue;
            }

            List<Vector3> polygon = ConvertPolygon(building.points);

            if (polygon.Count < 3)
            {
                continue;
            }

            ApplyRoadsideBuildingSetback(polygon);

            // Keep generated footprints planar for a stable building shell.
            for (int p = 0; p < polygon.Count; p++)
            {
                Vector3 point = polygon[p];
                point.y = groundHeight;
                polygon[p] = point;
            }

            float realHeight = useBuildingTypeHeights
                ? GetBuildingHeight(building.type)
                : buildingHeight;

            float scaledHeight = realHeight * worldScale;

            int startVertex = vertices.Count;
            int count = polygon.Count;

            for (int i = 0; i < count; i++)
            {
                Vector3 p = polygon[i];
                p.y = baseHeight;
                vertices.Add(p);
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 p = polygon[i];
                p.y = baseHeight + scaledHeight;
                vertices.Add(p);
            }

            List<int> topTriangles = BuildRobustPolygonTriangles(polygon);

            if (topTriangles.Count < 3)
            {
                Debug.LogWarning(
                    $"OSMWorldGenerator: Failed robust triangulation for building '{building.type}' with {polygon.Count} points."
                );
            }

            for (int i = 0; i < topTriangles.Count; i += 3)
            {
                triangles.Add(startVertex + count + topTriangles[i]);
                triangles.Add(startVertex + count + topTriangles[i + 1]);
                triangles.Add(startVertex + count + topTriangles[i + 2]);
            }

            for (int i = 1; i < count - 1; i++)
            {
                triangles.Add(startVertex);
                triangles.Add(startVertex + i + 1);
                triangles.Add(startVertex + i);
            }

            for (int i = 0; i < count; i++)
            {
                int next =
                    (i + 1) %
                    count;

                int bottomA =
                    startVertex + i;

                int bottomB =
                    startVertex + next;

                int topA =
                    startVertex + count + i;

                int topB =
                    startVertex + count + next;

                // Front side.
                triangles.Add(bottomA);
                triangles.Add(bottomB);
                triangles.Add(topB);

                triangles.Add(bottomA);
                triangles.Add(topB);
                triangles.Add(topA);

                // Back side.
                triangles.Add(bottomA);
                triangles.Add(topB);
                triangles.Add(bottomB);

                triangles.Add(bottomA);
                triangles.Add(topA);
                triangles.Add(topB);
            }

            for (int i = 0; i < count * 2; i++)
            {
                uvs.Add(Vector2.zero);
            }

            generated++;
        }

        Debug.Log(
            $"Buildings generated: {generated}"
        );

        CreateMeshObject(
            "BuildingMesh",
            vertices,
            triangles,
            uvs,
            buildingMaterial,
            buildingsParent
        );

        CreateNearRoadArchitecturalDetails(buildings);
    }

    // =========================================================
    // NEAR-ROAD ARCHITECTURAL DETAILS
    // =========================================================

    private void CreateNearRoadArchitecturalDetails(
        OSMBuilding[] buildings
    )
    {
        if (
            buildings == null ||
            routePoints.Count < 2
        )
        {
            return;
        }

        List<Vector3> detailVertices =
            new List<Vector3>();

        List<int> detailTriangles =
            new List<int>();

        int generated =
            0;

        foreach (OSMBuilding building in buildings)
        {
            if (
                building == null ||
                building.points == null ||
                building.points.Length < 3
            )
            {
                continue;
            }

            List<Vector3> polygon =
                ConvertPolygon(building.points);

            if (polygon.Count < 3)
            {
                continue;
            }

            ApplyRoadsideBuildingSetback(polygon);

            Vector3 center =
                GetPolygonCenter(polygon);

            float routeDistance =
                Vector3.Distance(
                    center,
                    ClosestRoutePointForSetback(center)
                )
                /
                Mathf.Max(
                    worldScale,
                    0.000001f
                );

            if (
                routeDistance >
                28f
            )
            {
                continue;
            }

            float height =
                useBuildingTypeHeights
                    ? GetBuildingHeight(building.type)
                    : buildingHeight;

            if (height < 4f)
            {
                continue;
            }

            int edgeIndex =
                FindNearestEdgeToRoute(
                    polygon
                );

            if (
                edgeIndex < 0 ||
                edgeIndex >= polygon.Count
            )
            {
                continue;
            }

            Vector3 a =
                polygon[edgeIndex];

            Vector3 b =
                polygon[
                    (edgeIndex + 1) %
                    polygon.Count
                ];

            Vector3 edge =
                b - a;

            edge.y =
                0f;

            if (edge.sqrMagnitude < 0.000001f)
            {
                continue;
            }

            edge.Normalize();

            Vector3 outward =
                new Vector3(
                    edge.z,
                    0f,
                    -edge.x
                ).normalized;

            Vector3 roadPoint =
                ClosestRoutePointForSetback(
                    center
                );

            Vector3 towardRoad =
                roadPoint -
                (
                    a +
                    b
                ) *
                0.5f;

            towardRoad.y =
                0f;

            if (
                towardRoad.sqrMagnitude <
                0.000001f
            )
            {
                continue;
            }

            towardRoad.Normalize();

            // Put architectural details on the road-facing side.
            Vector3 facing =
                towardRoad;

            float facadeDepth =
                0.10f *
                worldScale;

            Vector3 facadeCenter =
                (
                    a +
                    b
                )
                *
                0.5f
                +
                facing *
                facadeDepth;

            // Shallow roof/eave band.
            float eaveHeight =
                Mathf.Min(
                    height * 0.80f,
                    5.5f
                );

            AddArchitecturalBox(
                detailVertices,
                detailTriangles,
                facadeCenter +
                facing *
                0.12f *
                worldScale +
                Vector3.up *
                (
                    eaveHeight *
                    worldScale
                ),
                edge,
                facing,
                0.12f,
                Mathf.Clamp(
                    Vector3.Distance(a, b)
                    /
                    Mathf.Max(
                        worldScale,
                        0.000001f
                    ),
                    2f,
                    40f
                ),
                0.35f
            );

            // Corner pilasters give the flat facade depth.
            float pilasterHeight =
                Mathf.Min(
                    height *
                    0.85f,
                    8f
                );

            Vector3 leftCorner =
                a +
                facing *
                0.08f *
                worldScale;

            Vector3 rightCorner =
                b +
                facing *
                0.08f *
                worldScale;

            AddArchitecturalBox(
                detailVertices,
                detailTriangles,
                leftCorner +
                Vector3.up *
                (
                    pilasterHeight *
                    0.5f *
                    worldScale
                ),
                edge,
                facing,
                0.18f,
                0.35f,
                pilasterHeight
            );

            AddArchitecturalBox(
                detailVertices,
                detailTriangles,
                rightCorner +
                Vector3.up *
                (
                    pilasterHeight *
                    0.5f *
                    worldScale
                ),
                edge,
                facing,
                0.18f,
                0.35f,
                pilasterHeight
            );

            generated++;
        }

        if (detailVertices.Count == 0)
        {
            return;
        }

        CreateMeshObject(
            "NearRoadBuildingArchitecture",
            detailVertices,
            detailTriangles,
            uvs: null,
            material: buildingMaterial,
            parent: buildingsParent
        );

        Debug.Log(
            $"Near-road architectural details generated: {generated}"
        );
    }


    private int FindNearestEdgeToRoute(
        List<Vector3> polygon
    )
    {
        if (
            polygon == null ||
            polygon.Count < 2
        )
        {
            return -1;
        }

        float bestDistance =
            float.MaxValue;

        int bestIndex =
            -1;

        for (
            int i = 0;
            i < polygon.Count;
            i++
        )
        {
            Vector3 a =
                polygon[i];

            Vector3 b =
                polygon[
                    (i + 1) %
                    polygon.Count
                ];

            Vector3 midpoint =
                (
                    a +
                    b
                )
                *
                0.5f;

            Vector3 routePoint =
                ClosestRoutePointForSetback(
                    midpoint
                );

            float distance =
                Vector3.Distance(
                    midpoint,
                    routePoint
                );

            if (
                distance <
                bestDistance
            )
            {
                bestDistance =
                    distance;

                bestIndex =
                    i;
            }
        }

        return bestIndex;
    }


    private void AddArchitecturalBox(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 center,
        Vector3 right,
        Vector3 forward,
        float depthMeters,
        float widthMeters,
        float heightMeters
    )
    {
        right.y = 0f;
        forward.y = 0f;

        if (
            right.sqrMagnitude < 0.000001f ||
            forward.sqrMagnitude < 0.000001f
        )
        {
            return;
        }

        right.Normalize();
        forward.Normalize();

        Vector3 up =
            Vector3.up;

        Vector3 hx =
            right *
            widthMeters *
            0.5f *
            worldScale;

        Vector3 hy =
            up *
            heightMeters *
            0.5f *
            worldScale;

        Vector3 hz =
            forward *
            depthMeters *
            0.5f *
            worldScale;

        int start =
            vertices.Count;

        vertices.Add(center - hx - hy - hz);
        vertices.Add(center + hx - hy - hz);
        vertices.Add(center + hx + hy - hz);
        vertices.Add(center - hx + hy - hz);

        vertices.Add(center - hx - hy + hz);
        vertices.Add(center + hx - hy + hz);
        vertices.Add(center + hx + hy + hz);
        vertices.Add(center - hx + hy + hz);

        AddDetailFace(
            start, 0, 1, 2, 3, triangles
        );

        AddDetailFace(
            start, 4, 7, 6, 5, triangles
        );

        AddDetailFace(
            start, 0, 4, 5, 1, triangles
        );

        AddDetailFace(
            start, 3, 2, 6, 7, triangles
        );

        AddDetailFace(
            start, 1, 5, 6, 2, triangles
        );

        AddDetailFace(
            start, 0, 3, 7, 4, triangles
        );
    }


    private void AddDetailFace(
        int start,
        int a,
        int b,
        int c,
        int d,
        List<int> triangles
    )
    {
        triangles.Add(start + a);
        triangles.Add(start + b);
        triangles.Add(start + c);

        triangles.Add(start + a);
        triangles.Add(start + c);
        triangles.Add(start + d);

        triangles.Add(start + a);
        triangles.Add(start + c);
        triangles.Add(start + b);

        triangles.Add(start + a);
        triangles.Add(start + d);
        triangles.Add(start + c);
    }

    // =========================================================
    // BUILDING HEIGHTS
    // =========================================================

    private float GetBuildingHeight(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return buildingHeight;
        }

        switch (type.ToLowerInvariant())
        {
            case "house":
            case "detached":
            case "semidetached_house":
                return 7f;

            case "residential":
                return 10f;

            case "apartments":
                return 18f;

            case "school":
                return 12f;

            case "church":
                return 18f;

            case "commercial":
                return 15f;

            case "retail":
                return 10f;

            case "industrial":
                return 12f;

            case "warehouse":
                return 9f;

            case "office":
                return 18f;

            case "hotel":
                return 22f;

            case "hospital":
                return 18f;

            case "public":
                return 12f;

            case "yes":
            default:
                return buildingHeight;
        }
    }

    // =========================================================
    // POLYGON HELPERS
    // =========================================================

    private float SignedAreaXZ(
        List<Vector3> polygon
    )
    {
        float area = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3 a = polygon[i];
            Vector3 b = polygon[(i + 1) % polygon.Count];

            area += a.x * b.z - b.x * a.z;
        }

        return area * 0.5f;
    }

    private void RemoveDuplicateClosingPoint(
        List<Vector3> polygon
    )
    {
        if (
            polygon.Count > 2 &&
            Vector3.Distance(
                polygon[0],
                polygon[polygon.Count - 1]
            ) < 0.001f
        )
        {
            polygon.RemoveAt(polygon.Count - 1);
        }
    }

    private void RemoveDuplicatePoints(
        List<Vector3> polygon
    )
    {
        for (int i = polygon.Count - 1; i > 0; i--)
        {
            if (
                Vector3.Distance(
                    polygon[i],
                    polygon[i - 1]
                ) < 0.00001f
            )
            {
                polygon.RemoveAt(i);
            }
        }
    }

    // =========================================================
    // CREATE MESH OBJECT
    // =========================================================

    private GameObject CreateMeshObject(
        string objectName,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        Material material,
        GameObject parent
    )
    {
        if (
            vertices == null ||
            vertices.Count == 0 ||
            triangles == null ||
            triangles.Count == 0
        )
        {
            Debug.LogWarning(
                $"OSMWorldGenerator: {objectName} has no geometry."
            );
            return null;
        }

        GameObject obj = new GameObject(objectName);

        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        MeshFilter meshFilter =
            obj.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            obj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = objectName + "_Mesh";

        if (vertices.Count > 65535)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        if (uvs != null && uvs.Count == vertices.Count)
        {
            mesh.SetUVs(0, uvs);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        if (material != null)
        {
            meshRenderer.sharedMaterial = material;

            // Building shell must be visible from either side even if
            // an OSM footprint has inconsistent winding.
            if (objectName == "BuildingMesh")
            {
                Material instance =
                    meshRenderer.material;

                if (
                    instance.HasProperty("_Cull")
                )
                {
                    instance.SetFloat(
                        "_Cull",
                        0f
                    );
                }
            }
        }

        return obj;
    }

    // =========================================================
    // CLEAR WORLD
    // =========================================================

    public void ClearWorld()
    {
        DestroyGeneratedParent(ref groundParent, "Ground");
        DestroyGeneratedParent(ref roadsParent, "Roads");
        DestroyGeneratedParent(ref waterwaysParent, "Waterways");
        DestroyGeneratedParent(ref naturalWaterParent, "NaturalWater");
        DestroyGeneratedParent(ref buildingsParent, "Buildings");
        DestroyGeneratedParent(ref roofsParent, "Roofs");
        DestroyGeneratedParent(ref facadesParent, "Facades");
    }

    private void DestroyGeneratedParent(
        ref GameObject parent,
        string parentName
    )
    {
        if (parent != null)
        {
            DestroyGenerated(parent);
            parent = null;
        }

        Transform existing = transform.Find(parentName);

        if (existing != null)
        {
            DestroyGenerated(existing.gameObject);
        }
    }

    // =========================================================
    // SAFE DESTROY
    // =========================================================

    private void DestroyGenerated(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(obj);
            return;
        }
#endif

        Destroy(obj);
    }

    // =========================================================
    // EDITOR
    // =========================================================

    [ContextMenu("Generate OSM World")]
    private void GenerateOSMFromEditor()
    {
        GenerateWorld();
    }
}
