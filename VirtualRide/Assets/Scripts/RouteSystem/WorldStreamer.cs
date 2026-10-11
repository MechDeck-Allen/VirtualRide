using System.Collections.Generic;
using UnityEngine;

public class WorldStreamer : MonoBehaviour
{
    // =========================================================
    // CHUNK PREFABS
    // =========================================================

    [Header("Chunk Prefabs")]

    [Tooltip(
        "All baked OSM chunk prefabs in route order."
    )]
    public List<GameObject> chunkPrefabs =
        new List<GameObject>();


    // =========================================================
    // RIDER
    // =========================================================

    [Header("Rider")]

    [Tooltip(
        "RouteBikeController that provides the real route distance."
    )]
    public RouteBikeController bikeController;


    [Tooltip(
        "Bike/player transform. Used only as a fallback/debug reference."
    )]
    public Transform rider;


    // =========================================================
    // ROUTE POSITION
    // =========================================================

    [Header("Route Position")]

    [Tooltip(
        "Current route position in kilometres. " +
        "Automatically updated from RouteBikeController."
    )]
    [SerializeField]
    private float currentRouteKm = 0f;


    // =========================================================
    // STREAMING
    // =========================================================

    [Header("Streaming")]

    [Tooltip(
        "Number of chunks to keep loaded behind the rider."
    )]
    public int chunksBehind = 1;


    [Tooltip(
        "Number of chunks to keep loaded ahead of the rider."
    )]
    public int chunksAhead = 2;


    [Tooltip(
        "How often streaming is checked."
    )]
    public float updateInterval = 0.25f;


    // =========================================================
    // WORLD PARENT
    // =========================================================

    [Header("World Parent")]

    [Tooltip(
        "Parent under which dynamically loaded chunks are created."
    )]
    public Transform loadedChunksParent;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool debugLogs = true;


    // =========================================================
    // INTERNAL
    // =========================================================

    private readonly Dictionary<int, GameObject>
        loadedChunks =
            new Dictionary<int, GameObject>();


    private float updateTimer;

    private int currentChunkIndex = -1;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // Find bike controller automatically if not assigned.
        // -----------------------------------------------------

        if (bikeController == null)
        {
            bikeController =
                FindFirstObjectByType<
                    RouteBikeController
                >();
        }


        // -----------------------------------------------------
        // Fallback rider reference.
        // -----------------------------------------------------

        if (
            rider == null &&
            bikeController != null
        )
        {
            rider =
                bikeController.transform;
        }


        // -----------------------------------------------------
        // Default parent.
        // -----------------------------------------------------

        if (loadedChunksParent == null)
        {
            loadedChunksParent =
                transform;
        }


        // -----------------------------------------------------
        // Validate.
        // -----------------------------------------------------

        ValidatePrefabs();


        // -----------------------------------------------------
        // Initial streaming pass.
        // -----------------------------------------------------

        UpdateRoutePosition();

        UpdateStreaming(true);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // Get the actual route distance from the bike.
        // -----------------------------------------------------

        UpdateRoutePosition();


        // -----------------------------------------------------
        // Streaming timer.
        // -----------------------------------------------------

        updateTimer +=
            Time.deltaTime;


        if (
            updateTimer <
            updateInterval
        )
        {
            return;
        }


        updateTimer = 0f;

        UpdateStreaming(false);
    }


    // =========================================================
    // UPDATE ROUTE POSITION
    // =========================================================

    private void UpdateRoutePosition()
    {
        if (bikeController == null)
        {
            return;
        }


        currentRouteKm =
            Mathf.Max(
                0f,
                bikeController.GetDistanceTravelledKm()
            );
    }


    // =========================================================
    // VALIDATE PREFABS
    // =========================================================

    private void ValidatePrefabs()
    {
        if (
            chunkPrefabs == null ||
            chunkPrefabs.Count == 0
        )
        {
            Debug.LogError(
                "WorldStreamer: " +
                "No OSM chunk prefabs assigned."
            );

            return;
        }


        if (debugLogs)
        {
            Debug.Log(
                "VirtualRide: WorldStreamer has " +
                chunkPrefabs.Count +
                " chunk prefabs."
            );
        }


        for (
            int i = 0;
            i < chunkPrefabs.Count;
            i++
        )
        {
            if (
                chunkPrefabs[i] == null
            )
            {
                Debug.LogWarning(
                    $"WorldStreamer: " +
                    $"Chunk slot {i} is empty."
                );
            }
        }
    }


    // =========================================================
    // STREAMING
    // =========================================================

    private void UpdateStreaming(
        bool force
    )
    {
        if (
            chunkPrefabs == null ||
            chunkPrefabs.Count == 0
        )
        {
            return;
        }


        int targetChunk =
            GetChunkIndexForRouteDistance(
                currentRouteKm
            );


        if (
            !force &&
            targetChunk ==
            currentChunkIndex
        )
        {
            return;
        }


        currentChunkIndex =
            targetChunk;


        int minimumChunk =
            Mathf.Max(
                0,
                currentChunkIndex -
                chunksBehind
            );


        int maximumChunk =
            Mathf.Min(
                chunkPrefabs.Count - 1,
                currentChunkIndex +
                chunksAhead
            );


        if (debugLogs)
        {
            Debug.Log(
                "VirtualRide: " +
                $"Route = {currentRouteKm:F2} km | " +
                $"Current chunk = {currentChunkIndex:000} | " +
                $"Loading {minimumChunk:000} → " +
                $"{maximumChunk:000}"
            );
        }


        // =====================================================
        // LOAD
        // =====================================================

        for (
            int i = minimumChunk;
            i <= maximumChunk;
            i++
        )
        {
            LoadChunk(i);
        }


        // =====================================================
        // UNLOAD
        // =====================================================

        List<int> chunksToUnload =
            new List<int>();


        foreach (
            KeyValuePair<int, GameObject>
            pair
            in loadedChunks
        )
        {
            int index =
                pair.Key;


            if (
                index <
                minimumChunk ||
                index >
                maximumChunk
            )
            {
                chunksToUnload.Add(
                    index
                );
            }
        }


        foreach (
            int index
            in chunksToUnload
        )
        {
            UnloadChunk(index);
        }
    }


    // =========================================================
    // LOAD CHUNK
    // =========================================================

    private void LoadChunk(
        int index
    )
    {
        if (
            index < 0 ||
            index >=
            chunkPrefabs.Count
        )
        {
            return;
        }


        if (
            loadedChunks.ContainsKey(
                index
            )
        )
        {
            return;
        }


        GameObject prefab =
            chunkPrefabs[index];


        if (prefab == null)
        {
            Debug.LogWarning(
                "WorldStreamer: " +
                $"Chunk {index:000} prefab is missing."
            );

            return;
        }


        GameObject instance =
            Instantiate(
                prefab,
                loadedChunksParent
            );


        instance.name =
            $"OSM_Chunk_{index:000}";


        // -----------------------------------------------------
        // All chunk prefabs are already authored in the
        // shared world coordinate system.
        // No position offset is applied here.
        // -----------------------------------------------------

        instance.transform.localPosition =
            Vector3.zero;

        instance.transform.localRotation =
            Quaternion.identity;

        instance.transform.localScale =
            Vector3.one;


        loadedChunks.Add(
            index,
            instance
        );


        if (debugLogs)
        {
            Debug.Log(
                "VirtualRide: " +
                $"Loaded OSM Chunk {index:000}"
            );
        }
    }


    // =========================================================
    // UNLOAD CHUNK
    // =========================================================

    private void UnloadChunk(
        int index
    )
    {
        if (
            !loadedChunks.TryGetValue(
                index,
                out GameObject instance
            )
        )
        {
            return;
        }


        if (
            instance != null
        )
        {
            Destroy(
                instance
            );
        }


        loadedChunks.Remove(
            index
        );


        if (debugLogs)
        {
            Debug.Log(
                "VirtualRide: " +
                $"Unloaded OSM Chunk {index:000}"
            );
        }
    }


    // =========================================================
    // FIND CURRENT CHUNK
    // =========================================================

    private int GetChunkIndexForRouteDistance(
        float routeKm
    )
    {
        if (
            chunkPrefabs == null ||
            chunkPrefabs.Count == 0
        )
        {
            return -1;
        }


        // -----------------------------------------------------
        // Chunks are currently 1 km each:
        //
        // 0.00 → 0.99 km = 000
        // 1.00 → 1.99 km = 001
        // 2.00 → 2.99 km = 002
        // 3.00 → 3.99 km = 003
        // -----------------------------------------------------

        int index =
            Mathf.FloorToInt(
                Mathf.Max(
                    0f,
                    routeKm
                )
            );


        return Mathf.Clamp(
            index,
            0,
            chunkPrefabs.Count - 1
        );
    }


    // =========================================================
    // RESET TO ROUTE START
    // =========================================================

    public void ResetToRouteStart()
    {
        Debug.Log(
            "VirtualRide: Resetting world to route start."
        );


        // -----------------------------------------------------
        // Reset route distance.
        // -----------------------------------------------------

        currentRouteKm = 0f;


        // -----------------------------------------------------
        // Force the streamer to consider chunk 0 current.
        // -----------------------------------------------------

        currentChunkIndex = -1;


        // -----------------------------------------------------
        // Unload EVERYTHING currently loaded.
        // -----------------------------------------------------

        List<int> chunksToUnload =
            new List<int>(
                loadedChunks.Keys
            );


        foreach (
            int index
            in chunksToUnload
        )
        {
            UnloadChunk(index);
        }


        // -----------------------------------------------------
        // Force immediate reload around route start.
        // -----------------------------------------------------

        UpdateStreaming(true);
    }


    // =========================================================
    // MANUAL TEST
    // =========================================================

    public void SetRouteDistance(
        float routeKm
    )
    {
        currentRouteKm =
            Mathf.Max(
                0f,
                routeKm
            );

        UpdateStreaming(true);
    }


    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu(
        "Reload Current Chunks"
    )]
    private void ReloadCurrentChunks()
    {
        foreach (
            GameObject instance
            in loadedChunks.Values
        )
        {
            if (
                instance != null
            )
            {
                DestroyImmediate(
                    instance
                );
            }
        }


        loadedChunks.Clear();

        currentChunkIndex = -1;

        UpdateRoutePosition();

        UpdateStreaming(true);
    }
}