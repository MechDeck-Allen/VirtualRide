using System;
using System.Collections.Generic;
using UnityEngine;

public class RouteWorldGenerator : MonoBehaviour
{
    // =========================================================
    // ROUTE SOURCE
    // =========================================================

    [Header("Route Source")]
    public RouteDataLoader routeLoader;

    [Tooltip("Assign snapped_route_data.json here. " +
             "This is preferred for Editor generation.")]
    public TextAsset routeJson;


    // =========================================================
    // WORLD SCALE
    // =========================================================

    [Header("World Scale")]
    [Tooltip("0.01 = 1 Unity unit represents 100 real-world metres.")]
    public float worldScale = 0.01f;

    [Header("Shared OSM Origin")]
    [Tooltip("Must match OSMWorldGenerator originLatitude.")]
    public double originLatitude = 9.9642475;

    [Tooltip("Must match OSMWorldGenerator originLongitude.")]
    public double originLongitude = 76.2454182;

    [Header("Ground Alignment")]
    [Tooltip("Must match OSMWorldGenerator.groundHeight.")]
    public float groundHeight = -0.02f;


    // =========================================================
    // CYCLING ROAD
    // =========================================================

    [Header("Cycling Road")]
    [Tooltip("Main cycling surface width in real-world metres.")]
    public float roadWidth = 5f;

    [Tooltip("Road thickness in real-world metres.")]
    public float roadHeight = 0.10f;


    // =========================================================
    // SHOULDERS
    // =========================================================

    [Header("Shoulders")]
    [Tooltip("Shoulder width on each side in real-world metres.")]
    public float shoulderWidth = 0.75f;

    [Tooltip("Shoulder height above the route plane in real-world metres.")]
    public float shoulderHeight = 0.05f;


    // =========================================================
    // EDGE MARKINGS
    // =========================================================

    [Header("Edge Markings")]
    public bool generateEdgeLines = true;

    [Tooltip("Edge line width in real-world metres.")]
    public float edgeLineWidth = 0.10f;

    [Tooltip("Edge line height above the road in real-world metres.")]
    public float edgeLineHeight = 0.025f;

    [Header("Center Marking")]
    public bool generateCenterLine = true;

    [Tooltip("Centre line width in real-world metres.")]
    public float centerLineWidth = 0.15f;

    [Tooltip("Centre line height above the road in real-world metres.")]
    public float centerLineHeight = 0.02f;

    [Tooltip("Centre dash length in real-world metres.")]
    public float dashLength = 3f;

    [Tooltip("Centre dash gap in real-world metres.")]
    public float dashGap = 3f;


    // =========================================================
    // MESH
    // =========================================================

    [Header("Mesh")]
    [Range(1, 10)]
    public int pointSkip = 3;


    // =========================================================
    // MATERIALS
    // =========================================================

    [Header("Materials")]
    public Material roadMaterial;
    public Material shoulderMaterial;
    public Material markingMaterial;


    // =========================================================
    // GENERATION
    // =========================================================

    [Header("Generation")]
    public bool generateOnStart = false;


    // =========================================================
    // INTERNAL DATA
    // =========================================================

    [Serializable]
    private class JsonRoutePoint
    {
        public double latitude;
        public double longitude;
    }


    [Serializable]
    private class JsonRouteData
    {
        public int version;
        public string routeName;
        public float distanceKm;
        public JsonRoutePoint[] points;
    }


    private readonly List<Vector3> routePoints =
        new List<Vector3>();


    private GameObject roadObject;
    private GameObject shoulderObject;
    private GameObject markingObject;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateRoute();
        }
    }


    // =========================================================
    // MAIN
    // =========================================================

    public void GenerateRoute()
    {
        // Geographic coordinates below are projected directly into the
        // shared OSM world coordinate system. RouteSystem therefore must
        // remain a neutral parent.
        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;

        transform.localScale =
            Vector3.one;

        if (worldScale <= 0f)
        {
            Debug.LogError(
                "RouteWorldGenerator: World Scale must be > 0."
            );

            return;
        }


        if (!LoadRoutePoints())
        {
            return;
        }


        if (routePoints.Count < 2)
        {
            Debug.LogError(
                "RouteWorldGenerator: Less than 2 usable route points."
            );

            return;
        }


        ClearGeneratedRoute();


        BuildRoadMesh();


        BuildShoulderMesh();


        if (generateEdgeLines || generateCenterLine)
        {
            BuildRoadMarkingMesh();
        }


        Debug.Log(
            "VirtualRide: Route generated successfully. " +
            "Points = " + routePoints.Count
        );

        Debug.Log(
            "VirtualRide: Centre marking pattern = " +
            dashLength + "m dash / " +
            dashGap + "m gap"
        );
    }


    // =========================================================
    // LOAD ROUTE
    // =========================================================

    private bool LoadRoutePoints()
    {
        routePoints.Clear();


        // -----------------------------------------------------
        // Preferred path: explicit JSON asset
        // -----------------------------------------------------

        if (routeJson != null)
        {
            JsonRouteData data;

            try
            {
                data =
                    JsonUtility.FromJson<JsonRouteData>(
                        routeJson.text
                    );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "RouteWorldGenerator: Failed to parse route JSON.\n" +
                    e
                );

                return false;
            }


            if (
                data != null &&
                data.points != null &&
                data.points.Length >= 2
            )
            {
                BuildRoutePointsFromJson(
                    data.points
                );

                Debug.Log(
                    "VirtualRide: Loaded route JSON '" +
                    routeJson.name +
                    "' with " +
                    data.points.Length +
                    " points."
                );

                return routePoints.Count >= 2;
            }


            Debug.LogError(
                "RouteWorldGenerator: Assigned route JSON " +
                "does not contain at least 2 points."
            );

            return false;
        }


        // -----------------------------------------------------
        // Fallback: RouteDataLoader
        // -----------------------------------------------------

        if (routeLoader == null)
        {
            Debug.LogError(
                "RouteWorldGenerator: No Route JSON or Route Loader assigned."
            );

            return false;
        }


        RouteData dataFromLoader =
            routeLoader.Data;


        if (
            dataFromLoader == null ||
            dataFromLoader.points == null ||
            dataFromLoader.points.Length < 2
        )
        {
            Debug.Log(
                "VirtualRide: RouteDataLoader has no loaded route. " +
                "Calling LoadRoute()."
            );


            routeLoader.LoadRoute();


            dataFromLoader =
                routeLoader.Data;
        }


        if (
            dataFromLoader == null ||
            dataFromLoader.points == null ||
            dataFromLoader.points.Length < 2
        )
        {
            Debug.LogError(
                "RouteWorldGenerator: RouteDataLoader still has " +
                "no valid route."
            );

            return false;
        }


        BuildRoutePointsFromLoader(
            dataFromLoader
        );


        return routePoints.Count >= 2;
    }


    // =========================================================
    // BUILD POINTS FROM JSON
    // =========================================================

    private void BuildRoutePointsFromJson(
        JsonRoutePoint[] points
    )
    {
        routePoints.Clear();


        double originLat =
            originLatitude;


        double originLon =
            originLongitude;


        const double metersPerDegreeLat =
            111320.0;


        double metersPerDegreeLon =
            111320.0 *
            Math.Cos(
                originLat *
                Mathf.Deg2Rad
            );


        int skip =
            Mathf.Max(
                1,
                pointSkip
            );


        // -----------------------------------------------------
        // Sample points
        // -----------------------------------------------------

        for (
            int i = 0;
            i < points.Length;
            i += skip
        )
        {
            JsonRoutePoint point =
                points[i];


            double xMeters =
                (
                    point.longitude -
                    originLon
                ) *
                metersPerDegreeLon;


            double zMeters =
                (
                    point.latitude -
                    originLat
                ) *
                metersPerDegreeLat;


            routePoints.Add(
                new Vector3(
                    (float)(
                        xMeters *
                        worldScale
                    ),
                    groundHeight,
                    (float)(
                        zMeters *
                        worldScale
                    )
                )
            );
        }


        // -----------------------------------------------------
        // Always include final route point
        // -----------------------------------------------------

        JsonRoutePoint last =
            points[
                points.Length - 1
            ];


        double lastXMeters =
            (
                last.longitude -
                originLon
            ) *
            metersPerDegreeLon;


        double lastZMeters =
            (
                last.latitude -
                originLat
            ) *
            metersPerDegreeLat;


        Vector3 finalPoint =
            new Vector3(
                (float)(
                    lastXMeters *
                    worldScale
                ),
                0f,
                (float)(
                    lastZMeters *
                    worldScale
                )
            );


        if (
            routePoints.Count == 0 ||
            Vector3.Distance(
                routePoints[
                    routePoints.Count - 1
                ],
                finalPoint
            ) > 0.0001f
        )
        {
            routePoints.Add(
                finalPoint
            );
        }
    }


    // =========================================================
    // BUILD POINTS FROM LOADER
    // =========================================================

    private void BuildRoutePointsFromLoader(
        RouteData data
    )
    {
        routePoints.Clear();


        double originLat =
            originLatitude;


        double originLon =
            originLongitude;


        const double metersPerDegreeLat =
            111320.0;


        double metersPerDegreeLon =
            111320.0 *
            Math.Cos(
                originLat *
                Mathf.Deg2Rad
            );


        int skip =
            Mathf.Max(
                1,
                pointSkip
            );


        for (
            int i = 0;
            i < data.points.Length;
            i += skip
        )
        {
            RoutePoint point =
                data.points[i];


            double xMeters =
                (
                    point.longitude -
                    originLon
                ) *
                metersPerDegreeLon;


            double zMeters =
                (
                    point.latitude -
                    originLat
                ) *
                metersPerDegreeLat;


            routePoints.Add(
                new Vector3(
                    (float)(
                        xMeters *
                        worldScale
                    ),
                    groundHeight,
                    (float)(
                        zMeters *
                        worldScale
                    )
                )
            );
        }


        RoutePoint last =
            data.points[
                data.points.Length - 1
            ];


        double lastXMeters =
            (
                last.longitude -
                originLon
            ) *
            metersPerDegreeLon;


        double lastZMeters =
            (
                last.latitude -
                originLat
            ) *
            metersPerDegreeLat;


        Vector3 finalPoint =
            new Vector3(
                (float)(
                    lastXMeters *
                    worldScale
                ),
                0f,
                (float)(
                    lastZMeters *
                    worldScale
                )
            );


        if (
            routePoints.Count == 0 ||
            Vector3.Distance(
                routePoints[
                    routePoints.Count - 1
                ],
                finalPoint
            ) > 0.0001f
        )
        {
            routePoints.Add(
                finalPoint
            );
        }
    }


    // =========================================================
    // FORWARD / RIGHT
    // =========================================================

    private Vector3 GetForward(
        int index
    )
    {
        Vector3 direction;


        if (index <= 0)
        {
            direction =
                routePoints[1] -
                routePoints[0];
        }
        else if (
            index >=
            routePoints.Count - 1
        )
        {
            direction =
                routePoints[
                    routePoints.Count - 1
                ] -
                routePoints[
                    routePoints.Count - 2
                ];
        }
        else
        {
            direction =
                routePoints[index + 1] -
                routePoints[index - 1];
        }


        direction.y = 0f;


        if (
            direction.sqrMagnitude <
            0.000001f
        )
        {
            direction =
                Vector3.forward;
        }


        return direction.normalized;
    }


    private Vector3 GetRight(
        int index
    )
    {
        return Vector3.Cross(
            Vector3.up,
            GetForward(index)
        ).normalized;
    }


    // =========================================================
    // MAIN ROAD
    // =========================================================

    private void BuildRoadMesh()
    {
        roadObject =
            new GameObject(
                "Generated Road"
            );


        roadObject.transform.SetParent(
            transform,
            false
        );


        MeshFilter filter =
            roadObject.AddComponent<MeshFilter>();


        MeshRenderer renderer =
            roadObject.AddComponent<MeshRenderer>();


        MeshCollider collider =
            roadObject.AddComponent<MeshCollider>();


        Mesh mesh =
            new Mesh();


        mesh.name =
            "VirtualRide_CyclingRoad";


        List<Vector3> vertices =
            new List<Vector3>();


        List<int> triangles =
            new List<int>();


        float halfWidth =
            roadWidth *
            worldScale *
            0.5f;


        float height =
            roadHeight *
            worldScale;


        for (
            int i = 0;
            i < routePoints.Count;
            i++
        )
        {
            Vector3 point =
                routePoints[i];


            Vector3 right =
                GetRight(i);


            Vector3 left =
                point -
                right *
                halfWidth;


            Vector3 rightPoint =
                point +
                right *
                halfWidth;


            left.y =
                point.y +
                height;


            rightPoint.y =
                point.y +
                height;


            vertices.Add(left);
            vertices.Add(rightPoint);
        }


        for (
            int i = 0;
            i < routePoints.Count - 1;
            i++
        )
        {
            int index =
                i * 2;


            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);


            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }


        mesh.SetVertices(vertices);

        mesh.SetTriangles(
            triangles,
            0
        );

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();


        filter.sharedMesh =
            mesh;


        collider.sharedMesh =
            mesh;


        Material material =
            roadMaterial;

        if (material == null)
        {
            material =
                FindRoadMaterial();
        }

        if (material != null)
        {
            renderer.sharedMaterial =
                material;

            Debug.Log(
                "RouteWorldGenerator: Road material applied = " +
                material.name
            );
        }
        else
        {
            Debug.LogError(
                "RouteWorldGenerator: No road material could be assigned."
            );
        }
    }


    // =========================================================
    // SHOULDERS
    // =========================================================

    private void BuildShoulderMesh()
    {
        shoulderObject =
            new GameObject(
                "Road Shoulders"
            );


        shoulderObject.transform.SetParent(
            transform,
            false
        );


        MeshFilter filter =
            shoulderObject.AddComponent<MeshFilter>();


        MeshRenderer renderer =
            shoulderObject.AddComponent<MeshRenderer>();


        Mesh mesh =
            new Mesh();


        mesh.name =
            "VirtualRide_RoadShoulders";


        List<Vector3> vertices =
            new List<Vector3>();


        List<int> triangles =
            new List<int>();


        float roadHalfWidth =
            roadWidth *
            worldScale *
            0.5f;


        float totalHalfWidth =
            (
                roadWidth +
                shoulderWidth * 2f
            ) *
            worldScale *
            0.5f;


        float height =
            (
                roadHeight +
                shoulderHeight
            ) *
            worldScale;


        for (
            int i = 0;
            i < routePoints.Count;
            i++
        )
        {
            Vector3 point =
                routePoints[i];


            Vector3 right =
                GetRight(i);


            Vector3 outerLeft =
                point -
                right *
                totalHalfWidth;


            Vector3 innerLeft =
                point -
                right *
                roadHalfWidth;


            Vector3 innerRight =
                point +
                right *
                roadHalfWidth;


            Vector3 outerRight =
                point +
                right *
                totalHalfWidth;


            outerLeft.y =
                point.y +
                height;


            innerLeft.y =
                point.y +
                height;


            innerRight.y =
                point.y +
                height;


            outerRight.y =
                point.y +
                height;


            vertices.Add(outerLeft);
            vertices.Add(innerLeft);
            vertices.Add(innerRight);
            vertices.Add(outerRight);
        }


        for (
            int i = 0;
            i < routePoints.Count - 1;
            i++
        )
        {
            int a =
                i * 4;


            int b =
                (i + 1) * 4;


            // Left shoulder
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(a + 1);


            triangles.Add(a + 1);
            triangles.Add(b);
            triangles.Add(b + 1);


            // Right shoulder
            triangles.Add(a + 2);
            triangles.Add(b + 2);
            triangles.Add(a + 3);


            triangles.Add(a + 3);
            triangles.Add(b + 2);
            triangles.Add(b + 3);
        }


        mesh.SetVertices(vertices);

        mesh.SetTriangles(
            triangles,
            0
        );

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();


        filter.sharedMesh =
            mesh;


        Material material =
            shoulderMaterial != null
            ? shoulderMaterial
            : roadMaterial;


        if (material != null)
        {
            renderer.sharedMaterial =
                material;
        }
    }


    // =========================================================
    // EDGE LINES
    // =========================================================

    private void BuildRoadMarkingMesh()
    {
        markingObject =
            new GameObject(
                "Road Markings"
            );


        markingObject.transform.SetParent(
            transform,
            false
        );


        MeshFilter filter =
            markingObject.AddComponent<MeshFilter>();


        MeshRenderer renderer =
            markingObject.AddComponent<MeshRenderer>();


        Mesh mesh =
            new Mesh();


        mesh.name =
            "VirtualRide_RoadEdgeLines";


        List<Vector3> vertices =
            new List<Vector3>();


        List<int> triangles =
            new List<int>();


        float roadHalfWidth =
            roadWidth *
            worldScale *
            0.5f;


        float lineHalfWidth =
            edgeLineWidth *
            worldScale *
            0.5f;


        float height =
            (
                roadHeight +
                edgeLineHeight
            ) *
            worldScale;


        for (
            int i = 0;
            i < routePoints.Count - 1;
            i++
        )
        {
            if (generateEdgeLines)
            {
                AddEdgeSegment(
                    routePoints[i],
                    routePoints[i + 1],
                    GetRight(i),
                    -(
                        roadHalfWidth -
                        lineHalfWidth
                    ),
                    lineHalfWidth,
                    height,
                    vertices,
                    triangles
                );


                AddEdgeSegment(
                    routePoints[i],
                    routePoints[i + 1],
                    GetRight(i),
                    (
                        roadHalfWidth -
                        lineHalfWidth
                    ),
                    lineHalfWidth,
                    height,
                    vertices,
                    triangles
                );
            }
        }


        // -----------------------------------------------------
        // Dashed centre line
        //
        // Uses a finite number of dash intervals calculated from
        // the route's cumulative distance. There is deliberately
        // NO while-loop, avoiding the previous hang.
        // -----------------------------------------------------

        if (generateCenterLine)
        {
            BuildDashedCenterLine(
                vertices,
                triangles
            );
        }


        if (
            vertices.Count == 0 ||
            triangles.Count == 0
        )
        {
            DestroyGenerated(
                markingObject
            );

            markingObject = null;

            return;
        }


        mesh.SetVertices(vertices);

        mesh.SetTriangles(
            triangles,
            0
        );

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();


        filter.sharedMesh =
            mesh;


        Material material =
            markingMaterial;

        if (material == null)
        {
            material =
                CreateVisibleMarkingMaterial(
                    roadMaterial
                );
        }

        if (material != null)
        {
            renderer.sharedMaterial =
                material;
        }

        Debug.Log(
            "RouteWorldGenerator: Road markings vertices = " +
            vertices.Count +
            ", triangles = " +
            triangles.Count
        );
    }


    // =========================================================
    // BUILD DASHED CENTRE LINE
    // =========================================================

    private void BuildDashedCenterLine(
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        float dash =
            Mathf.Max(
                0.10f,
                dashLength
            );


        float gap =
            Mathf.Max(
                0.10f,
                dashGap
            );


        float pattern =
            dash +
            gap;


        float halfWidth =
            centerLineWidth *
            worldScale *
            0.5f;


        float height =
            (
                roadHeight +
                centerLineHeight
            ) *
            worldScale;


        float routeDistanceMeters =
            0f;


        for (
            int i = 0;
            i < routePoints.Count - 1;
            i++
        )
        {
            Vector3 start =
                routePoints[i];


            Vector3 end =
                routePoints[i + 1];


            Vector3 segment =
                end - start;


            segment.y = 0f;


            float segmentUnityLength =
                segment.magnitude;


            if (
                segmentUnityLength <
                0.000001f
            )
            {
                continue;
            }


            Vector3 direction =
                segment /
                segmentUnityLength;


            float segmentMeters =
                segmentUnityLength /
                Mathf.Max(
                    worldScale,
                    0.000001f
                );


            float segmentStartMeters =
                routeDistanceMeters;


            float segmentEndMeters =
                routeDistanceMeters +
                segmentMeters;


            // Only process dash intervals that can overlap
            // this segment. This loop is always finite.
            int firstDashIndex =
                Mathf.FloorToInt(
                    segmentStartMeters /
                    pattern
                );


            int lastDashIndex =
                Mathf.FloorToInt(
                    (
                        segmentEndMeters -
                        0.000001f
                    ) /
                    pattern
                );


            for (
                int dashIndex =
                    firstDashIndex;
                dashIndex <= lastDashIndex;
                dashIndex++
            )
            {
                float dashStartMeters =
                    dashIndex *
                    pattern;


                float dashEndMeters =
                    dashStartMeters +
                    dash;


                float overlapStart =
                    Mathf.Max(
                        segmentStartMeters,
                        dashStartMeters
                    );


                float overlapEnd =
                    Mathf.Min(
                        segmentEndMeters,
                        dashEndMeters
                    );


                if (
                    overlapEnd <=
                    overlapStart
                )
                {
                    continue;
                }


                float localStart =
                    overlapStart -
                    segmentStartMeters;


                float localEnd =
                    overlapEnd -
                    segmentStartMeters;


                Vector3 dashStart =
                    start +
                    direction *
                    (
                        localStart *
                        worldScale
                    );


                Vector3 dashEnd =
                    start +
                    direction *
                    (
                        localEnd *
                        worldScale
                    );


                AddCenterSegment(
                    dashStart,
                    dashEnd,
                    halfWidth,
                    height,
                    vertices,
                    triangles
                );
            }


            routeDistanceMeters =
                segmentEndMeters;
        }
    }


    // =========================================================
    // ADD CENTER SEGMENT
    // =========================================================

    private void AddCenterSegment(
        Vector3 start,
        Vector3 end,
        float halfWidth,
        float height,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        Vector3 direction =
            end - start;


        direction.y = 0f;


        if (
            direction.sqrMagnitude <
            0.000001f
        )
        {
            return;
        }


        direction.Normalize();


        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                direction
            ).normalized;


        Vector3 leftStart =
            start -
            right *
            halfWidth;


        Vector3 rightStart =
            start +
            right *
            halfWidth;


        Vector3 leftEnd =
            end -
            right *
            halfWidth;


        Vector3 rightEnd =
            end +
            right *
            halfWidth;


        leftStart.y += height;
        rightStart.y += height;
        leftEnd.y += height;
        rightEnd.y += height;


        int index =
            vertices.Count;


        vertices.Add(leftStart);
        vertices.Add(rightStart);
        vertices.Add(leftEnd);
        vertices.Add(rightEnd);


        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 1);


        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }


    // =========================================================
    // ADD EDGE SEGMENT
    // =========================================================

    private void AddEdgeSegment(
        Vector3 start,
        Vector3 end,
        Vector3 right,
        float offset,
        float halfWidth,
        float height,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        Vector3 centerStart =
            start +
            right *
            offset;


        Vector3 centerEnd =
            end +
            right *
            offset;


        Vector3 direction =
            centerEnd -
            centerStart;


        direction.y = 0f;


        if (
            direction.sqrMagnitude <
            0.000001f
        )
        {
            return;
        }


        direction.Normalize();


        Vector3 localRight =
            Vector3.Cross(
                Vector3.up,
                direction
            ).normalized;


        Vector3 a =
            centerStart -
            localRight *
            halfWidth;


        Vector3 b =
            centerStart +
            localRight *
            halfWidth;


        Vector3 c =
            centerEnd -
            localRight *
            halfWidth;


        Vector3 d =
            centerEnd +
            localRight *
            halfWidth;


        a.y =
            centerStart.y +
            height;


        b.y =
            centerStart.y +
            height;


        c.y =
            centerEnd.y +
            height;


        d.y =
            centerEnd.y +
            height;


        int index =
            vertices.Count;


        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);


        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 1);


        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }


    // =========================================================
    // MATERIAL
    // =========================================================


    private Material CreateVisibleMarkingMaterial(
        Material fallback
    )
    {
        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit"
            );

        if (shader == null)
        {
            return fallback;
        }

        Material material =
            new Material(shader);

        material.name =
            "VirtualRide_Runtime_RoadMarking";

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                Color.white
            );
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat(
                "_Metallic",
                0f
            );
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                0.15f
            );
        }

        return material;
    }


    private Material FindRoadMaterial()
    {
        GameObject road =
            GameObject.Find(
                "RoadSegment"
            );


        if (road != null)
        {
            MeshRenderer renderer =
                road.GetComponent<MeshRenderer>();


            if (
                renderer != null &&
                renderer.sharedMaterial != null
            )
            {
                return renderer.sharedMaterial;
            }
        }


        return null;
    }


    // =========================================================
    // CLEAR
    // =========================================================

    public void ClearRoute()
    {
        routePoints.Clear();

        ClearGeneratedRoute();
    }


    private void ClearGeneratedRoute()
    {
        Transform road =
            transform.Find(
                "Generated Road"
            );


        Transform shoulders =
            transform.Find(
                "Road Shoulders"
            );


        Transform markings =
            transform.Find(
                "Road Markings"
            );


        if (road != null)
        {
            DestroyGenerated(
                road.gameObject
            );
        }


        if (shoulders != null)
        {
            DestroyGenerated(
                shoulders.gameObject
            );
        }


        if (markings != null)
        {
            DestroyGenerated(
                markings.gameObject
            );
        }


        roadObject = null;
        shoulderObject = null;
        markingObject = null;
    }


    private void DestroyGenerated(
        GameObject obj
    )
    {
        if (obj == null)
        {
            return;
        }


#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.DestroyObjectImmediate(
                obj
            );

            return;
        }
#endif


        Destroy(obj);
    }


    // =========================================================
    // CONTEXT MENUS
    // =========================================================

    [ContextMenu("Generate Route")]
    private void GenerateRouteFromEditor()
    {
        GenerateRoute();
    }


    [ContextMenu("Clear Generated Route")]
    private void ClearGeneratedRouteFromEditor()
    {
        ClearRoute();
    }
}
