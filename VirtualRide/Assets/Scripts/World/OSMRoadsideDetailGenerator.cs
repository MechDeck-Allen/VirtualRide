using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

public class OSMRoadsideDetailGenerator : MonoBehaviour
{
    [Header("Processed Chunk")]
    public TextAsset processedChunk;

    [Header("Route JSON")]
    public TextAsset routeJson;

    [Header("Shared Coordinate Origin")]
    public double originLatitude = 9.9642475;
    public double originLongitude = 76.2454182;

    [Header("World Scale")]
    public float worldScale = 0.01f;

    [Header("Ground Alignment")]
    public float groundHeight = -0.02f;

    [Header("Visible Corridor")]
    [Tooltip("Only OSM roadside features within this real-world distance from the cycling route are generated.")]
    public float corridorRadiusMeters = 35f;

    [Header("Chunk Route Range")]
    public float chunkStartKm = 0f;
    public float chunkEndKm = 0f;

    [Header("Trees")]
    public float treeTrunkHeight = 3f;
    public float treeCanopyHeight = 4f;
    public float treeCanopyRadius = 1.75f;
    public float treeTrunkRadius = 0.15f;
    public int treeSegments = 7;

    [Header("Procedural Poles")]
    public bool generateProceduralPoles = true;
    public float poleSpacingMeters = 35f;
    public float poleHeightMeters = 6f;
    public float poleRadiusMeters = 0.09f;

    [Tooltip("Distance from cycling route centerline. 5 m keeps poles outside the 5 m road + 0.75 m shoulders.")]
    public float poleOffsetFromRouteMeters = 5.0f;

    [Header("Procedural Lamps")]
    public bool generateProceduralLamps = false;
    public float lampSpacingMeters = 45f;
    public float lampHeightMeters = 5.5f;
    public float lampPoleRadiusMeters = 0.07f;

    [Header("Barrier / Fence")]
    public float fencePostSpacingMeters = 3f;
    public float fenceHeightMeters = 1.2f;
    public float fencePostRadiusMeters = 0.05f;
    public float fenceRailRadiusMeters = 0.035f;

    [Header("Bridge Detail")]
    public bool generateBridgeRails = true;
    public float bridgeRailHeightMeters = 1.0f;
    public float bridgeRailSpacingMeters = 3f;

    [Header("Materials")]
    public Material trunkMaterial;
    public Material leavesMaterial;
    public Material poleMaterial;
    public Material lampMaterial;
    public Material fenceMaterial;
    public Material bridgeRailMaterial;

    [Header("Generation")]
    public bool generateOnStart = false;


    [Serializable]
    private class Point
    {
        public double latitude;
        public double longitude;
    }

    [Serializable]
    private class RouteData
    {
        public Point[] points;
    }


    private readonly List<Vector3> routePoints =
        new List<Vector3>();


    private readonly List<Vector3> treeTrunkVertices =
        new List<Vector3>();

    private readonly List<int> treeTrunkTriangles =
        new List<int>();

    private readonly List<Vector3> treeCanopyVertices =
        new List<Vector3>();

    private readonly List<int> treeCanopyTriangles =
        new List<int>();

    private readonly List<Vector3> detailVertices =
        new List<Vector3>();

    private readonly List<int> detailTriangles =
        new List<int>();

    private readonly List<Vector3> fenceVertices =
        new List<Vector3>();

    private readonly List<int> fenceTriangles =
        new List<int>();

    private readonly List<Vector3> bridgeVertices =
        new List<Vector3>();

    private readonly List<int> bridgeTriangles =
        new List<int>();


    private void Start()
    {
        if (generateOnStart)
        {
            GenerateDetails();
        }
    }


    public void GenerateDetails()
    {
        ClearGenerated();


        if (processedChunk == null)
        {
            Debug.LogError(
                "OSMRoadsideDetailGenerator: processedChunk is not assigned."
            );
            return;
        }


        LoadRoute();


        if (routePoints.Count < 2)
        {
            Debug.LogError(
                "OSMRoadsideDetailGenerator: Route JSON contains fewer than 2 points."
            );
            return;
        }


        // Actual OSM trees / poles / lamps / barriers / bridges.
        GenerateTreeFeatures();
        GeneratePointFeatures();
        GenerateBarrierFeatures();
        GenerateBridgeFeatures();

        // Procedural roadside infrastructure if OSM is sparse.
        if (generateProceduralPoles)
        {
            GenerateRoutePoles();
        }

        // Procedural lamps are attached directly to procedural poles.
        // This keeps every generated pole visually complete.


        CreateMeshObject(
            "RoadsideTrees_Trunks",
            treeTrunkVertices,
            treeTrunkTriangles,
            trunkMaterial
        );

        CreateMeshObject(
            "RoadsideTrees_Canopies",
            treeCanopyVertices,
            treeCanopyTriangles,
            leavesMaterial
        );

        CreateMeshObject(
            "RoadsideInfrastructure",
            detailVertices,
            detailTriangles,
            poleMaterial
        );

        CreateMeshObject(
            "RoadsideFences",
            fenceVertices,
            fenceTriangles,
            fenceMaterial
        );

        CreateMeshObject(
            "BridgeRails",
            bridgeVertices,
            bridgeTriangles,
            bridgeRailMaterial
        );


        Debug.Log(
            "OSMRoadsideDetailGenerator: generation complete."
        );
    }


    // =========================================================
    // ROUTE
    // =========================================================

    private void LoadRoute()
    {
        routePoints.Clear();

        if (routeJson == null)
        {
            return;
        }

        try
        {
            RouteData data =
                JsonUtility.FromJson<RouteData>(
                    routeJson.text
                );

            if (
                data != null &&
                data.points != null
            )
            {
                foreach (Point point in data.points)
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
        catch (Exception)
        {
            // Text fallback below.
        }

        if (routePoints.Count >= 2)
        {
            return;
        }

        // Fallback: extract latitude/longitude pairs directly.
        MatchCollection matches =
            Regex.Matches(
                routeJson.text,
                @"""latitude""\s*:\s*(-?\d+(?:\.\d+)?).*?""longitude""\s*:\s*(-?\d+(?:\.\d+)?)",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );

        foreach (Match match in matches)
        {
            if (
                double.TryParse(
                    match.Groups[1].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double lat
                )
                &&
                double.TryParse(
                    match.Groups[2].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double lon
                )
            )
            {
                routePoints.Add(
                    GeoToUnity(
                        lat,
                        lon
                    )
                );
            }
        }
    }


    // =========================================================
    // TREES
    // =========================================================

    private void GenerateTreeFeatures()
    {
        Match array =
            Regex.Match(
                processedChunk.text,
                @"""trees""\s*:\s*\[(.*?)\]",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );

        if (!array.Success)
        {
            return;
        }

        MatchCollection objects =
            Regex.Matches(
                array.Groups[1].Value,
                @"\{(.*?)\}",
                RegexOptions.Singleline
            );

        foreach (Match match in objects)
        {
            string obj =
                match.Groups[1].Value;

            if (
                !TryGetCoordinate(
                    obj,
                    out double lat,
                    out double lon
                )
            )
            {
                continue;
            }

            Vector3 position =
                GeoToUnity(
                    lat,
                    lon
                );

            position.y =
                groundHeight;

            if (
                DistanceToRouteMeters(position)
                > corridorRadiusMeters
            )
            {
                continue;
            }

            AddTree(
                position
            );
        }
    }


    private void AddTree(
        Vector3 position
    )
    {
        int seed =
            Mathf.Abs(
                (
                    position.x.GetHashCode() * 397
                )
                ^
                position.z.GetHashCode()
            );

        System.Random random =
            new System.Random(seed);

        float heightScale =
            Mathf.Lerp(
                0.80f,
                1.20f,
                (float)random.NextDouble()
            );

        float radiusScale =
            Mathf.Lerp(
                0.80f,
                1.20f,
                (float)random.NextDouble()
            );

        float trunkHeight =
            treeTrunkHeight *
            heightScale;

        float canopyHeight =
            treeCanopyHeight *
            heightScale;

        float canopyRadius =
            treeCanopyRadius *
            radiusScale;


        AddCylinder(
            position,
            Vector3.up,
            trunkHeight,
            treeTrunkRadius,
            treeSegments,
            treeTrunkVertices,
            treeTrunkTriangles
        );

        AddCone(
            position +
            Vector3.up *
            (
                trunkHeight *
                worldScale
            ),
            canopyHeight,
            canopyRadius,
            treeSegments,
            treeCanopyVertices,
            treeCanopyTriangles
        );
    }


    // =========================================================
    // OSM POINT FEATURES
    // =========================================================

    private void GeneratePointFeatures()
    {
        GeneratePointArray(
            "utilityPoles",
            false
        );

        GeneratePointArray(
            "streetLamps",
            true
        );
    }


    private void GeneratePointArray(
        string arrayName,
        bool lamp
    )
    {
        Match array =
            Regex.Match(
                processedChunk.text,
                @"""" +
                arrayName +
                @"""\s*:\s*\[(.*?)\]",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );

        if (!array.Success)
        {
            return;
        }

        MatchCollection objects =
            Regex.Matches(
                array.Groups[1].Value,
                @"\{(.*?)\}",
                RegexOptions.Singleline
            );

        foreach (Match match in objects)
        {
            if (
                !TryGetCoordinate(
                    match.Groups[1].Value,
                    out double lat,
                    out double lon
                )
            )
            {
                continue;
            }

            Vector3 position =
                GeoToUnity(
                    lat,
                    lon
                );

            if (
                DistanceToRouteMeters(position)
                > corridorRadiusMeters
            )
            {
                continue;
            }

            if (lamp)
            {
                AddLamp(
                    position
                );
            }
            else
            {
                AddPole(
                    position
                );
            }
        }
    }


    // =========================================================
    // PROCEDURAL POLES / LAMPS
    // =========================================================

    private void GenerateRoutePoles()
    {
        if (poleSpacingMeters <= 0f)
        {
            return;
        }

        float total =
            RouteLengthMeters();

        float startMeters =
            Mathf.Max(
                0f,
                chunkStartKm * 1000f -
                corridorRadiusMeters
            );

        float endMeters =
            chunkEndKm > chunkStartKm
                ? Mathf.Min(
                    total,
                    chunkEndKm * 1000f +
                    corridorRadiusMeters
                )
                : total;

        for (
            float d = startMeters + 15f;
            d < endMeters;
            d += poleSpacingMeters
        )
        {
            Vector3 position =
                RoutePositionAtDistance(
                    d
                );

            Vector3 side =
                RouteRightAtDistance(
                    d
                );

            float sideSign =
                ((int)(
                    d /
                    poleSpacingMeters
                ) % 2 == 0)
                    ? -1f
                    : 1f;

            position +=
                side *
                (
                    sideSign *
                    poleOffsetFromRouteMeters *
                    worldScale
                );

            position.y =
                groundHeight;

            AddPole(
                position
            );
        }
    }


    private void GenerateRouteLamps()
    {
        if (lampSpacingMeters <= 0f)
        {
            return;
        }

        float total =
            RouteLengthMeters();

        float startMeters =
            Mathf.Max(
                0f,
                chunkStartKm * 1000f -
                corridorRadiusMeters
            );

        float endMeters =
            chunkEndKm > chunkStartKm
                ? Mathf.Min(
                    total,
                    chunkEndKm * 1000f +
                    corridorRadiusMeters
                )
                : total;

        for (
            float d = startMeters + 30f;
            d < endMeters;
            d += lampSpacingMeters
        )
        {
            Vector3 position =
                RoutePositionAtDistance(
                    d
                );

            Vector3 side =
                RouteRightAtDistance(
                    d
                );

            position +=
                side *
                4f *
                worldScale;

            AddLamp(
                position
            );
        }
    }


    private void AddPole(
        Vector3 position
    )
    {
        AddCylinder(
            position,
            Vector3.up,
            poleHeightMeters,
            poleRadiusMeters,
            6,
            detailVertices,
            detailTriangles
        );

        // Every procedural utility pole also gets a lamp arm/head.
        AddLamp(
            position
        );
    }


    private void AddLamp(
        Vector3 position
    )
    {
        AddCylinder(
            position,
            Vector3.up,
            lampHeightMeters,
            lampPoleRadiusMeters,
            6,
            detailVertices,
            detailTriangles
        );

        Vector3 armStart =
            position +
            Vector3.up *
            (
                lampHeightMeters *
                0.92f *
                worldScale
            );

        // Point the lamp arm toward the cycling road, not along it.
        // This works for poles on either side of the route.
        Vector3 closestRoadPoint =
            ClosestPointOnRoute(
                position
            );

        Vector3 armDirection =
            closestRoadPoint -
            position;

        armDirection.y = 0f;

        if (armDirection.sqrMagnitude < 0.00001f)
        {
            armDirection =
                Vector3.forward;
        }

        armDirection.Normalize();

        Vector3 armEnd =
            armStart +
            armDirection *
            2f *
            worldScale;

        AddCylinderBetween(
            armStart,
            armEnd,
            lampPoleRadiusMeters,
            6,
            detailVertices,
            detailTriangles
        );

        AddSphere(
            armEnd +
            Vector3.down *
            0.18f *
            worldScale,
            0.16f,
            6,
            5,
            detailVertices,
            detailTriangles
        );
    }


    // =========================================================
    // BARRIERS / FENCES
    // =========================================================

    private void GenerateBarrierFeatures()
    {
        Match array =
            Regex.Match(
                processedChunk.text,
                @"""barriers""\s*:\s*\[(.*?)\]",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );

        if (!array.Success)
        {
            return;
        }

        MatchCollection objects =
            Regex.Matches(
                array.Groups[1].Value,
                @"\{(.*?)\}",
                RegexOptions.Singleline
            );

        foreach (Match match in objects)
        {
            string obj =
                match.Groups[1].Value;

            Match pointsMatch =
                Regex.Match(
                    obj,
                    @"""points""\s*:\s*\[(.*?)\]",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline
                );

            if (!pointsMatch.Success)
            {
                continue;
            }

            List<Vector3> points =
                ParsePointArray(
                    pointsMatch.Groups[1].Value
                );

            for (
                int i = 0;
                i < points.Count - 1;
                i++
            )
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];

                Vector3 mid =
                    (a + b) *
                    0.5f;

                if (
                    DistanceToRouteMeters(mid)
                    > corridorRadiusMeters
                )
                {
                    continue;
                }

                AddFenceSegment(
                    a,
                    b
                );
            }
        }
    }


    private void AddFenceSegment(
        Vector3 a,
        Vector3 b
    )
    {
        float lengthMeters =
            Vector3.Distance(
                a,
                b
            ) /
            Mathf.Max(
                0.000001f,
                worldScale
            );

        int posts =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    lengthMeters /
                    Mathf.Max(
                        0.5f,
                        fencePostSpacingMeters
                    )
                )
            );

        for (
            int i = 0;
            i <= posts;
            i++
        )
        {
            float t =
                i /
                (float)posts;

            Vector3 p =
                Vector3.Lerp(
                    a,
                    b,
                    t
                );

            AddCylinder(
                p,
                Vector3.up,
                fenceHeightMeters,
                fencePostRadiusMeters,
                5,
                fenceVertices,
                fenceTriangles
            );
        }

        Vector3 elevatedA =
            a +
            Vector3.up *
            (
                fenceHeightMeters *
                0.65f *
                worldScale
            );

        Vector3 elevatedB =
            b +
            Vector3.up *
            (
                fenceHeightMeters *
                0.65f *
                worldScale
            );

        AddCylinderBetween(
            elevatedA,
            elevatedB,
            fenceRailRadiusMeters,
            5,
            fenceVertices,
            fenceTriangles
        );
    }


    // =========================================================
    // BRIDGES
    // =========================================================

    private void GenerateBridgeFeatures()
    {
        if (!generateBridgeRails)
        {
            return;
        }

        Match array =
            Regex.Match(
                processedChunk.text,
                @"""bridges""\s*:\s*\[(.*?)\]",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );

        if (!array.Success)
        {
            return;
        }

        MatchCollection objects =
            Regex.Matches(
                array.Groups[1].Value,
                @"\{(.*?)\}",
                RegexOptions.Singleline
            );

        foreach (Match match in objects)
        {
            Match pointsMatch =
                Regex.Match(
                    match.Groups[1].Value,
                    @"""points""\s*:\s*\[(.*?)\]",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline
                );

            if (!pointsMatch.Success)
            {
                continue;
            }

            List<Vector3> points =
                ParsePointArray(
                    pointsMatch.Groups[1].Value
                );

            for (
                int i = 0;
                i < points.Count - 1;
                i++
            )
            {
                Vector3 a =
                    points[i];

                Vector3 b =
                    points[i + 1];

                Vector3 mid =
                    (a + b) *
                    0.5f;

                if (
                    DistanceToRouteMeters(mid)
                    > corridorRadiusMeters
                )
                {
                    continue;
                }

                AddBridgeRailSegment(
                    a,
                    b
                );
            }
        }
    }


    private void AddBridgeRailSegment(
        Vector3 a,
        Vector3 b
    )
    {
        Vector3 direction =
            b - a;

        direction.y = 0f;

        if (
            direction.sqrMagnitude
            < 0.000001f
        )
        {
            return;
        }

        direction.Normalize();

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                direction
            ).normalized;

        float halfRoad =
            2.0f *
            worldScale;

        Vector3 leftA =
            a +
            side *
            halfRoad +
            Vector3.up *
            (
                bridgeRailHeightMeters *
                worldScale
            );

        Vector3 leftB =
            b +
            side *
            halfRoad +
            Vector3.up *
            (
                bridgeRailHeightMeters *
                worldScale
            );

        Vector3 rightA =
            a -
            side *
            halfRoad +
            Vector3.up *
            (
                bridgeRailHeightMeters *
                worldScale
            );

        Vector3 rightB =
            b -
            side *
            halfRoad +
            Vector3.up *
            (
                bridgeRailHeightMeters *
                worldScale
            );

        AddCylinderBetween(
            leftA,
            leftB,
            0.05f,
            6,
            bridgeVertices,
            bridgeTriangles
        );

        AddCylinderBetween(
            rightA,
            rightB,
            0.05f,
            6,
            bridgeVertices,
            bridgeTriangles
        );
    }


    // =========================================================
    // ROUTE MATH
    // =========================================================

    private float RouteLengthMeters()
    {
        float total = 0f;

        for (
            int i = 0;
            i < routePoints.Count - 1;
            i++
        )
        {
            total +=
                Vector3.Distance(
                    routePoints[i],
                    routePoints[i + 1]
                ) /
                Mathf.Max(
                    worldScale,
                    0.000001f
                );
        }

        return total;
    }


    private Vector3 RoutePositionAtDistance(
        float meters
    )
    {
        float remaining =
            meters;

        for (
            int i = 0;
            i < routePoints.Count - 1;
            i++
        )
        {
            float length =
                Vector3.Distance(
                    routePoints[i],
                    routePoints[i + 1]
                ) /
                Mathf.Max(
                    worldScale,
                    0.000001f
                );

            if (remaining <= length)
            {
                float t =
                    length <= 0.001f
                        ? 0f
                        : remaining /
                          length;

                return Vector3.Lerp(
                    routePoints[i],
                    routePoints[i + 1],
                    t
                );
            }

            remaining -=
                length;
        }

        return routePoints[
            routePoints.Count - 1
        ];
    }


    private Vector3 RouteRightAtDistance(
        float meters
    )
    {
        Vector3 direction =
            RouteTangentAtDistance(
                meters
            );

        return Vector3.Cross(
            Vector3.up,
            direction
        ).normalized;
    }


    private Vector3 RouteTangentAtDistance(
        float meters
    )
    {
        float sample =
            Mathf.Max(
                1f,
                meters
            );

        Vector3 a =
            RoutePositionAtDistance(
                Mathf.Max(
                    0f,
                    sample - 0.5f
                )
            );

        Vector3 b =
            RoutePositionAtDistance(
                sample + 0.5f
            );

        Vector3 direction =
            b - a;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.000001f)
        {
            return Vector3.forward;
        }

        return direction.normalized;
    }


    private Vector3 ClosestPointOnRoute(
        Vector3 position
    )
    {
        float bestDistance =
            float.MaxValue;

        Vector3 bestPoint =
            position;

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

            Vector3 ab =
                b - a;

            ab.y = 0f;

            float lenSq =
                ab.sqrMagnitude;

            if (lenSq < 0.000001f)
            {
                continue;
            }

            float t =
                Vector3.Dot(
                    position - a,
                    ab
                ) /
                lenSq;

            t =
                Mathf.Clamp01(
                    t
                );

            Vector3 closest =
                Vector3.Lerp(
                    a,
                    b,
                    t
                );

            float distance =
                Vector3.Distance(
                    position,
                    closest
                );

            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                bestPoint =
                    closest;
            }
        }

        return bestPoint;
    }


    private Vector3 RouteTangentAtPosition(
        Vector3 position
    )
    {
        float bestDistance =
            float.MaxValue;

        Vector3 bestDirection =
            Vector3.forward;

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

            Vector3 ab =
                b - a;

            ab.y = 0f;

            float lenSq =
                ab.sqrMagnitude;

            if (lenSq < 0.000001f)
            {
                continue;
            }

            float t =
                Vector3.Dot(
                    position - a,
                    ab
                ) /
                lenSq;

            t =
                Mathf.Clamp01(
                    t
                );

            Vector3 closest =
                Vector3.Lerp(
                    a,
                    b,
                    t
                );

            float distance =
                Vector3.Distance(
                    position,
                    closest
                );

            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                bestDirection =
                    ab.normalized;
            }
        }

        return bestDirection;
    }


    private float DistanceToRouteMeters(
        Vector3 position
    )
    {
        float bestUnity =
            float.MaxValue;

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

            Vector3 ab =
                b - a;

            ab.y = 0f;

            float lenSq =
                ab.sqrMagnitude;

            if (lenSq < 0.000001f)
            {
                continue;
            }

            float t =
                Vector3.Dot(
                    position - a,
                    ab
                ) /
                lenSq;

            t =
                Mathf.Clamp01(
                    t
                );

            Vector3 closest =
                a +
                ab *
                t;

            float distance =
                Vector3.Distance(
                    position,
                    closest
                );

            if (distance < bestUnity)
            {
                bestUnity =
                    distance;
            }
        }

        return
            bestUnity /
            Mathf.Max(
                0.000001f,
                worldScale
            );
    }


    // =========================================================
    // GEO
    // =========================================================

    private Vector3 GeoToUnity(
        double latitude,
        double longitude
    )
    {
        double metersPerDegreeLongitude =
            111320.0 *
            Math.Cos(
                originLatitude *
                Mathf.Deg2Rad
            );

        double xMeters =
            (
                longitude -
                originLongitude
            ) *
            metersPerDegreeLongitude;

        double zMeters =
            (
                latitude -
                originLatitude
            ) *
            111320.0;

        return new Vector3(
            (float)(
                xMeters *
                worldScale
            ),
            0f,
            (float)(
                zMeters *
                worldScale
            )
        );
    }


    // =========================================================
    // JSON HELPERS
    // =========================================================

    private bool TryGetCoordinate(
        string text,
        out double latitude,
        out double longitude
    )
    {
        latitude =
            0;

        longitude =
            0;

        Match lat =
            Regex.Match(
                text,
                @"""latitude""\s*:\s*(-?\d+(?:\.\d+)?)",
                RegexOptions.IgnoreCase
            );

        Match lon =
            Regex.Match(
                text,
                @"""longitude""\s*:\s*(-?\d+(?:\.\d+)?)",
                RegexOptions.IgnoreCase
            );

        if (
            !lat.Success ||
            !lon.Success
        )
        {
            return false;
        }

        if (
            !double.TryParse(
                lat.Groups[1].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out latitude
            )
        )
        {
            return false;
        }

        if (
            !double.TryParse(
                lon.Groups[1].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out longitude
            )
        )
        {
            return false;
        }

        return true;
    }


    private List<Vector3> ParsePointArray(
        string arrayText
    )
    {
        List<Vector3> points =
            new List<Vector3>();

        MatchCollection objects =
            Regex.Matches(
                arrayText,
                @"\{(.*?)\}",
                RegexOptions.Singleline
            );

        foreach (Match match in objects)
        {
            if (
                TryGetCoordinate(
                    match.Groups[1].Value,
                    out double lat,
                    out double lon
                )
            )
            {
                points.Add(
                    GeoToUnity(
                        lat,
                        lon
                    )
                );
            }
        }

        return points;
    }


    // =========================================================
    // GEOMETRY HELPERS
    // =========================================================

    private void AddCylinder(
        Vector3 basePosition,
        Vector3 poleDirection,
        float lengthMeters,
        float radiusMeters,
        int radialSegments,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        poleDirection =
            poleDirection.normalized;

        Vector3 reference =
            Mathf.Abs(
                Vector3.Dot(
                    poleDirection,
                    Vector3.up
                )
            ) > 0.9f
                ? Vector3.right
                : Vector3.up;

        Vector3 right =
            Vector3.Cross(
                poleDirection,
                reference
            ).normalized;

        Vector3 forward =
            Vector3.Cross(
                right,
                poleDirection
            ).normalized;

        int start =
            vertices.Count;

        float radius =
            radiusMeters *
            worldScale;

        float length =
            lengthMeters *
            worldScale;

        for (
            int i = 0;
            i < radialSegments;
            i++
        )
        {
            float angle =
                i /
                (float)radialSegments *
                Mathf.PI *
                2f;

            Vector3 ring =
                (
                    right *
                    Mathf.Cos(angle) +
                    forward *
                    Mathf.Sin(angle)
                )
                *
                radius;

            vertices.Add(
                basePosition +
                ring
            );
        }

        for (
            int i = 0;
            i < radialSegments;
            i++
        )
        {
            float angle =
                i /
                (float)radialSegments *
                Mathf.PI *
                2f;

            Vector3 ring =
                (
                    right *
                    Mathf.Cos(angle) +
                    forward *
                    Mathf.Sin(angle)
                )
                *
                radius;

            vertices.Add(
                basePosition +
                poleDirection *
                length +
                ring
            );
        }

        for (
            int i = 0;
            i < radialSegments;
            i++
        )
        {
            int next =
                (
                    i + 1
                ) %
                radialSegments;

            int bottom =
                start + i;

            int bottomNext =
                start + next;

            int top =
                start +
                radialSegments +
                i;

            int topNext =
                start +
                radialSegments +
                next;

            triangles.Add(bottom);
            triangles.Add(top);
            triangles.Add(topNext);

            triangles.Add(bottom);
            triangles.Add(topNext);
            triangles.Add(bottomNext);
        }
    }


    private void AddCylinderBetween(
        Vector3 a,
        Vector3 b,
        float radiusMeters,
        int radialSegments,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        float lengthMeters =
            Vector3.Distance(
                a,
                b
            ) /
            Mathf.Max(
                worldScale,
                0.000001f
            );

        if (lengthMeters <= 0.001f)
        {
            return;
        }

        AddCylinder(
            a,
            (
                b - a
            ).normalized,
            lengthMeters,
            radiusMeters,
            radialSegments,
            vertices,
            triangles
        );
    }


    private void AddCone(
        Vector3 basePosition,
        float heightMeters,
        float radiusMeters,
        int segments,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        int start =
            vertices.Count;

        float radius =
            radiusMeters *
            worldScale;

        float height =
            heightMeters *
            worldScale;

        for (
            int i = 0;
            i < segments;
            i++
        )
        {
            float angle =
                i /
                (float)segments *
                Mathf.PI *
                2f;

            vertices.Add(
                basePosition +
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                )
            );
        }

        int tip =
            vertices.Count;

        vertices.Add(
            basePosition +
            Vector3.up *
            height
        );

        for (
            int i = 0;
            i < segments;
            i++
        )
        {
            int next =
                (
                    i + 1
                ) %
                segments;

            triangles.Add(
                start + i
            );

            triangles.Add(
                tip
            );

            triangles.Add(
                start + next
            );
        }

        for (
            int i = 1;
            i < segments - 1;
            i++
        )
        {
            triangles.Add(start);
            triangles.Add(start + i + 1);
            triangles.Add(start + i);
        }
    }


    private void AddSphere(
        Vector3 center,
        float radiusMeters,
        int rings,
        int segments,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        float radius =
            radiusMeters *
            worldScale;

        int start =
            vertices.Count;

        for (
            int y = 0;
            y <= rings;
            y++
        )
        {
            float v =
                y /
                (float)rings;

            float phi =
                v *
                Mathf.PI;

            float sinPhi =
                Mathf.Sin(phi);

            float cosPhi =
                Mathf.Cos(phi);

            for (
                int x = 0;
                x < segments;
                x++
            )
            {
                float u =
                    x /
                    (float)segments;

                float theta =
                    u *
                    Mathf.PI *
                    2f;

                Vector3 p =
                    new Vector3(
                        Mathf.Cos(theta) * sinPhi,
                        cosPhi,
                        Mathf.Sin(theta) * sinPhi
                    )
                    *
                    radius;

                vertices.Add(
                    center + p
                );
            }
        }

        for (
            int y = 0;
            y < rings;
            y++
        )
        {
            for (
                int x = 0;
                x < segments;
                x++
            )
            {
                int next =
                    (x + 1) %
                    segments;

                int a =
                    start +
                    y * segments +
                    x;

                int b =
                    start +
                    y * segments +
                    next;

                int c =
                    start +
                    (
                        y + 1
                    ) *
                    segments +
                    x;

                int d =
                    start +
                    (
                        y + 1
                    ) *
                    segments +
                    next;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(d);

                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(b);
            }
        }
    }


    private void CreateMeshObject(
        string objectName,
        List<Vector3> vertices,
        List<int> triangles,
        Material material
    )
    {
        if (
            vertices.Count == 0 ||
            triangles.Count == 0
        )
        {
            return;
        }

        GameObject obj =
            new GameObject(
                objectName
            );

        obj.transform.SetParent(
            transform,
            false
        );

        MeshFilter filter =
            obj.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            obj.AddComponent<MeshRenderer>();

        Mesh mesh =
            new Mesh();

        mesh.name =
            objectName +
            "_Mesh";

        if (
            vertices.Count >
            65535
        )
        {
            mesh.indexFormat =
                IndexFormat.UInt32;
        }

        mesh.SetVertices(
            vertices
        );

        mesh.SetTriangles(
            triangles,
            0
        );

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        filter.sharedMesh =
            mesh;

        if (material != null)
        {
            renderer.sharedMaterial =
                material;
        }
    }


    // =========================================================
    // CLEAR
    // =========================================================

    public void ClearGenerated()
    {
        string[] names =
        {
            "RoadsideTrees_Trunks",
            "RoadsideTrees_Canopies",
            "RoadsideInfrastructure",
            "RoadsideFences",
            "BridgeRails",
        };

        foreach (string name in names)
        {
            Transform child =
                transform.Find(name);

            if (child != null)
            {
                DestroyGenerated(
                    child.gameObject
                );
            }
        }
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
            DestroyImmediate(obj);
            return;
        }
#endif

        Destroy(obj);
    }


    [ContextMenu("Generate Roadside Details")]
    private void GenerateFromEditor()
    {
        GenerateDetails();
    }


    [ContextMenu("Clear Roadside Details")]
    private void ClearFromEditor()
    {
        ClearGenerated();
    }
}
