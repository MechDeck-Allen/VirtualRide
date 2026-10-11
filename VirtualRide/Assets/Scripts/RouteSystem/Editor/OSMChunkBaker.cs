#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class OSMChunkBaker
{
    private const string PREFAB_FOLDER =
        "Assets/Prefabs/OSMChunks";

    private const string MESH_FOLDER =
        "Assets/Generated/OSMMeshes";

    private const string WORLD_DATA_FOLDER =
        "world_data/alappuzha/processed";


    // =========================================================
    // TEST CHUNK 000
    // =========================================================

    [MenuItem("VirtualRide/OSM/Bake Test Chunk 000")]
    public static void BakeTestChunk000()
    {
        EnsureFolders();


        // Unity's Application.dataPath is:
        // E:\\projects\\unity_virtualsim\\VirtualRide\\Assets
        //
        // Your world_data folder is one level ABOVE the Unity
        // project folder:
        // E:\\projects\\unity_virtualsim\\world_data\\...
        //
        // Therefore we go up TWO levels from Assets.
        string unityProjectRoot =
            Directory.GetParent(
                Application.dataPath
            ).FullName;


        string workspaceRoot =
            Directory.GetParent(
                unityProjectRoot
            ).FullName;


        string processedRoot =
            Path.Combine(
                workspaceRoot,
                "world_data",
                "alappuzha",
                "processed"
            );


        string jsonPath =
            Path.Combine(
                processedRoot,
                "chunk_000.json"
            );


        Debug.Log(
            "OSMChunkBaker: Looking for processed data at:\n" +
            jsonPath
        );


        if (
            !File.Exists(jsonPath)
        )
        {
            Debug.LogError(
                "OSMChunkBaker: Processed chunk not found:\n" +
                jsonPath
            );

            return;
        }


        BakeChunkFromFile(
            jsonPath
        );


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    // =========================================================
    // BAKE ALL
    // =========================================================

    [MenuItem("VirtualRide/OSM/Bake All Chunks")]
    public static void BakeAllChunks()
    {
        EnsureFolders();


        string unityProjectRoot =
            Directory.GetParent(
                Application.dataPath
            ).FullName;


        string workspaceRoot =
            Directory.GetParent(
                unityProjectRoot
            ).FullName;


        string processedRoot =
            Path.Combine(
                workspaceRoot,
                "world_data",
                "alappuzha",
                "processed"
            );


        if (
            !Directory.Exists(
                processedRoot
            )
        )
        {
            Debug.LogError(
                "OSMChunkBaker: Processed folder not found:\n" +
                processedRoot
            );

            return;
        }


        string[] files =
            Directory.GetFiles(
                processedRoot,
                "chunk_*.json",
                SearchOption.TopDirectoryOnly
            );


        Array.Sort(
            files,
            StringComparer.OrdinalIgnoreCase
        );


        int baked =
            0;


        foreach (
            string file
            in files
        )
        {
            if (
                BakeChunkFromFile(
                    file
                )
            )
            {
                baked++;
            }
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        Debug.Log(
            $"VirtualRide: Successfully baked {baked} processed chunks."
        );
    }


    // =========================================================
    // BAKE ONE EXTERNAL JSON FILE
    // =========================================================

    private static bool BakeChunkFromFile(
        string jsonPath
    )
    {
        if (
            !File.Exists(
                jsonPath
            )
        )
        {
            Debug.LogError(
                "OSMChunkBaker: File does not exist:\n" +
                jsonPath
            );

            return false;
        }


        string jsonText =
            File.ReadAllText(
                jsonPath
            );


        int chunkIndex =
            ExtractInt(
                jsonText,
                "\"chunkIndex\""
            );


        if (
            chunkIndex < 0
        )
        {
            Debug.LogError(
                "OSMChunkBaker: Invalid/missing chunkIndex in:\n" +
                jsonPath
            );

            return false;
        }


        if (
            !LooksLikeProcessedChunk(
                jsonText
            )
        )
        {
            Debug.LogError(
                "OSMChunkBaker: This file does not look like " +
                "processed world data:\n" +
                jsonPath
            );

            return false;
        }


        int treeCount =
            CountTreeRecords(
                jsonText
            );


        Debug.Log(
            "=========================================="
        );


        Debug.Log(
            $"VirtualRide: Baking chunk {chunkIndex:000}"
        );


        Debug.Log(
            "SOURCE = " +
            jsonPath
        );


        Debug.Log(
            $"TREE RECORDS IN SOURCE = {treeCount}"
        );


        // -----------------------------------------------------
        // Temporary TextAsset.
        //
        // This file remains on disk in world_data and is NOT
        // required to be inside Assets. The generators only
        // need the text content while the bake is running.
        // -----------------------------------------------------

        TextAsset jsonAsset =
            new TextAsset(
                jsonText
            );


        GameObject root =
            new GameObject(
                $"OSM_Chunk_{chunkIndex:000}"
            );


        try
        {
            DeleteExistingChunkMeshes(
                chunkIndex
            );


            string prefabPath =
                $"{PREFAB_FOLDER}/OSM_Chunk_{chunkIndex:000}.prefab";


            if (
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPath
                ) != null
            )
            {
                AssetDatabase.DeleteAsset(
                    prefabPath
                );
            }


            // =================================================
            // ROUTE JSON
            // =================================================

            TextAsset routeAsset =
                LoadExternalTextAsset(
                    GetRouteJsonPath()
                );

            // =================================================
            // WORLD
            // =================================================

            OSMWorldGenerator world =
                root.AddComponent<
                    OSMWorldGenerator
                >();


            world.osmData =
                jsonAsset;

            world.routeJson =
                routeAsset;

            world.roadsideBuildingSetbackMeters =
                2.0f;

            world.roadsideBuildingInfluenceMeters =
                35f;


            world.generateOnStart =
                false;


            world.originLatitude =
                9.9642475;


            world.originLongitude =
                76.2454182;


            world.worldScale =
                0.01f;


            world.roadWidth =
                3f;


            world.roadHeight =
                0.05f;


            world.waterWidth =
                4f;


            world.waterHeight =
                0.02f;


            world.naturalWaterHeight =
                0.10f;


            world.buildingHeight =
                12f;


            world.buildingBaseHeight =
                0.02f;


            world.useBuildingTypeHeights =
                true;


            world.generateRoofs =
                true;


            world.roofHeight =
                5f;


            world.churchRoofHeight =
                8f;


            world.generateFacades =
                true;


            world.windowWidth =
                1.2f;


            world.windowHeight =
                1.2f;


            world.windowHorizontalSpacing =
                3f;


            world.windowVerticalSpacing =
                3f;


            world.doorWidth =
                1.1f;


            world.doorHeight =
                2.2f;


            world.facadeOffset =
                0.03f;


            world.maxWindowsPerWall =
                4;


            world.maxWindowRows =
                4;


            world.groundHeight =
                -0.02f;


            world.groundMarginMeters =
                25f;


            world.roadMaterial =
                FindMaterial(
                    "OSM_Road_Material"
                );


            world.waterMaterial =
                FindMaterial(
                    "OSM_Water_Material"
                );


            Material buildingMaterial =
                FindMaterial(
                    "OSM_Building_Material"
                );

            world.buildingMaterial =
                buildingMaterial;


            world.roofMaterial =
                FindMaterial(
                    "OSM_Roof_Material"
                );


            world.groundMaterial =
                FindMaterial(
                    "Ground_Material"
                );


            world.windowMaterial =
                FindMaterial(
                    "OSM_Window_Material"
                );


            world.doorMaterial =
                FindMaterial(
                    "OSM_Door_Material"
                );


            world.GenerateWorld();


            // =================================================
            // OSM ROAD NETWORK
            // =================================================
            // Keep the complete OSM road network in the chunk.
            // The dedicated cycling route is a separate riding surface.
            Transform osmRoads =
                root.transform.Find(
                    "Roads"
                );

            if (osmRoads != null)
            {
                osmRoads.gameObject.SetActive(true);

                Debug.Log(
                    "OSMChunkBaker: OSM road network kept active."
                );
            }


            // =================================================
            // FORCE BUILDING MATERIAL
            // =================================================

            if (
                !UnityEngine.Object.ReferenceEquals(
                    buildingMaterial,
                    null
                )
            )
            {
                MeshRenderer[] generatedRenderers =
                    root.GetComponentsInChildren<MeshRenderer>(
                        true
                    );

                foreach (
                    MeshRenderer generatedRenderer
                    in generatedRenderers
                )
                {
                    if (generatedRenderer == null)
                    {
                        continue;
                    }

                    string generatedName =
                        generatedRenderer.gameObject.name;

                    if (
                        generatedName == "BuildingMesh" ||
                        generatedName ==
                        "NearRoadBuildingArchitecture"
                    )
                    {
                        generatedRenderer.sharedMaterial =
                            buildingMaterial;
                    }
                }
            }


            UnityEngine.Object.DestroyImmediate(
                world
            );


            // =================================================
            // NEAR-ROUTE ROADSIDE DETAILS
            // =================================================

            if (routeAsset != null)
            {
                OSMRoadsideDetailGenerator roadside =
                    root.AddComponent<
                        OSMRoadsideDetailGenerator
                    >();

                roadside.processedChunk =
                    jsonAsset;

                roadside.routeJson =
                    routeAsset;

                roadside.originLatitude =
                    9.9642475;

                roadside.originLongitude =
                    76.2454182;

                roadside.worldScale =
                    0.01f;

                roadside.corridorRadiusMeters =
                    35f;

                roadside.chunkStartKm =
                    ExtractFloat(
                        jsonText,
                        "\"routeStartKm\""
                    );

                roadside.chunkEndKm =
                    ExtractFloat(
                        jsonText,
                        "\"routeEndKm\""
                    );

                roadside.treeTrunkHeight = 3f;
                roadside.treeCanopyHeight = 4f;
                roadside.treeCanopyRadius = 1.75f;
                roadside.treeTrunkRadius = 0.15f;
                roadside.treeSegments = 7;

                roadside.generateProceduralPoles = true;
                roadside.poleSpacingMeters = 35f;
                roadside.poleHeightMeters = 6f;
                roadside.poleRadiusMeters = 0.09f;
                roadside.poleOffsetFromRouteMeters = 5.0f;

                roadside.generateProceduralLamps = false;
                roadside.lampSpacingMeters = 45f;
                roadside.lampHeightMeters = 5.5f;
                roadside.lampPoleRadiusMeters = 0.07f;

                roadside.fencePostSpacingMeters = 3f;
                roadside.fenceHeightMeters = 1.2f;
                roadside.fencePostRadiusMeters = 0.05f;
                roadside.fenceRailRadiusMeters = 0.035f;

                roadside.generateBridgeRails = true;
                roadside.bridgeRailHeightMeters = 1f;
                roadside.bridgeRailSpacingMeters = 3f;

                roadside.trunkMaterial =
                    FindMaterial("OSM_Tree_Trunk_Material");

                roadside.leavesMaterial =
                    FindMaterial("OSM_Tree_Leaves_Material");

                roadside.poleMaterial =
                    FindMaterial("OSM_Pole_Material");

                roadside.lampMaterial =
                    FindMaterial("OSM_Lamp_Material");

                roadside.fenceMaterial =
                    FindMaterial("OSM_Fence_Material");

                roadside.bridgeRailMaterial =
                    FindMaterial("OSM_BridgeRail_Material");

                roadside.generateOnStart =
                    false;

                roadside.GenerateDetails();

                // -------------------------------------------------
                // NEAR-ROAD ENVIRONMENT ENHANCER
                // -------------------------------------------------

                RoadsideEnvironmentEnhancer environment =
                    root.AddComponent<
                        RoadsideEnvironmentEnhancer
                    >();

                environment.processedChunk =
                    jsonAsset;

                environment.routeJson =
                    routeAsset;

                environment.originLatitude =
                    9.9642475;

                environment.originLongitude =
                    76.2454182;

                environment.worldScale =
                    0.01f;

                environment.groundHeight =
                    -0.02f;

                environment.chunkStartKm =
                    roadside.chunkStartKm;

                environment.chunkEndKm =
                    roadside.chunkEndKm;

                environment.detailRadiusMeters =
                    35f;

                environment.generateBushes =
                    true;

                environment.bushSpacingMeters =
                    10f;

                environment.bushMinOffsetMeters =
                    4.5f;

                environment.bushMaxOffsetMeters =
                    10f;

                environment.bushMinHeightMeters =
                    0.45f;

                environment.bushMaxHeightMeters =
                    1.10f;

                environment.bushMinRadiusMeters =
                    0.50f;

                environment.bushMaxRadiusMeters =
                    1.10f;

                environment.bushClustersPerSide =
                    1;

                environment.generateGroundProfile =
                    true;

                environment.groundInnerOffsetMeters =
                    3.2f;

                environment.groundOuterOffsetMeters =
                    10f;

                environment.groundProfileStepMeters =
                    5f;

                environment.groundVariationMeters =
                    0.10f;

                environment.generateBuildingDetails =
                    true;

                environment.buildingDetailRadiusMeters =
                    28f;

                // Keep the roadside detail generator aligned with
                // OSMWorldGenerator's building setback.
                environment.buildingSetbackMeters =
                    2.0f;

                environment.buildingInfluenceMeters =
                    35f;

                environment.balconyLevelOneMeters =
                    3.2f;

                environment.balconyLevelTwoMeters =
                    6f;

                environment.balconyWidthMeters =
                    2.2f;

                environment.balconyDepthMeters =
                    1f;

                environment.balconySlabThicknessMeters =
                    0.12f;

                environment.balconyRailHeightMeters =
                    1f;

                environment.balconyRailRadiusMeters =
                    0.035f;

                environment.rooftopTankHeightMeters =
                    0.9f;

                environment.rooftopTankRadiusMeters =
                    0.6f;

                environment.rooftopTankClearanceMeters =
                    0.25f;

                environment.vegetationMaterial =
                    FindMaterial(
                        "OSM_Tree_Leaves_Material"
                    );

                environment.groundMaterial =
                    FindMaterial(
                        "Ground_Material"
                    );

                environment.buildingDetailMaterial =
                    FindMaterial(
                        "OSM_Building_Material"
                    );

                environment.railMaterial =
                    FindMaterial(
                        "OSM_Fence_Material"
                    );

                environment.Generate();

                UnityEngine.Object.DestroyImmediate(
                    environment
                );

                UnityEngine.Object.DestroyImmediate(
                    roadside
                );

                UnityEngine.Object.DestroyImmediate(
                    routeAsset
                );
            }
            else
            {
                Debug.LogWarning(
                    "OSMChunkBaker: snapped_route_data.json not found. " +
                    "Near-route details skipped."
                );
            }


            // =================================================
            // WORLD CHUNK METADATA
            // =================================================

            WorldChunk metadata =
                root.AddComponent<
                    WorldChunk
                >();


            metadata.chunkIndex =
                chunkIndex;


            metadata.routeStartKm =
                ExtractFloat(
                    jsonText,
                    "\"routeStartKm\""
                );


            metadata.routeEndKm =
                ExtractFloat(
                    jsonText,
                    "\"routeEndKm\""
                );


            metadata.streamable =
                true;


            metadata.alwaysLoaded =
                false;


            // =================================================
            // PERSIST MESHES
            // =================================================

            if (
                !SaveMeshesAsAssets(
                    root,
                    chunkIndex
                )
            )
            {
                return false;
            }


            AssetDatabase.SaveAssets();


            MeshFilter[] before =
                root.GetComponentsInChildren<
                    MeshFilter
                >(true);


            foreach (
                MeshFilter filter
                in before
            )
            {
                if (
                    filter == null ||
                    filter.sharedMesh == null
                )
                {
                    Debug.LogError(
                        "OSMChunkBaker: Null mesh before prefab save: " +
                        GetHierarchyPath(
                            filter
                        )
                    );

                    return false;
                }
            }


            // =================================================
            // PREFAB
            // =================================================

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    prefabPath
                );


            if (
                saved == null
            )
            {
                Debug.LogError(
                    "OSMChunkBaker: Failed to save prefab:\n" +
                    prefabPath
                );

                return false;
            }


            AssetDatabase.ImportAsset(
                prefabPath,
                ImportAssetOptions.ForceSynchronousImport
            );


            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPath
                );


            if (
                prefab == null
            )
            {
                return false;
            }


            MeshFilter[] after =
                prefab.GetComponentsInChildren<
                    MeshFilter
                >(true);


            int valid =
                0;


            foreach (
                MeshFilter filter
                in after
            )
            {
                if (
                    filter != null &&
                    filter.sharedMesh != null
                )
                {
                    valid++;
                }
            }


            bool hasTrunks =
                prefab.transform.Find(
                    "TreeTrunks"
                ) != null;


            bool hasCanopies =
                prefab.transform.Find(
                    "TreeCanopies"
                ) != null;


            Debug.Log(
                $"OSMChunkBaker: Mesh refs " +
                $"{valid}/{after.Length}"
            );


            Debug.Log(
                $"OSMChunkBaker: TreeTrunks={hasTrunks}, " +
                $"TreeCanopies={hasCanopies}"
            );


            Debug.Log(
                $"VirtualRide: Chunk {chunkIndex:000} bake complete."
            );


            Debug.Log(
                "=========================================="
            );


            return (
                after.Length > 0 &&
                valid == after.Length
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                root
            );


            // TextAsset is a temporary in-memory object.
            UnityEngine.Object.DestroyImmediate(
                jsonAsset
            );
        }
    }


    // =========================================================
    // PROCESSED CHUNK CHECK
    // =========================================================

    private static bool LooksLikeProcessedChunk(
        string json
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                json
            )
        )
        {
            return false;
        }


        return (
            json.Contains("\"roads\"") &&
            json.Contains("\"buildings\"") &&
            !json.Contains("\"osm\"")
        );
    }


    // =========================================================
    // TREE COUNT
    // =========================================================

    private static int CountTreeRecords(
        string json
    )
    {
        if (
            string.IsNullOrEmpty(
                json
            )
        )
        {
            return 0;
        }


        const string token =
            "\"type\":\"tree\"";


        int count =
            0;


        int position =
            0;


        while (
            true
        )
        {
            int found =
                json.IndexOf(
                    token,
                    position,
                    StringComparison.OrdinalIgnoreCase
                );


            if (
                found < 0
            )
            {
                break;
            }


            count++;


            position =
                found +
                token.Length;
        }


        return count;
    }


    // =========================================================
    // NUMBER HELPERS
    // =========================================================

    private static int ExtractInt(
        string json,
        string key
    )
    {
        return Mathf.RoundToInt(
            ExtractFloat(
                json,
                key,
                -1f
            )
        );
    }


    private static float ExtractFloat(
        string json,
        string key
    )
    {
        return ExtractFloat(
            json,
            key,
            0f
        );
    }


    private static float ExtractFloat(
        string json,
        string key,
        float fallback
    )
    {
        int keyIndex =
            json.IndexOf(
                key,
                StringComparison.OrdinalIgnoreCase
            );


        if (
            keyIndex < 0
        )
        {
            return fallback;
        }


        int colon =
            json.IndexOf(
                ':',
                keyIndex
            );


        if (
            colon < 0
        )
        {
            return fallback;
        }


        int start =
            colon + 1;


        while (
            start < json.Length &&
            char.IsWhiteSpace(
                json[start]
            )
        )
        {
            start++;
        }


        int end =
            start;


        while (
            end < json.Length &&
            (
                char.IsDigit(json[end]) ||
                json[end] == '-' ||
                json[end] == '+' ||
                json[end] == '.' ||
                json[end] == 'e' ||
                json[end] == 'E'
            )
        )
        {
            end++;
        }


        if (
            end <= start
        )
        {
            return fallback;
        }


        string value =
            json.Substring(
                start,
                end - start
            );


        if (
            float.TryParse(
                value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float result
            )
        )
        {
            return result;
        }


        return fallback;
    }


    // =========================================================
    // SAVE MESHES
    // =========================================================

    private static bool SaveMeshesAsAssets(
        GameObject root,
        int chunkIndex
    )
    {
        MeshFilter[] filters =
            root.GetComponentsInChildren<
                MeshFilter
            >(true);


        int saved =
            0;


        foreach (
            MeshFilter filter
            in filters
        )
        {
            if (
                filter == null ||
                filter.sharedMesh == null
            )
            {
                continue;
            }


            string safe =
                MakeSafeFileName(
                    filter.gameObject.name
                );


            string meshPath =
                $"{MESH_FOLDER}/Chunk_{chunkIndex:000}_{safe}.asset";


            if (
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    meshPath
                ) != null
            )
            {
                AssetDatabase.DeleteAsset(
                    meshPath
                );
            }


            Mesh copy =
                UnityEngine.Object.Instantiate(
                    filter.sharedMesh
                );


            copy.name =
                Path.GetFileNameWithoutExtension(
                    meshPath
                );


            AssetDatabase.CreateAsset(
                copy,
                meshPath
            );


            AssetDatabase.SaveAssets();


            AssetDatabase.ImportAsset(
                meshPath,
                ImportAssetOptions.ForceSynchronousImport
            );


            Mesh persistent =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    meshPath
                );


            if (
                persistent == null
            )
            {
                return false;
            }


            filter.sharedMesh =
                persistent;


            EditorUtility.SetDirty(
                filter
            );


            saved++;
        }


        Debug.Log(
            $"VirtualRide: Saved {saved} persistent meshes " +
            $"for chunk {chunkIndex}."
        );


        return saved > 0;
    }


    // =========================================================
    // DELETE OLD MESHES
    // =========================================================

    private static void DeleteExistingChunkMeshes(
        int chunkIndex
    )
    {
        string prefix =
            $"Chunk_{chunkIndex:000}_";


        string[] guids =
            AssetDatabase.FindAssets(
                "t:Mesh",
                new[] { MESH_FOLDER }
            );


        foreach (
            string guid
            in guids
        )
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );


            if (
                Path.GetFileName(
                    path
                ).StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                AssetDatabase.DeleteAsset(
                    path
                );
            }
        }
    }


    // =========================================================
    // EXTERNAL ROUTE JSON
    // =========================================================

    private static string GetRouteJsonPath()
    {
        string unityProjectRoot =
            Directory.GetParent(
                Application.dataPath
            ).FullName;

        string workspaceRoot =
            Directory.GetParent(
                unityProjectRoot
            ).FullName;

        return Path.Combine(
            workspaceRoot,
            "routes",
            "alappuzha",
            "snapped_route_data.json"
        );
    }


    private static TextAsset LoadExternalTextAsset(
        string path
    )
    {
        if (
            string.IsNullOrEmpty(path) ||
            !File.Exists(path)
        )
        {
            return null;
        }

        return new TextAsset(
            File.ReadAllText(path)
        );
    }


    // =========================================================
    // MATERIAL
    // =========================================================

    private static Material FindMaterial(
        string materialName
    )
    {
        string[] guids =
            AssetDatabase.FindAssets(
                $"{materialName} t:Material"
            );


        if (
            guids == null ||
            guids.Length == 0
        )
        {
            Debug.LogWarning(
                "OSMChunkBaker: Material not found: " +
                materialName
            );

            return null;
        }


        return AssetDatabase.LoadAssetAtPath<Material>(
            AssetDatabase.GUIDToAssetPath(
                guids[0]
            )
        );
    }


    // =========================================================
    // FOLDERS
    // =========================================================

    private static void EnsureFolders()
    {
        CreateFolderRecursive(
            PREFAB_FOLDER
        );


        CreateFolderRecursive(
            MESH_FOLDER
        );
    }


    private static void CreateFolderRecursive(
        string folderPath
    )
    {
        string[] parts =
            folderPath.Split('/');


        string current =
            parts[0];


        for (
            int i = 1;
            i < parts.Length;
            i++
        )
        {
            string next =
                $"{current}/{parts[i]}";


            if (
                !AssetDatabase.IsValidFolder(
                    next
                )
            )
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }


            current =
                next;
        }
    }


    // =========================================================
    // SAFE NAME
    // =========================================================

    private static string MakeSafeFileName(
        string value
    )
    {
        foreach (
            char c
            in Path.GetInvalidFileNameChars()
        )
        {
            value =
                value.Replace(
                    c,
                    '_'
                );
        }


        return value;
    }


    // =========================================================
    // HIERARCHY PATH
    // =========================================================

    private static string GetHierarchyPath(
        Component component
    )
    {
        if (
            component == null
        )
        {
            return "<NULL COMPONENT>";
        }


        string path =
            component.gameObject.name;


        Transform parent =
            component.transform.parent;


        while (
            parent != null
        )
        {
            path =
                parent.name +
                "/" +
                path;


            parent =
                parent.parent;
        }


        return path;
    }
}

#endif
