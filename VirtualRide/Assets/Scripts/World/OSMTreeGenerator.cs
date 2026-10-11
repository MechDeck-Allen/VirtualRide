using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

public class OSMTreeGenerator : MonoBehaviour
{
    [Header("OSM Data")]
    public TextAsset osmData;

    [Header("Coordinate Origin")]
    public double originLatitude = 9.9642475;
    public double originLongitude = 76.2454182;

    [Header("World Scale")]
    public float worldScale = 0.01f;

    [Header("Tree Size")]
    public float trunkHeight = 3f;
    public float canopyHeight = 4f;
    public float canopyRadius = 1.75f;
    public float trunkRadius = 0.15f;

    [Header("Variation")]
    [Range(0f, 0.5f)]
    public float heightVariation = 0.20f;

    [Range(0f, 0.5f)]
    public float radiusVariation = 0.25f;

    [Range(5, 10)]
    public int radialSegments = 7;

    [Header("Materials")]
    public Material trunkMaterial;
    public Material leavesMaterial;

    [Header("Generation")]
    public bool generateOnStart = false;


    private struct TreePoint
    {
        public double latitude;
        public double longitude;
        public float height;
        public float crownDiameter;
    }


    private void Start()
    {
        if (generateOnStart)
        {
            GenerateTrees();
        }
    }


    // =========================================================
    // MAIN
    // =========================================================

    public void GenerateTrees()
    {
        ClearTrees();


        if (osmData == null)
        {
            Debug.LogError(
                "OSMTreeGenerator: OSM Data is not assigned."
            );

            return;
        }


        List<TreePoint> trees =
            ParseTreeArrayDirectly(
                osmData.text
            );


        Debug.Log(
            "OSMTreeGenerator: Directly parsed tree points = " +
            trees.Count
        );


        if (trees.Count == 0)
        {
            Debug.LogWarning(
                "OSMTreeGenerator: Could not extract any trees from " +
                osmData.name
            );

            return;
        }


        List<Vector3> trunkVertices =
            new List<Vector3>();


        List<int> trunkTriangles =
            new List<int>();


        List<Vector3> canopyVertices =
            new List<Vector3>();


        List<int> canopyTriangles =
            new List<int>();


        for (
            int i = 0;
            i < trees.Count;
            i++
        )
        {
            GenerateTree(
                trees[i],
                trunkVertices,
                trunkTriangles,
                canopyVertices,
                canopyTriangles
            );
        }


        CreateMeshObject(
            "TreeTrunks",
            trunkVertices,
            trunkTriangles,
            trunkMaterial
        );


        CreateMeshObject(
            "TreeCanopies",
            canopyVertices,
            canopyTriangles,
            leavesMaterial
        );


        Debug.Log(
            "OSMTreeGenerator: Tree geometry created for " +
            trees.Count +
            " trees."
        );
    }


    // =========================================================
    // DIRECT TEXT PARSER
    // =========================================================
    //
    // We deliberately avoid JsonUtility here.
    //
    // We locate the actual "trees":[ ... ] section and extract
    // latitude/longitude values directly from each object.
    // This avoids any dependency on Unity's JSON field mapping.
    //
    // =========================================================

    private List<TreePoint> ParseTreeArrayDirectly(
        string json
    )
    {
        List<TreePoint> result =
            new List<TreePoint>();


        if (
            string.IsNullOrEmpty(json)
        )
        {
            return result;
        }


        Match arrayMatch =
            Regex.Match(
                json,
                @"""trees""\s*:\s*\[(.*?)\]",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );


        if (
            !arrayMatch.Success
        )
        {
            Debug.LogWarning(
                "OSMTreeGenerator: No 'trees' array was found."
            );

            return result;
        }


        string arrayText =
            arrayMatch.Groups[1].Value;


        MatchCollection objects =
            Regex.Matches(
                arrayText,
                @"\{(.*?)\}",
                RegexOptions.Singleline
            );


        foreach (
            Match objectMatch
            in objects
        )
        {
            string objectText =
                objectMatch.Groups[1].Value;


            Match latMatch =
                Regex.Match(
                    objectText,
                    @"""latitude""\s*:\s*(-?[0-9]+(?:\.[0-9]+)?)",
                    RegexOptions.IgnoreCase
                );


            Match lonMatch =
                Regex.Match(
                    objectText,
                    @"""longitude""\s*:\s*(-?[0-9]+(?:\.[0-9]+)?)",
                    RegexOptions.IgnoreCase
                );


            if (
                !latMatch.Success ||
                !lonMatch.Success
            )
            {
                continue;
            }


            if (
                !double.TryParse(
                    latMatch.Groups[1].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double latitude
                )
            )
            {
                continue;
            }


            if (
                !double.TryParse(
                    lonMatch.Groups[1].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double longitude
                )
            )
            {
                continue;
            }


            float height =
                0f;


            Match heightMatch =
                Regex.Match(
                    objectText,
                    @"""height""\s*:\s*""?([0-9]+(?:\.[0-9]+)?)""?",
                    RegexOptions.IgnoreCase
                );


            if (
                heightMatch.Success
            )
            {
                float.TryParse(
                    heightMatch.Groups[1].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out height
                );
            }


            float crownDiameter =
                0f;


            Match crownMatch =
                Regex.Match(
                    objectText,
                    @"""crownDiameter""\s*:\s*""?([0-9]+(?:\.[0-9]+)?)""?",
                    RegexOptions.IgnoreCase
                );


            if (
                crownMatch.Success
            )
            {
                float.TryParse(
                    crownMatch.Groups[1].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out crownDiameter
                );
            }


            result.Add(
                new TreePoint
                {
                    latitude = latitude,
                    longitude = longitude,
                    height = height,
                    crownDiameter = crownDiameter
                }
            );
        }


        return result;
    }


    // =========================================================
    // GENERATE TREE
    // =========================================================

    private void GenerateTree(
        TreePoint tree,
        List<Vector3> trunkVertices,
        List<int> trunkTriangles,
        List<Vector3> canopyVertices,
        List<int> canopyTriangles
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
                tree.longitude -
                originLongitude
            )
            *
            metersPerDegreeLongitude;


        double zMeters =
            (
                tree.latitude -
                originLatitude
            )
            *
            111320.0;


        Vector3 position =
            new Vector3(
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


        int seed =
            Mathf.Abs(
                (
                    tree.latitude.GetHashCode() *
                    397
                )
                ^
                tree.longitude.GetHashCode()
            );


        System.Random random =
            new System.Random(seed);


        float heightScale =
            1f +
            Mathf.Lerp(
                -heightVariation,
                heightVariation,
                (float)random.NextDouble()
            );


        float radiusScale =
            1f +
            Mathf.Lerp(
                -radiusVariation,
                radiusVariation,
                (float)random.NextDouble()
            );


        float totalHeight =
            (
                trunkHeight +
                canopyHeight
            )
            *
            heightScale;


        if (
            tree.height > 1f
        )
        {
            totalHeight =
                tree.height;
        }


        float actualTrunkHeight =
            Mathf.Max(
                1f,
                totalHeight *
                0.45f
            );


        float actualCanopyHeight =
            Mathf.Max(
                0.75f,
                totalHeight -
                actualTrunkHeight
            );


        float actualTrunkRadius =
            trunkRadius *
            radiusScale;


        float actualCanopyRadius =
            canopyRadius *
            radiusScale;


        if (
            tree.crownDiameter > 0.2f
        )
        {
            actualCanopyRadius =
                tree.crownDiameter *
                0.5f *
                radiusScale;
        }


        AddTrunk(
            position,
            actualTrunkHeight,
            actualTrunkRadius,
            trunkVertices,
            trunkTriangles
        );


        AddCanopy(
            position +
            Vector3.up *
            (
                actualTrunkHeight *
                worldScale
            ),
            actualCanopyHeight,
            actualCanopyRadius,
            canopyVertices,
            canopyTriangles
        );
    }


    // =========================================================
    // TRUNK
    // =========================================================

    private void AddTrunk(
        Vector3 position,
        float heightMeters,
        float radiusMeters,
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


            vertices.Add(
                position +
                new Vector3(
                    Mathf.Cos(angle) *
                    radius,
                    0f,
                    Mathf.Sin(angle) *
                    radius
                )
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


            vertices.Add(
                position +
                new Vector3(
                    Mathf.Cos(angle) *
                    radius,
                    height,
                    Mathf.Sin(angle) *
                    radius
                )
            );
        }


        for (
            int i = 0;
            i < segments;
            i++
        )
        {
            int next =
                (i + 1) %
                segments;


            triangles.Add(
                start + i
            );

            triangles.Add(
                start + segments + i
            );

            triangles.Add(
                start + segments + next
            );


            triangles.Add(
                start + i
            );

            triangles.Add(
                start + segments + next
            );

            triangles.Add(
                start + next
            );
        }
    }


    // =========================================================
    // CANOPY
    // =========================================================

    private void AddCanopy(
        Vector3 basePosition,
        float heightMeters,
        float radiusMeters,
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


        Vector3 tip =
            basePosition +
            Vector3.up *
            height;


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


            vertices.Add(
                basePosition +
                new Vector3(
                    Mathf.Cos(angle) *
                    radius,
                    0f,
                    Mathf.Sin(angle) *
                    radius
                )
            );
        }


        int tipIndex =
            vertices.Count;


        vertices.Add(
            tip
        );


        for (
            int i = 0;
            i < segments;
            i++
        )
        {
            int next =
                (i + 1) %
                segments;


            triangles.Add(
                start + i
            );

            triangles.Add(
                tipIndex
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
            triangles.Add(
                start
            );

            triangles.Add(
                start + i + 1
            );

            triangles.Add(
                start + i
            );
        }
    }


    // =========================================================
    // MESH
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
            obj.AddComponent<MeshFilter>();


        MeshRenderer renderer =
            obj.AddComponent<MeshRenderer>();


        Mesh mesh =
            new Mesh();


        mesh.name =
            objectName +
            "_Mesh";


        if (
            vertices.Count > 65535
        )
        {
            mesh.indexFormat =
                IndexFormat.UInt32;
        }


        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

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

    public void ClearTrees()
    {
        Transform trunks =
            transform.Find(
                "TreeTrunks"
            );


        Transform canopies =
            transform.Find(
                "TreeCanopies"
            );


        if (trunks != null)
        {
            DestroyGenerated(
                trunks.gameObject
            );
        }


        if (canopies != null)
        {
            DestroyGenerated(
                canopies.gameObject
            );
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


    [ContextMenu("Generate Trees")]
    private void GenerateTreesFromEditor()
    {
        GenerateTrees();
    }


    [ContextMenu("Clear Trees")]
    private void ClearTreesFromEditor()
    {
        ClearTrees();
    }
}
