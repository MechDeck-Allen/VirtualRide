using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    [Header("Chunk Identity")]
    public int chunkIndex = 0;

    [Header("Route Range")]
    [Tooltip("Route distance where this chunk starts, in km.")]
    public float routeStartKm = 0f;

    [Tooltip("Route distance where this chunk ends, in km.")]
    public float routeEndKm = 1f;

    [Header("Streaming")]
    [Tooltip("Allow this chunk to be loaded by the streamer.")]
    public bool streamable = true;

    [Tooltip("Keep this chunk loaded even when far away.")]
    public bool alwaysLoaded = false;


    // =========================================================
    // PROPERTIES
    // =========================================================

    public float ChunkLengthKm
    {
        get
        {
            return Mathf.Max(
                0f,
                routeEndKm - routeStartKm
            );
        }
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        Debug.Log(
            $"VirtualRide: World Chunk {chunkIndex} " +
            $"loaded ({routeStartKm:F2} - {routeEndKm:F2} km)"
        );
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(10f, 1f, 10f)
        );
    }
}