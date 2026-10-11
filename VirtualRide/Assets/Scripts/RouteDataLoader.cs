using UnityEngine;

public class RouteDataLoader : MonoBehaviour
{
    [Header("Route Data")]
    public TextAsset routeFile;

    [Header("Debug")]
    public bool loadOnStart = true;

    private RouteData routeData;

    public RouteData Data => routeData;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (loadOnStart)
        {
            LoadRoute();
        }
    }


    // =========================================================
    // LOAD ROUTE
    // =========================================================

    public bool LoadRoute()
    {
        if (routeFile == null)
        {
            Debug.LogError(
                "RouteDataLoader: No route JSON assigned."
            );

            routeData = null;

            return false;
        }


        try
        {
            RouteData loadedData =
                JsonUtility.FromJson<RouteData>(
                    routeFile.text
                );


            if (loadedData == null)
            {
                Debug.LogError(
                    "RouteDataLoader: Failed to parse route JSON."
                );

                routeData = null;

                return false;
            }


            if (
                loadedData.points == null ||
                loadedData.points.Length < 2
            )
            {
                Debug.LogError(
                    "RouteDataLoader: Route JSON contains " +
                    "fewer than 2 points."
                );

                Debug.LogError(
                    $"Route file: {routeFile.name}"
                );

                Debug.LogError(
                    $"Reported point count: " +
                    $"{loadedData.pointCount}"
                );

                routeData = null;

                return false;
            }


            routeData =
                loadedData;


            Debug.Log(
                "VirtualRide: Route Data Loaded"
            );

            Debug.Log(
                $"Route: {routeData.routeName}"
            );

            Debug.Log(
                $"Distance: {routeData.distanceKm:F3} km"
            );

            Debug.Log(
                $"Points actually loaded: " +
                $"{routeData.points.Length}"
            );


            return true;
        }
        catch (System.Exception error)
        {
            Debug.LogError(
                "RouteDataLoader: JSON loading failed.\n" +
                error
            );

            routeData = null;

            return false;
        }
    }
}