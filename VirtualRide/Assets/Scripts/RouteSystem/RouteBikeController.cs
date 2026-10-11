using System.Collections.Generic;
using UnityEngine;

public class RouteBikeController : MonoBehaviour
{
    // =========================================================
    // RIDE CONTROL
    // =========================================================

    [Header("Ride Control")]
    public RideController rideController;


    // =========================================================
    // ROUTE
    // =========================================================

    [Header("Route")]
    [Tooltip("The same RouteDataLoader used by RouteWorldGenerator.")]
    public RouteDataLoader routeLoader;


    // =========================================================
    // INPUT
    // =========================================================

    [Header("Input")]
    [Tooltip("Existing BikeInput component that provides speed in km/h.")]
    public BikeInput bikeInput;


    // =========================================================
    // RIDE DATA
    // =========================================================

    [Header("Ride Data")]
    [Tooltip("Central RideData object used by the simulator HUD and statistics.")]
    public RideData rideData;


    // =========================================================
    // WORLD ALIGNMENT
    // =========================================================

    [Header("World Alignment")]
    [Tooltip("Must match OSMWorldGenerator and RouteWorldGenerator.")]
    public double originLatitude = 9.9642475;

    [Tooltip("Must match OSMWorldGenerator and RouteWorldGenerator.")]
    public double originLongitude = 76.2454182;

    [Tooltip("Must match OSMWorldGenerator and RouteWorldGenerator.")]
    public float worldScale = 0.01f;

    [Tooltip("Must match the OSM ground height.")]
    public float groundHeight = -0.02f;


    // =========================================================
    // BIKE POSITION
    // =========================================================

    [Header("Bike Position")]
    [Tooltip("Bike pivot height above the ground in real-world metres.")]
    public float bikeHeight = 1.0f;

    [Tooltip("How many route points ahead are used to determine heading.")]
    [Range(1, 10)]
    public int lookAheadPoints = 3;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Rotation")]
    [Tooltip("Higher values make the bike turn into bends more quickly.")]
    public float rotationSmoothSpeed = 8f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    public bool showDebug = true;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private RoutePoint[] routePoints;

    private readonly List<float> segmentLengths =
        new List<float>();

    private int currentSegment = 0;

    private float distanceAlongSegmentMeters = 0f;

    private bool initialized = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Initialize();
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void Initialize()
    {
        if (routeLoader == null)
        {
            Debug.LogError(
                "RouteBikeController: Route Loader is not assigned."
            );

            return;
        }

        if (routeLoader.Data == null)
        {
            routeLoader.LoadRoute();
        }

        if (routeLoader.Data == null)
        {
            Debug.LogError(
                "RouteBikeController: Route data is unavailable."
            );

            return;
        }

        routePoints =
            routeLoader.Data.points;

        if (
            routePoints == null ||
            routePoints.Length < 2
        )
        {
            Debug.LogError(
                "RouteBikeController: Route contains fewer than 2 points."
            );

            return;
        }

        BuildSegmentLengths();

        currentSegment = 0;

        distanceAlongSegmentMeters = 0f;

        initialized = true;

        UpdateBikeTransform(true);

        Debug.Log(
            "VirtualRide: Bike initialized at route start."
        );

        Debug.Log(
            "VirtualRide: Route points = " +
            routePoints.Length
        );
    }


    // =========================================================
    // BUILD SEGMENT LENGTHS
    // =========================================================

    private void BuildSegmentLengths()
    {
        segmentLengths.Clear();

        for (
            int i = 0;
            i < routePoints.Length - 1;
            i++
        )
        {
            Vector3 a =
                GeoToUnity(
                    routePoints[i].latitude,
                    routePoints[i].longitude
                );

            Vector3 b =
                GeoToUnity(
                    routePoints[i + 1].latitude,
                    routePoints[i + 1].longitude
                );

            Vector3 flatA = a;
            Vector3 flatB = b;

            flatA.y = 0f;
            flatB.y = 0f;

            float unityDistance =
                Vector3.Distance(
                    flatA,
                    flatB
                );

            float realDistance =
                unityDistance /
                worldScale;

            segmentLengths.Add(
                realDistance
            );
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (rideController != null)
        {
            if (
                !rideController.rideActive ||
                rideController.ridePaused ||
                rideController.rideFinished
            )
            {
                UpdateBikeTransform(false);
                return;
            }
        }

        if (bikeInput == null)
        {
            UpdateBikeTransform(false);
            return;
        }


        // -----------------------------------------------------
        // READ SPEED FROM CURRENT INPUT SYSTEM
        // -----------------------------------------------------

        float speedKmh =
            Mathf.Max(
                0f,
                bikeInput.GetSpeedKmh()
            );


        // -----------------------------------------------------
        // SEND SPEED INTO CENTRAL RIDE DATA
        // -----------------------------------------------------

        if (rideData != null)
        {
            rideData.SetSpeed(
                speedKmh
            );

            // Automatically start the ride
            // when the rider begins moving.
        }


        // -----------------------------------------------------
        // CONVERT SPEED
        // -----------------------------------------------------

        float speedMetersPerSecond =
            speedKmh /
            3.6f;


        // -----------------------------------------------------
        // CALCULATE MOVEMENT
        // -----------------------------------------------------

        float movementMeters =
            speedMetersPerSecond *
            Time.deltaTime;


        // -----------------------------------------------------
        // MOVE ALONG ROUTE
        // -----------------------------------------------------

        MoveAlongRoute(
            movementMeters
        );
    }


    // =========================================================
    // MOVE
    // =========================================================

    private void MoveAlongRoute(
        float meters
    )
    {
        if (meters <= 0f)
        {
            UpdateBikeTransform(false);
            return;
        }

        while (
            meters > 0f &&
            currentSegment <
            segmentLengths.Count
        )
        {
            float segmentLength =
                segmentLengths[
                    currentSegment
                ];

            if (segmentLength <= 0.001f)
            {
                currentSegment++;

                distanceAlongSegmentMeters = 0f;

                continue;
            }

            float remaining =
                segmentLength -
                distanceAlongSegmentMeters;

            if (meters < remaining)
            {
                distanceAlongSegmentMeters +=
                    meters;

                meters = 0f;
            }
            else
            {
                meters -= remaining;

                currentSegment++;

                distanceAlongSegmentMeters = 0f;

                if (
                    currentSegment >=
                    segmentLengths.Count
                )
                {
                    currentSegment =
                        segmentLengths.Count - 1;

                    distanceAlongSegmentMeters =
                        segmentLengths[
                            currentSegment
                        ];

                    break;
                }
            }
        }

        UpdateBikeTransform(false);

        /*
         * IMPORTANT:
         *
         * Route position is intentionally NOT written into
         * RideData.distanceKm here.
         *
         * Distance now comes from the physical bike through
         * BLE telemetry.
         *
         * The route controller still maintains its own
         * currentSegment and distanceAlongSegmentMeters,
         * so gameplay movement remains unchanged.
         */
    }


    // =========================================================
    // UPDATE BIKE TRANSFORM
    // =========================================================

    private void UpdateBikeTransform(
        bool instantRotation
    )
    {
        if (
            routePoints == null ||
            routePoints.Length < 2 ||
            segmentLengths.Count < 1
        )
        {
            return;
        }

        int index =
            Mathf.Clamp(
                currentSegment,
                0,
                routePoints.Length - 2
            );

        float segmentLength =
            segmentLengths[index];

        float t = 0f;

        if (segmentLength > 0.001f)
        {
            t =
                distanceAlongSegmentMeters /
                segmentLength;
        }

        t =
            Mathf.Clamp01(t);

        Vector3 start =
            GeoToUnity(
                routePoints[index].latitude,
                routePoints[index].longitude
            );

        Vector3 end =
            GeoToUnity(
                routePoints[index + 1].latitude,
                routePoints[index + 1].longitude
            );

        Vector3 position =
            Vector3.Lerp(
                start,
                end,
                t
            );


        // -----------------------------------------------------
        // KEEP BIKE ABOVE ROAD
        // -----------------------------------------------------

        position.y =
            groundHeight +
            (
                bikeHeight *
                worldScale
            );

        transform.position =
            position;


        // -----------------------------------------------------
        // LOOK AHEAD FOR HEADING
        // -----------------------------------------------------

        int lookIndex =
            Mathf.Clamp(
                index + lookAheadPoints,
                index + 1,
                routePoints.Length - 1
            );

        Vector3 lookPosition =
            GeoToUnity(
                routePoints[lookIndex].latitude,
                routePoints[lookIndex].longitude
            );

        Vector3 direction =
            lookPosition -
            position;

        direction.y = 0f;

        if (
            direction.sqrMagnitude >
            0.000001f
        )
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );

            if (instantRotation)
            {
                transform.rotation =
                    targetRotation;
            }
            else
            {
                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        1f -
                        Mathf.Exp(
                            -rotationSmoothSpeed *
                            Time.deltaTime
                        )
                    );
            }
        }
    }


    // =========================================================
    // GEO -> UNITY
    // =========================================================

    private Vector3 GeoToUnity(
        double latitude,
        double longitude
    )
    {
        const double metersPerDegreeLatitude =
            111320.0;

        double metersPerDegreeLongitude =
            metersPerDegreeLatitude *
            System.Math.Cos(
                originLatitude *
                Mathf.Deg2Rad
            );

        double deltaLatitude =
            latitude -
            originLatitude;

        double deltaLongitude =
            longitude -
            originLongitude;

        float x =
            (float)(
                deltaLongitude *
                metersPerDegreeLongitude *
                worldScale
            );

        float z =
            (float)(
                deltaLatitude *
                metersPerDegreeLatitude *
                worldScale
            );

        return new Vector3(
            x,
            groundHeight,
            z
        );
    }


    // =========================================================
    // PUBLIC API
    // =========================================================

    public float GetDistanceTravelledMeters()
    {
        if (
            segmentLengths.Count == 0
        )
        {
            return 0f;
        }

        float distance = 0f;

        int completedSegments =
            Mathf.Clamp(
                currentSegment,
                0,
                segmentLengths.Count - 1
            );

        for (
            int i = 0;
            i < completedSegments;
            i++
        )
        {
            distance +=
                segmentLengths[i];
        }

        distance +=
            distanceAlongSegmentMeters;

        return distance;
    }


    public float GetDistanceTravelledKm()
    {
        return
            GetDistanceTravelledMeters() /
            1000f;
    }


    public int GetCurrentRouteSegment()
    {
        return currentSegment;
    }


    public bool IsAtRouteEnd()
    {
        return
            currentSegment >=
            segmentLengths.Count - 1;
    }


    public void ResetRide()
    {
        if (!initialized)
        {
            return;
        }

        currentSegment = 0;

        distanceAlongSegmentMeters = 0f;

        if (rideData != null)
        {
            rideData.ResetRide();
        }

        UpdateBikeTransform(true);
    }


    // =========================================================
    // DEBUG GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (
            !showDebug ||
            routePoints == null ||
            routePoints.Length < 2
        )
        {
            return;
        }

        int index =
            Mathf.Clamp(
                currentSegment,
                0,
                routePoints.Length - 2
            );

        Vector3 current =
            GeoToUnity(
                routePoints[index].latitude,
                routePoints[index].longitude
            );

        int lookIndex =
            Mathf.Clamp(
                index + lookAheadPoints,
                index + 1,
                routePoints.Length - 1
            );

        Vector3 look =
            GeoToUnity(
                routePoints[lookIndex].latitude,
                routePoints[lookIndex].longitude
            );

        Gizmos.DrawLine(
            current,
            look
        );
    }
    // =========================================================
    // RESET TO ROUTE START
    // =========================================================

    public void ResetToRouteStart()
    {
        currentSegment = 0;
        distanceAlongSegmentMeters = 0f;

        if (
            routePoints != null &&
            routePoints.Length > 0
        )
        {
            UpdateBikeTransform(true);
        }

        Debug.Log(
            "VirtualRide: RouteBikeController reset to route start."
        );
    }
}