using UnityEngine;

public class RouteProgressTracker : MonoBehaviour
{
    [Header("Rider")]
    [Tooltip("Assign the rider/bike Transform when one exists.")]
    public Transform rider;

    [Header("Route")]
    public RouteDataLoader routeLoader;

    [Header("Progress")]
    [SerializeField]
    private float distanceTravelledKm = 0f;

    [SerializeField]
    private int nearestPointIndex = 0;

    [Header("Debug")]
    public bool logProgress = false;

    // =========================================================
    // UNITY
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // NO RIDER YET
        // -----------------------------------------------------
        //
        // This is intentional.
        //
        // We are currently building the world and route system
        // before the physical/virtual rider is implemented.
        //
        // Therefore the tracker simply waits.
        //
        if (rider == null)
        {
            return;
        }

        // -----------------------------------------------------
        // NO ROUTE LOADER
        // -----------------------------------------------------

        if (routeLoader == null)
        {
            return;
        }

        // -----------------------------------------------------
        // ROUTE DATA NOT LOADED
        // -----------------------------------------------------

        if (routeLoader.Data == null)
        {
            return;
        }

        if (routeLoader.Data.points == null)
        {
            return;
        }

        if (routeLoader.Data.points.Length < 2)
        {
            return;
        }

        // -----------------------------------------------------
        // FIND NEAREST ROUTE POINT
        // -----------------------------------------------------

        UpdateNearestRoutePoint();
    }


    // =========================================================
    // FIND NEAREST ROUTE POINT
    // =========================================================

    private void UpdateNearestRoutePoint()
    {
        RoutePoint[] points =
            routeLoader.Data.points;

        float closestDistance =
            float.MaxValue;

        int closestIndex =
            nearestPointIndex;

        for (
            int i = 0;
            i < points.Length;
            i++
        )
        {
            Vector3 routePosition =
                GeoToUnity(
                    points[i].latitude,
                    points[i].longitude
                );

            float distance =
                Vector3.Distance(
                    rider.position,
                    routePosition
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        nearestPointIndex =
            closestIndex;

        distanceTravelledKm =
            (float)(
                points[nearestPointIndex]
                .distanceFromStart
            );

        if (logProgress)
        {
            Debug.Log(
                $"VirtualRide: Route progress = " +
                $"{distanceTravelledKm:F3} km"
            );
        }
    }


    // =========================================================
    // GEO → UNITY
    // =========================================================

    private Vector3 GeoToUnity(
        double latitude,
        double longitude
    )
    {
        const double metersPerDegreeLatitude =
            111320.0;

        double metersPerDegreeLongitude =
            111320.0 *
            System.Math.Cos(
                routeLoader.transform
                    .position
                    .y *
                Mathf.Deg2Rad
            );

        double deltaLatitude =
            latitude -
            routeLoaderOriginLatitude;

        double deltaLongitude =
            longitude -
            routeLoaderOriginLongitude;

        float x =
            (float)(
                deltaLongitude *
                metersPerDegreeLongitude *
                0.01
            );

        float z =
            (float)(
                deltaLatitude *
                metersPerDegreeLatitude *
                0.01
            );

        return routeLoader.transform.position +
               new Vector3(
                   x,
                   0f,
                   z
               );
    }


    // =========================================================
    // ORIGIN
    // =========================================================

    //
    // These values match the current route origin.
    //
    // We will later move these into a shared coordinate system
    // instead of duplicating them.
    //

    private const double routeLoaderOriginLatitude =
        9.9642475;

    private const double routeLoaderOriginLongitude =
        76.2454182;


    // =========================================================
    // PUBLIC API
    // =========================================================

    public float GetDistanceTravelledKm()
    {
        return distanceTravelledKm;
    }


    public int GetNearestPointIndex()
    {
        return nearestPointIndex;
    }


    public void ResetProgress()
    {
        distanceTravelledKm = 0f;
        nearestPointIndex = 0;
    }


    public void SetRider(
        Transform newRider
    )
    {
        rider = newRider;
    }
}