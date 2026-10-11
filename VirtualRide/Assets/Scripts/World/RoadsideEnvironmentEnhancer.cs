using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

public class RoadsideEnvironmentEnhancer : MonoBehaviour
{
    [Header("Processed Chunk")]
    public TextAsset processedChunk;

    [Header("Route")]
    public TextAsset routeJson;

    [Header("Shared Coordinate Origin")]
    public double originLatitude = 9.9642475;
    public double originLongitude = 76.2454182;

    [Header("World Scale")]
    public float worldScale = 0.01f;
    public float groundHeight = -0.02f;

    [Header("Chunk Route Range")]
    public float chunkStartKm = 0f;
    public float chunkEndKm = 0f;

    [Header("Visible Corridor")]
    public float detailRadiusMeters = 35f;

    [Header("Bushes / Shrubs")]
    public bool generateBushes = true;
    public float bushSpacingMeters = 8f;
    public float bushMinOffsetMeters = 4.5f;
    public float bushMaxOffsetMeters = 11f;
    public float bushMinHeightMeters = 0.45f;
    public float bushMaxHeightMeters = 1.25f;
    public float bushMinRadiusMeters = 0.45f;
    public float bushMaxRadiusMeters = 1.20f;
    public int bushClustersPerSide = 1;

    [Header("Roadside Ground")]
    public bool generateGroundProfile = true;
    public float groundInnerOffsetMeters = 3.2f;
    public float groundOuterOffsetMeters = 11f;
    public float groundProfileStepMeters = 6f;
    public float groundVariationMeters = 0.08f;

    [Header("Building Detail")]
    public bool generateBuildingDetails = true;
    [Tooltip("Must match OSMWorldGenerator roadsideBuildingSetbackMeters.")]
    public float buildingSetbackMeters = 2.0f;
    [Tooltip("Must match OSMWorldGenerator roadsideBuildingInfluenceMeters.")]
    public float buildingInfluenceMeters = 35f;
    public float buildingDetailRadiusMeters = 28f;
    public float balconyLevelOneMeters = 3.2f;
    public float balconyLevelTwoMeters = 6.0f;
    public float balconyWidthMeters = 2.2f;
    public float balconyDepthMeters = 1.0f;
    public float balconySlabThicknessMeters = 0.12f;
    public float balconyRailHeightMeters = 1.0f;
    public float balconyRailRadiusMeters = 0.035f;
    public float rooftopTankHeightMeters = 0.9f;
    public float rooftopTankRadiusMeters = 0.6f;
    public float rooftopTankClearanceMeters = 0.25f;

    [Header("Extra Building Detail")]
    public float awningHeightMeters = 2.4f;
    public float awningWidthMeters = 2.0f;
    public float awningDepthMeters = 0.7f;
    public float acUnitSizeMeters = 0.55f;

    [Header("Materials")]
    public Material vegetationMaterial;
    public Material groundMaterial;
    public Material buildingDetailMaterial;
    public Material railMaterial;


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


    private sealed class BuildingRecord
    {
        public string type;
        public List<Vector3> points =
            new List<Vector3>();
    }


    private readonly List<Vector3> routePoints =
        new List<Vector3>();


    private readonly List<float> routeCumulativeMeters =
        new List<float>();


    private readonly List<Vector3> bushVertices =
        new List<Vector3>();


    private readonly List<int> bushTriangles =
        new List<int>();


    private readonly List<Vector3> groundVertices =
        new List<Vector3>();


    private readonly List<int> groundTriangles =
        new List<int>();


    private readonly List<Vector3> buildingVertices =
        new List<Vector3>();


    private readonly List<int> buildingTriangles =
        new List<int>();


    private readonly List<Vector3> railVertices =
        new List<Vector3>();


    private readonly List<int> railTriangles =
        new List<int>();


    // =========================================================
    // MAIN
    // =========================================================

    public void Generate()
    {
        ClearGenerated();


        if (processedChunk == null)
        {
            Debug.LogError(
                "RoadsideEnvironmentEnhancer: processedChunk is not assigned."
            );

            return;
        }


        if (routeJson == null)
        {
            Debug.LogError(
                "RoadsideEnvironmentEnhancer: routeJson is not assigned."
            );

            return;
        }


        if (!LoadRoute())
        {
            Debug.LogError(
                "RoadsideEnvironmentEnhancer: route could not be loaded."
            );

            return;
        }


        if (routePoints.Count < 2)
        {
            Debug.LogError(
                "RoadsideEnvironmentEnhancer: route has fewer than 2 points."
            );

            return;
        }


        if (generateGroundProfile)
        {
            GenerateGroundPatches();
        }


        if (generateBushes)
        {
            GenerateBushClusters();
        }


        if (generateBuildingDetails)
        {
            GenerateNearbyBuildingDetails();
        }


        CreateMeshObject(
            "RoadsideBushes",
            bushVertices,
            bushTriangles,
            vegetationMaterial
        );


        CreateMeshObject(
            "RoadsideGroundProfile",
            groundVertices,
            groundTriangles,
            groundMaterial
        );


        CreateMeshObject(
            "NearbyBuildingDetails",
            buildingVertices,
            buildingTriangles,
            buildingDetailMaterial
        );


        CreateMeshObject(
            "NearbyBuildingRails",
            railVertices,
            railTriangles,
            railMaterial
        );


        Debug.Log(
            "RoadsideEnvironmentEnhancer: improved roadside environment generated."
        );
    }


    // =========================================================
    // ROUTE LOADING
    // =========================================================

    private bool LoadRoute()
    {
        routePoints.Clear();
        routeCumulativeMeters.Clear();


        RouteData data = null;


        try
        {
            data =
                JsonUtility.FromJson<RouteData>(
                    routeJson.text
                );
        }
        catch
        {
            data = null;
        }


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


        if (routePoints.Count < 2)
        {
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


        if (routePoints.Count < 2)
        {
            return false;
        }


        routeCumulativeMeters.Add(
            0f
        );


        float total =
            0f;


        for (
            int i = 0;
            i < routePoints.Count - 1;
            i++
        )
        {
            float segment =
                Vector3.Distance(
                    routePoints[i],
                    routePoints[i + 1]
                )
                /
                Mathf.Max(
                    0.000001f,
                    worldScale
                );


            total +=
                segment;


            routeCumulativeMeters.Add(
                total
            );
        }


        return true;
    }


    // =========================================================
    // BUSHES
    // =========================================================

    private void GenerateBushClusters()
    {
        float total =
            RouteLengthMeters();


        float start =
            Mathf.Max(
                0f,
                chunkStartKm * 1000f -
                detailRadiusMeters
            );


        float end =
            chunkEndKm > chunkStartKm
                ? Mathf.Min(
                    total,
                    chunkEndKm * 1000f +
                    detailRadiusMeters
                )
                : total;


        float spacing =
            Mathf.Max(
                4f,
                bushSpacingMeters
            );


        int sample =
            0;


        for (
            float distance = start;
            distance <= end;
            distance += spacing
        )
        {
            Vector3 roadPoint =
                RoutePositionAtDistance(
                    distance
                );


            Vector3 side =
                RouteRightAtDistance(
                    distance
                );


            for (
                int cluster = 0;
                cluster < Mathf.Max(
                    1,
                    bushClustersPerSide
                );
                cluster++
            )
            {
                System.Random random =
                    new System.Random(
                        sample * 7919 +
                        cluster * 104729 +
                        Mathf.RoundToInt(
                            chunkStartKm * 100f
                        )
                    );


                float sideSign =
                    (
                        (
                            sample +
                            cluster
                        ) %
                        2 ==
                        0
                    )
                        ? -1f
                        : 1f;


                float offset =
                    Mathf.Lerp(
                        bushMinOffsetMeters,
                        bushMaxOffsetMeters,
                        (float)random.NextDouble()
                    );


                Vector3 position =
                    roadPoint +
                    side *
                    (
                        sideSign *
                        offset *
                        worldScale
                    );

                // Route samples are on Y = 0. Put the bush base on the
                // same ground plane as the OSM world.
                position.y = groundHeight;

                // Route coordinates are generated at Y = 0.
                // Place the bush base on the shared world ground.
                position.y = groundHeight;


                if (
                    DistanceToRouteMeters(
                        position
                    )
                    < groundInnerOffsetMeters
                )
                {
                    continue;
                }


                float radius =
                    Mathf.Lerp(
                        bushMinRadiusMeters,
                        bushMaxRadiusMeters,
                        (float)random.NextDouble()
                    );


                float height =
                    Mathf.Lerp(
                        bushMinHeightMeters,
                        bushMaxHeightMeters,
                        (float)random.NextDouble()
                    );


                AddBushCluster(
                    position,
                    radius,
                    height,
                    random
                );
            }


            sample++;
        }
    }


    private void AddBushCluster(
        Vector3 center,
        float radiusMeters,
        float heightMeters,
        System.Random random
    )
    {
        // 4 irregular lobes instead of a single cone.
        int lobes =
            3 +
            random.Next(
                0,
                2
            );


        for (
            int i = 0;
            i < lobes;
            i++
        )
        {
            float angle =
                (
                    i /
                    (float)lobes *
                    Mathf.PI *
                    2f
                )
                +
                Mathf.Lerp(
                    -0.35f,
                    0.35f,
                    (float)random.NextDouble()
                );


            float distance =
                radiusMeters *
                0.35f *
                (float)random.NextDouble();


            Vector3 offset =
                new Vector3(
                    Mathf.Cos(angle) *
                    distance *
                    worldScale,
                    0f,
                    Mathf.Sin(angle) *
                    distance *
                    worldScale
                );


            float lobeRadius =
                radiusMeters *
                Mathf.Lerp(
                    0.55f,
                    0.90f,
                    (float)random.NextDouble()
                );


            float lobeHeight =
                heightMeters *
                Mathf.Lerp(
                    0.70f,
                    1.15f,
                    (float)random.NextDouble()
                );


            AddBushBlob(
                center +
                offset,
                lobeRadius,
                lobeHeight
            );
        }


        // Low central base fills the silhouette.
        AddBushBlob(
            center,
            radiusMeters *
            0.75f,
            heightMeters *
            0.72f
        );
    }


    private void AddBushBlob(
        Vector3 center,
        float radiusMeters,
        float heightMeters
    )
    {
        center.y = groundHeight;
        int rings =
            3;

        int segments =
            7;

        int start =
            bushVertices.Count;


        float radius =
            radiusMeters *
            worldScale;


        float height =
            heightMeters *
            worldScale;


        // Bottom ring, middle ring, top point.
        for (
            int ring = 0;
            ring < rings;
            ring++
        )
        {
            float t =
                ring /
                (float)(rings - 1);


            float y =
                Mathf.Lerp(
                    0f,
                    height * 0.78f,
                    t
                );


            float horizontalScale =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    0.5f
                );


            horizontalScale =
                Mathf.Max(
                    0.20f,
                    horizontalScale
                );


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


                verticesSafeBushAdd(
                    center +
                    new Vector3(
                        Mathf.Cos(angle) *
                        radius *
                        horizontalScale,
                        y,
                        Mathf.Sin(angle) *
                        radius *
                        horizontalScale
                    )
                );
            }
        }


        int top =
            bushVertices.Count;


        bushVertices.Add(
            center +
            Vector3.up *
            height
        );


        for (
            int ring = 0;
            ring < rings - 1;
            ring++
        )
        {
            int ringStart =
                start +
                ring *
                segments;


            int nextRingStart =
                start +
                (ring + 1) *
                segments;


            for (
                int i = 0;
                i < segments;
                i++
            )
            {
                int next =
                    (i + 1) %
                    segments;


                bushTriangles.Add(
                    ringStart + i
                );

                bushTriangles.Add(
                    nextRingStart + i
                );

                bushTriangles.Add(
                    nextRingStart + next
                );


                bushTriangles.Add(
                    ringStart + i
                );

                bushTriangles.Add(
                    nextRingStart + next
                );

                bushTriangles.Add(
                    ringStart + next
                );
            }
        }


        int finalRingStart =
            start +
            (
                rings - 1
            ) *
            segments;


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


            bushTriangles.Add(
                finalRingStart + i
            );

            bushTriangles.Add(
                top
            );

            bushTriangles.Add(
                finalRingStart + next
            );
        }
    }


    private void verticesSafeBushAdd(
        Vector3 value
    )
    {
        bushVertices.Add(value);
    }


    // =========================================================
    // GROUND PATCHES
    // =========================================================

    private void GenerateGroundPatches()
    {
        float total =
            RouteLengthMeters();


        float start =
            Mathf.Max(
                0f,
                chunkStartKm * 1000f -
                detailRadiusMeters
            );


        float end =
            chunkEndKm > chunkStartKm
                ? Mathf.Min(
                    total,
                    chunkEndKm * 1000f +
                    detailRadiusMeters
                )
                : total;


        float spacing =
            Mathf.Max(
                3f,
                groundProfileStepMeters
            );


        int sample =
            0;


        for (
            float distance = start;
            distance <= end;
            distance += spacing
        )
        {
            Vector3 road =
                RoutePositionAtDistance(
                    distance
                );


            Vector3 right =
                RouteRightAtDistance(
                    distance
                );


            System.Random random =
                new System.Random(
                    1459 +
                    sample * 6151
                );


            for (
                int side = -1;
                side <= 1;
                side += 2
            )
            {
                float inner =
                    Mathf.Lerp(
                        groundInnerOffsetMeters,
                        groundOuterOffsetMeters,
                        0.20f +
                        0.20f *
                        (float)random.NextDouble()
                    );


                float outer =
                    Mathf.Lerp(
                        groundInnerOffsetMeters + 1f,
                        groundOuterOffsetMeters,
                        0.55f +
                        0.30f *
                        (float)random.NextDouble()
                    );


                float halfLength =
                    spacing *
                    Mathf.Lerp(
                        0.35f,
                        0.55f,
                        (float)random.NextDouble()
                    );


                float variation =
                    Mathf.Lerp(
                        -groundVariationMeters,
                        groundVariationMeters,
                        (float)random.NextDouble()
                    );


                Vector3 a =
                    road +
                    right *
                    (
                        side *
                        inner *
                        worldScale
                    )
                    -
                    RouteTangentAtDistance(
                        distance
                    )
                    *
                    (
                        halfLength *
                        worldScale
                    );


                Vector3 b =
                    road +
                    right *
                    (
                        side *
                        outer *
                        worldScale
                    )
                    -
                    RouteTangentAtDistance(
                        distance
                    )
                    *
                    (
                        halfLength *
                        0.85f *
                        worldScale
                    );


                Vector3 c =
                    road +
                    right *
                    (
                        side *
                        outer *
                        worldScale
                    )
                    +
                    RouteTangentAtDistance(
                        distance
                    )
                    *
                    (
                        halfLength *
                        0.85f *
                        worldScale
                    );


                Vector3 d =
                    road +
                    right *
                    (
                        side *
                        inner *
                        worldScale
                    )
                    +
                    RouteTangentAtDistance(
                        distance
                    )
                    *
                    (
                        halfLength *
                        worldScale
                    );


                a.y =
                    groundHeight +
                    (
                        0.008f +
                        variation *
                        0.35f
                    )
                    *
                    worldScale;


                b.y =
                    groundHeight +
                    (
                        0.012f +
                        variation
                    )
                    *
                    worldScale;


                c.y =
                    groundHeight +
                    (
                        0.012f +
                        variation *
                        0.70f
                    )
                    *
                    worldScale;


                d.y =
                    groundHeight +
                    (
                        0.008f +
                        variation *
                        0.35f
                    )
                    *
                    worldScale;


                AddGroundPatch(
                    a,
                    b,
                    c,
                    d
                );
            }


            sample++;
        }
    }


    private void AddGroundPatch(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d
    )
    {
        int start =
            groundVertices.Count;


        groundVertices.Add(a);
        groundVertices.Add(b);
        groundVertices.Add(c);
        groundVertices.Add(d);


        groundTriangles.Add(
            start
        );

        groundTriangles.Add(
            start + 1
        );

        groundTriangles.Add(
            start + 2
        );


        groundTriangles.Add(
            start
        );

        groundTriangles.Add(
            start + 2
        );

        groundTriangles.Add(
            start + 3
        );
    }


    // =========================================================
    // BUILDING DETAILS
    // =========================================================

    private void GenerateNearbyBuildingDetails()
    {
        List<BuildingRecord> buildings =
            ParseBuildings();


        int enhanced =
            0;


        foreach (
            BuildingRecord building
            in buildings
        )
        {
            if (
                building.points.Count < 3
            )
            {
                continue;
            }

            // IMPORTANT: the main OSMWorldGenerator shifts nearby
            // building shells away from the cycling road. Apply the
            // exact same footprint shift here before generating
            // balconies, awnings, AC units and rooftop tanks so all
            // architectural details remain attached to the shell.
            ApplyRoadsideBuildingSetback(
                building.points
            );

            Vector3 center =
                CalculateCentroid(
                    building.points
                );


            if (
                DistanceToRouteMeters(
                    center
                )
                > buildingDetailRadiusMeters
            )
            {
                continue;
            }


            if (
                !FindRouteFacingEdge(
                    building.points,
                    out Vector3 edgeMid,
                    out Vector3 edgeDirection,
                    out Vector3 routeFacing
                )
            )
            {
                continue;
            }


            float width =
                BuildingWidthMeters(
                    building.points
                );


            float height =
                EstimateBuildingHeight(
                    building.type
                );


            // Most visible buildings get one balcony/awning.
            if (width >= 5f)
            {
                AddBalcony(
                    edgeMid,
                    edgeDirection,
                    routeFacing,
                    Mathf.Min(
                        balconyLevelOneMeters,
                        height *
                        0.55f
                    )
                );


                AddAwning(
                    edgeMid,
                    edgeDirection,
                    routeFacing,
                    Mathf.Min(
                        awningHeightMeters,
                        height *
                        0.45f
                    )
                );
            }


            // Taller/larger buildings get a second balcony sometimes.
            if (
                width >= 7f &&
                height >= 8f
            )
            {
                AddBalcony(
                    edgeMid +
                    edgeDirection *
                    0.3f *
                    worldScale,
                    edgeDirection,
                    routeFacing,
                    Mathf.Min(
                        balconyLevelTwoMeters,
                        height *
                        0.72f
                    )
                );
            }


            // One small AC unit on some nearby buildings.
            if (
                width >= 6f &&
                enhanced % 2 == 0
            )
            {
                AddACUnit(
                    edgeMid,
                    edgeDirection,
                    routeFacing,
                    Mathf.Min(
                        4.0f,
                        height *
                        0.55f
                    )
                );
            }


            // Rooftop tank on larger buildings, not every building.
            if (
                width >= 8f &&
                enhanced % 3 == 0
            )
            {
                AddRooftopTank(
                    center,
                    height
                );
            }


            enhanced++;
        }


        Debug.Log(
            $"RoadsideEnvironmentEnhancer: nearby buildings enhanced = {enhanced}"
        );
    }


    private List<BuildingRecord> ParseBuildings()
    {
        List<BuildingRecord> result =
            new List<BuildingRecord>();


        string array =
            ExtractArrayContent(
                processedChunk.text,
                "buildings"
            );


        if (
            string.IsNullOrEmpty(
                array
            )
        )
        {
            return result;
        }


        List<string> objects =
            ExtractTopLevelObjects(
                array
            );


        foreach (
            string objectText
            in objects
        )
        {
            string points =
                ExtractArrayContent(
                    objectText,
                    "points"
                );


            if (
                string.IsNullOrEmpty(
                    points
                )
            )
            {
                continue;
            }


            List<Vector3> parsedPoints =
                ParsePointArray(
                    points
                );


            if (
                parsedPoints.Count <
                3
            )
            {
                continue;
            }


            Match typeMatch =
                Regex.Match(
                    objectText,
                    @"""type""\s*:\s*""([^""]*)""",
                    RegexOptions.IgnoreCase
                );


            result.Add(
                new BuildingRecord
                {
                    type =
                        typeMatch.Success
                            ? typeMatch.Groups[1].Value
                            : "yes",

                    points =
                        parsedPoints
                }
            );
        }


        return result;
    }


    private List<Vector3> ParsePointArray(
        string text
    )
    {
        List<Vector3> result =
            new List<Vector3>();


        List<string> objects =
            ExtractTopLevelObjects(
                text
            );


        foreach (
            string objectText
            in objects
        )
        {
            if (
                TryGetCoordinate(
                    objectText,
                    out double lat,
                    out double lon
                )
            )
            {
                Vector3 point =
                    GeoToUnity(
                        lat,
                        lon
                    );

                point.y =
                    groundHeight;

                result.Add(
                    point
                );
            }
        }


        return result;
    }


    private string ExtractArrayContent(
        string json,
        string key
    )
    {
        if (
            string.IsNullOrEmpty(
                json
            )
        )
        {
            return null;
        }


        Match keyMatch =
            Regex.Match(
                json,
                @"""" +
                Regex.Escape(
                    key
                ) +
                @"""\s*:",
                RegexOptions.IgnoreCase
            );


        if (!keyMatch.Success)
        {
            return null;
        }


        int arrayStart =
            json.IndexOf(
                '[',
                keyMatch.Index +
                keyMatch.Length
            );


        if (
            arrayStart < 0
        )
        {
            return null;
        }


        int arrayEnd =
            FindMatchingDelimiter(
                json,
                arrayStart,
                '[',
                ']'
            );


        if (
            arrayEnd < 0
        )
        {
            return null;
        }


        return json.Substring(
            arrayStart + 1,
            arrayEnd -
            arrayStart -
            1
        );
    }


    private List<string> ExtractTopLevelObjects(
        string text
    )
    {
        List<string> result =
            new List<string>();


        int depth =
            0;

        int start =
            -1;

        bool inString =
            false;

        bool escaped =
            false;


        for (
            int i = 0;
            i < text.Length;
            i++
        )
        {
            char c =
                text[i];


            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }


            if (c == '"')
            {
                inString = true;
                continue;
            }


            if (c == '{')
            {
                if (depth == 0)
                {
                    start = i;
                }

                depth++;
            }
            else if (c == '}')
            {
                if (depth > 0)
                {
                    depth--;

                    if (
                        depth == 0 &&
                        start >= 0
                    )
                    {
                        result.Add(
                            text.Substring(
                                start,
                                i -
                                start +
                                1
                            )
                        );

                        start = -1;
                    }
                }
            }
        }


        return result;
    }


    private int FindMatchingDelimiter(
        string text,
        int start,
        char open,
        char close
    )
    {
        int depth =
            0;

        bool inString =
            false;

        bool escaped =
            false;


        for (
            int i = start;
            i < text.Length;
            i++
        )
        {
            char c =
                text[i];


            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }


            if (c == '"')
            {
                inString = true;
                continue;
            }


            if (c == open)
            {
                depth++;
            }
            else if (c == close)
            {
                depth--;

                if (depth == 0)
                {
                    return i;
                }
            }
        }


        return -1;
    }


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


        return
            double.TryParse(
                lat.Groups[1].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out latitude
            )
            &&
            double.TryParse(
                lon.Groups[1].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out longitude
            );
    }


    private Vector3 CalculateCentroid(
        List<Vector3> points
    )
    {
        Vector3 sum =
            Vector3.zero;


        foreach (
            Vector3 point
            in points
        )
        {
            sum +=
                point;
        }


        return
            sum /
            Mathf.Max(
                1,
                points.Count
            );
    }


    private float BuildingWidthMeters(
        List<Vector3> points
    )
    {
        float max =
            0f;


        for (
            int i = 0;
            i < points.Count;
            i++
        )
        {
            Vector3 a =
                points[i];

            Vector3 b =
                points[
                    (
                        i + 1
                    )
                    %
                    points.Count
                ];


            float length =
                Vector3.Distance(
                    a,
                    b
                )
                /
                Mathf.Max(
                    worldScale,
                    0.000001f
                );


            max =
                Mathf.Max(
                    max,
                    length
                );
        }


        return max;
    }


    private bool FindRouteFacingEdge(
        List<Vector3> points,
        out Vector3 edgeMid,
        out Vector3 edgeDirection,
        out Vector3 routeFacing
    )
    {
        edgeMid =
            Vector3.zero;

        edgeDirection =
            Vector3.forward;

        routeFacing =
            Vector3.forward;


        Vector3 routePoint =
            ClosestPointOnRoute(
                CalculateCentroid(
                    points
                )
            );


        float best =
            float.MaxValue;


        for (
            int i = 0;
            i < points.Count;
            i++
        )
        {
            Vector3 a =
                points[i];

            Vector3 b =
                points[
                    (
                        i + 1
                    )
                    %
                    points.Count
                ];


            Vector3 edge =
                b - a;


            edge.y =
                0f;


            if (
                edge.sqrMagnitude <
                0.000001f
            )
            {
                continue;
            }


            Vector3 midpoint =
                (
                    a +
                    b
                )
                *
                0.5f;


            float distance =
                Vector3.Distance(
                    midpoint,
                    routePoint
                );


            if (
                distance <
                best
            )
            {
                best =
                    distance;

                edgeMid =
                    midpoint;

                edgeDirection =
                    edge.normalized;
            }
        }


        routeFacing =
            routePoint -
            edgeMid;


        routeFacing.y =
            0f;


        if (
            routeFacing.sqrMagnitude <
            0.000001f
        )
        {
            return false;
        }


        routeFacing.Normalize();


        return true;
    }


    private float EstimateBuildingHeight(
        string type
    )
    {
        string t =
            string.IsNullOrEmpty(type)
                ? "yes"
                : type.ToLowerInvariant();


        if (
            t == "garage" ||
            t == "shed" ||
            t == "hut"
        )
        {
            return 4f;
        }


        if (
            t == "church" ||
            t == "cathedral"
        )
        {
            return 15f;
        }


        if (
            t == "industrial" ||
            t == "warehouse" ||
            t == "commercial"
        )
        {
            return 10f;
        }


        return 8f;
    }


    // =========================================================
    // BUILDING GEOMETRY
    // =========================================================

    private void AddBalcony(
        Vector3 edgeMid,
        Vector3 edgeDirection,
        Vector3 routeFacing,
        float levelMeters
    )
    {
        Vector3 right =
            edgeDirection.normalized;


        Vector3 outward =
            routeFacing.normalized;


        Vector3 center =
            edgeMid +
            outward *
            (
                balconyDepthMeters *
                0.45f *
                worldScale
            )
            +
            Vector3.up *
            (
                levelMeters *
                worldScale
            );


        AddBox(
            center,
            right,
            outward,
            Vector3.up,
            new Vector3(
                balconyWidthMeters,
                balconySlabThicknessMeters,
                balconyDepthMeters
            ),
            buildingVertices,
            buildingTriangles
        );


        float halfWidth =
            balconyWidthMeters *
            0.5f;


        Vector3 railFront =
            center +
            outward *
            (
                balconyDepthMeters *
                0.5f *
                worldScale
            )
            +
            Vector3.up *
            (
                balconyRailHeightMeters *
                0.5f *
                worldScale
            );


        Vector3 left =
            railFront -
            right *
            halfWidth *
            worldScale;


        Vector3 rightPoint =
            railFront +
            right *
            halfWidth *
            worldScale;


        AddCylinderBetween(
            left,
            left +
            Vector3.down *
            (
                balconyRailHeightMeters *
                0.5f *
                worldScale
            ),
            balconyRailRadiusMeters,
            5,
            railVertices,
            railTriangles
        );


        AddCylinderBetween(
            railFront,
            railFront +
            Vector3.down *
            (
                balconyRailHeightMeters *
                0.5f *
                worldScale
            ),
            balconyRailRadiusMeters,
            5,
            railVertices,
            railTriangles
        );


        AddCylinderBetween(
            rightPoint,
            rightPoint +
            Vector3.down *
            (
                balconyRailHeightMeters *
                0.5f *
                worldScale
            ),
            balconyRailRadiusMeters,
            5,
            railVertices,
            railTriangles
        );


        AddCylinderBetween(
            left,
            rightPoint,
            balconyRailRadiusMeters,
            5,
            railVertices,
            railTriangles
        );


        // Lower support brackets give the balcony depth.
        Vector3 supportLeft =
            center -
            right *
            halfWidth *
            0.65f *
            worldScale;


        Vector3 supportRight =
            center +
            right *
            halfWidth *
            0.65f *
            worldScale;


        AddCylinderBetween(
            supportLeft,
            supportLeft -
            outward *
            balconyDepthMeters *
            0.45f *
            worldScale,
            0.04f,
            5,
            railVertices,
            railTriangles
        );


        AddCylinderBetween(
            supportRight,
            supportRight -
            outward *
            balconyDepthMeters *
            0.45f *
            worldScale,
            0.04f,
            5,
            railVertices,
            railTriangles
        );
    }


    private void AddAwning(
        Vector3 edgeMid,
        Vector3 edgeDirection,
        Vector3 routeFacing,
        float levelMeters
    )
    {
        Vector3 right =
            edgeDirection.normalized;

        Vector3 outward =
            routeFacing.normalized;


        Vector3 center =
            edgeMid +
            outward *
            (
                awningDepthMeters *
                0.45f *
                worldScale
            )
            +
            Vector3.up *
            (
                levelMeters *
                worldScale
            );


        AddBox(
            center,
            right,
            outward,
            Vector3.up,
            new Vector3(
                awningWidthMeters,
                0.08f,
                awningDepthMeters
            ),
            buildingVertices,
            buildingTriangles
        );
    }


    private void AddACUnit(
        Vector3 edgeMid,
        Vector3 edgeDirection,
        Vector3 routeFacing,
        float levelMeters
    )
    {
        Vector3 right =
            edgeDirection.normalized;


        Vector3 outward =
            routeFacing.normalized;


        Vector3 center =
            edgeMid -
            outward *
            (
                0.12f *
                worldScale
            )
            +
            right *
            (
                0.25f *
                worldScale
            )
            +
            Vector3.up *
            (
                levelMeters *
                worldScale
            );


        AddBox(
            center,
            right,
            outward,
            Vector3.up,
            new Vector3(
                acUnitSizeMeters,
                acUnitSizeMeters *
                0.70f,
                0.28f
            ),
            buildingVertices,
            buildingTriangles
        );
    }


    private void AddRooftopTank(
        Vector3 center,
        float buildingHeightMeters
    )
    {
        Vector3 tankBase =
            center +
            Vector3.up *
            (
                (
                    buildingHeightMeters +
                    rooftopTankClearanceMeters
                )
                *
                worldScale
            );


        AddCylinder(
            tankBase,
            Vector3.up,
            rooftopTankHeightMeters,
            rooftopTankRadiusMeters,
            8,
            buildingVertices,
            buildingTriangles
        );


        // Tiny support legs.
        float legOffset =
            rooftopTankRadiusMeters *
            0.55f;


        Vector3[] offsets =
        {
            new Vector3(
                legOffset,
                0f,
                legOffset
            ),

            new Vector3(
                -legOffset,
                0f,
                legOffset
            ),

            new Vector3(
                legOffset,
                0f,
                -legOffset
            ),

            new Vector3(
                -legOffset,
                0f,
                -legOffset
            ),
        };


        foreach (
            Vector3 offset
            in offsets
        )
        {
            Vector3 basePoint =
                tankBase +
                offset *
                worldScale;


            AddCylinder(
                basePoint,
                Vector3.up,
                rooftopTankClearanceMeters,
                0.05f,
                5,
                buildingVertices,
                buildingTriangles
            );
        }
    }


    // =========================================================
    // ROUTE MATH
    // =========================================================

    private float RouteLengthMeters()
    {
        if (
            routeCumulativeMeters.Count == 0
        )
        {
            return 0f;
        }


        return routeCumulativeMeters[
            routeCumulativeMeters.Count - 1
        ];
    }


    private Vector3 RoutePositionAtDistance(
        float meters
    )
    {
        if (
            routePoints.Count < 2
        )
        {
            return Vector3.zero;
        }


        float d =
            Mathf.Clamp(
                meters,
                0f,
                RouteLengthMeters()
            );


        for (
            int i = 0;
            i < routeCumulativeMeters.Count - 1;
            i++
        )
        {
            float a =
                routeCumulativeMeters[i];


            float b =
                routeCumulativeMeters[
                    i + 1
                ];


            if (
                d <= b
            )
            {
                float t =
                    b - a <= 0.001f
                        ? 0f
                        : (
                            d - a
                        )
                        /
                        (
                            b - a
                        );


                return Vector3.Lerp(
                    routePoints[i],
                    routePoints[
                        i + 1
                    ],
                    t
                );
            }
        }


        return routePoints[
            routePoints.Count - 1
        ];
    }


    private Vector3 RouteTangentAtDistance(
        float meters
    )
    {
        float total =
            RouteLengthMeters();


        float sample =
            Mathf.Clamp(
                meters,
                0f,
                total
            );


        float delta =
            Mathf.Min(
                1f,
                Mathf.Max(
                    0.25f,
                    total * 0.0001f
                )
            );


        Vector3 a =
            RoutePositionAtDistance(
                Mathf.Max(
                    0f,
                    sample - delta
                )
            );


        Vector3 b =
            RoutePositionAtDistance(
                Mathf.Min(
                    total,
                    sample + delta
                )
            );


        Vector3 direction =
            b - a;


        direction.y =
            0f;


        if (
            direction.sqrMagnitude <
            0.000001f
        )
        {
            return Vector3.forward;
        }


        return
            direction.normalized;
    }


    private Vector3 RouteRightAtDistance(
        float meters
    )
    {
        return
            Vector3.Cross(
                Vector3.up,
                RouteTangentAtDistance(
                    meters
                )
            )
            .normalized;
    }


    private Vector3 ClosestPointOnRoute(
        Vector3 position
    )
    {
        float best =
            float.MaxValue;


        Vector3 result =
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
                routePoints[
                    i + 1
                ];


            Vector3 segment =
                b - a;


            segment.y =
                0f;


            float lenSq =
                segment.sqrMagnitude;


            if (
                lenSq <
                0.000001f
            )
            {
                continue;
            }


            float t =
                Vector3.Dot(
                    position - a,
                    segment
                )
                /
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


            if (
                distance <
                best
            )
            {
                best =
                    distance;

                result =
                    closest;
            }
        }


        return result;
    }


    private float DistanceToRouteMeters(
        Vector3 position
    )
    {
        return
            Vector3.Distance(
                position,
                ClosestPointOnRoute(
                    position
                )
            )
            /
            Mathf.Max(
                worldScale,
                0.000001f
            );
    }


    // =========================================================
    // BUILDING / CYCLING ROUTE ALIGNMENT
    // =========================================================

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

        float nearestDistanceMeters = float.MaxValue;
        Vector3 nearestBuildingPoint = Vector3.zero;
        Vector3 nearestRoutePoint = Vector3.zero;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3 a = polygon[i];
            Vector3 b = polygon[(i + 1) % polygon.Count];

            for (int s = 0; s <= 4; s++)
            {
                float t = s / 4f;
                Vector3 candidate = Vector3.Lerp(a, b, t);
                Vector3 routePoint = ClosestPointOnRoute(candidate);

                float distanceMeters =
                    Vector3.Distance(candidate, routePoint) /
                    Mathf.Max(worldScale, 0.000001f);

                if (distanceMeters < nearestDistanceMeters)
                {
                    nearestDistanceMeters = distanceMeters;
                    nearestBuildingPoint = candidate;
                    nearestRoutePoint = routePoint;
                }
            }
        }

        if (
            nearestDistanceMeters == float.MaxValue ||
            nearestDistanceMeters > buildingInfluenceMeters
        )
        {
            return;
        }

        // Cycling road = 5 m wide, plus 0.75 m shoulder each side.
        // Add the configurable building setback to that clearance.
        float desiredEdgeDistance =
            2.5f +
            0.75f +
            Mathf.Max(0f, buildingSetbackMeters);

        float shiftMeters =
            desiredEdgeDistance - nearestDistanceMeters;

        if (shiftMeters <= 0f)
        {
            return;
        }

        Vector3 awayFromRoad =
            nearestBuildingPoint -
            nearestRoutePoint;

        awayFromRoad.y = 0f;

        if (awayFromRoad.sqrMagnitude < 0.000001f)
        {
            return;
        }

        awayFromRoad.Normalize();

        Vector3 shift =
            awayFromRoad *
            shiftMeters *
            worldScale;

        for (int i = 0; i < polygon.Count; i++)
        {
            polygon[i] += shift;
        }
    }


    // =========================================================
    // GEO CONVERSION
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
            )
            *
            metersPerDegreeLongitude;


        double zMeters =
            (
                latitude -
                originLatitude
            )
            *
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
    // GEOMETRY HELPERS
    // =========================================================

    private void AddBox(
        Vector3 center,
        Vector3 right,
        Vector3 forward,
        Vector3 up,
        Vector3 sizeMeters,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        right.Normalize();
        forward.Normalize();
        up.Normalize();


        Vector3 hx =
            right *
            sizeMeters.x *
            0.5f *
            worldScale;


        Vector3 hy =
            up *
            sizeMeters.y *
            0.5f *
            worldScale;


        Vector3 hz =
            forward *
            sizeMeters.z *
            0.5f *
            worldScale;


        int start =
            vertices.Count;


        vertices.Add(
            center -
            hx -
            hy -
            hz
        );

        vertices.Add(
            center +
            hx -
            hy -
            hz
        );

        vertices.Add(
            center +
            hx +
            hy -
            hz
        );

        vertices.Add(
            center -
            hx +
            hy -
            hz
        );


        vertices.Add(
            center -
            hx -
            hy +
            hz
        );

        vertices.Add(
            center +
            hx -
            hy +
            hz
        );

        vertices.Add(
            center +
            hx +
            hy +
            hz
        );

        vertices.Add(
            center -
            hx +
            hy +
            hz
        );


        AddBoxFace(
            start,
            0,
            1,
            2,
            3,
            triangles
        );


        AddBoxFace(
            start,
            4,
            7,
            6,
            5,
            triangles
        );


        AddBoxFace(
            start,
            0,
            4,
            5,
            1,
            triangles
        );


        AddBoxFace(
            start,
            3,
            2,
            6,
            7,
            triangles
        );


        AddBoxFace(
            start,
            1,
            5,
            6,
            2,
            triangles
        );


        AddBoxFace(
            start,
            0,
            3,
            7,
            4,
            triangles
        );
    }


    private void AddBoxFace(
        int start,
        int a,
        int b,
        int c,
        int d,
        List<int> triangles
    )
    {
        triangles.Add(
            start + a
        );

        triangles.Add(
            start + b
        );

        triangles.Add(
            start + c
        );


        triangles.Add(
            start + a
        );

        triangles.Add(
            start + c
        );

        triangles.Add(
            start + d
        );
    }


    private void AddCylinder(
        Vector3 basePosition,
        Vector3 direction,
        float lengthMeters,
        float radiusMeters,
        int radialSegments,
        List<Vector3> vertices,
        List<int> triangles
    )
    {
        direction =
            direction.normalized;


        Vector3 reference =
            Mathf.Abs(
                Vector3.Dot(
                    direction,
                    Vector3.up
                )
            )
            >
            0.9f
                ? Vector3.right
                : Vector3.up;


        Vector3 right =
            Vector3.Cross(
                direction,
                reference
            ).normalized;


        Vector3 forward =
            Vector3.Cross(
                right,
                direction
            ).normalized;


        int start =
            vertices.Count;


        float radius =
            radiusMeters *
            worldScale;


        float length =
            lengthMeters *
            worldScale;


        int segments =
            Mathf.Clamp(
                radialSegments,
                5,
                10
            );


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


            Vector3 ring =
                (
                    right *
                    Mathf.Cos(angle)
                )
                +
                (
                    forward *
                    Mathf.Sin(angle)
                );


            ring *=
                radius;


            vertices.Add(
                basePosition +
                ring
            );
        }


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


            Vector3 ring =
                (
                    right *
                    Mathf.Cos(angle)
                )
                +
                (
                    forward *
                    Mathf.Sin(angle)
                );


            ring *=
                radius;


            vertices.Add(
                basePosition +
                direction *
                length +
                ring
            );
        }


        for (
            int i = 0;
            i < segments;
            i++
        )
        {
            int next =
                (
                    i + 1
                )
                %
                segments;


            triangles.Add(
                start + i
            );

            triangles.Add(
                start +
                segments +
                i
            );

            triangles.Add(
                start +
                segments +
                next
            );


            triangles.Add(
                start + i
            );

            triangles.Add(
                start +
                segments +
                next
            );

            triangles.Add(
                start + next
            );
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
            )
            /
            Mathf.Max(
                worldScale,
                0.000001f
            );


        if (
            lengthMeters <=
            0.001f
        )
        {
            return;
        }


        AddCylinder(
            a,
            b - a,
            lengthMeters,
            radiusMeters,
            radialSegments,
            vertices,
            triangles
        );
    }


    // =========================================================
    // MESH CREATION
    // =========================================================

    private void CreateMeshObject(
        string objectName,
        List<Vector3> vertices,
        List<int> triangles,
        Material material
    )
    {
        if (
            vertices == null ||
            triangles == null ||
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
            obj.AddComponent<
                MeshFilter
            >();


        MeshRenderer renderer =
            obj.AddComponent<
                MeshRenderer
            >();


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
            "RoadsideBushes",
            "RoadsideGroundProfile",
            "NearbyBuildingDetails",
            "NearbyBuildingRails"
        };


        foreach (
            string name
            in names
        )
        {
            Transform child =
                transform.Find(
                    name
                );


            if (
                child == null
            )
            {
                continue;
            }


#if UNITY_EDITOR

            if (
                !Application.isPlaying
            )
            {
                DestroyImmediate(
                    child.gameObject
                );

                continue;
            }

#endif

            Destroy(
                child.gameObject
            );
        }
    }


    [ContextMenu("Generate Improved Environment")]
    private void GenerateFromEditor()
    {
        Generate();
    }


    [ContextMenu("Clear Improved Environment")]
    private void ClearFromEditor()
    {
        ClearGenerated();
    }
}
