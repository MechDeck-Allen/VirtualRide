using UnityEngine;

public class CadenceSimulator : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [Tooltip("Central RideData used by the simulator.")]
    public RideData rideData;

    // =========================================================
    // BIKE PARAMETERS
    // =========================================================

    [Header("Bike Parameters")]
    [Tooltip("Wheel circumference in metres.")]
    public float wheelCircumferenceMeters = 2.1f;

    [Tooltip(
        "Virtual gearing ratio used to convert wheel RPM " +
        "into crank cadence RPM."
    )]
    public float gearRatio = 2.4f;

    // =========================================================
    // CADENCE RESPONSE
    // =========================================================

    [Header("Cadence Response")]
    [Tooltip("Minimum cadence while moving.")]
    public float minimumCadenceRpm = 50f;

    [Tooltip("Maximum cadence.")]
    public float maximumCadenceRpm = 130f;

    [Tooltip("How quickly cadence follows speed.")]
    public float smoothing = 6f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    public bool showDebug = true;

    // =========================================================
    // INTERNAL
    // =========================================================

    private float currentCadenceRpm;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (rideData == null)
        {
            rideData =
                FindFirstObjectByType<RideData>();
        }

        if (rideData == null)
        {
            Debug.LogError(
                "CadenceSimulator: RideData was not found."
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (rideData == null)
        {
            return;
        }

        float speedKmh =
            Mathf.Max(
                0f,
                rideData.speedKmh
            );

        // -----------------------------------------------------
        // STOPPED
        // -----------------------------------------------------

        if (speedKmh <= 0.01f)
        {
            currentCadenceRpm =
                Mathf.MoveTowards(
                    currentCadenceRpm,
                    0f,
                    smoothing *
                    30f *
                    Time.deltaTime
                );

            rideData.SetRpm(
                currentCadenceRpm
            );

            return;
        }

        // -----------------------------------------------------
        // SPEED -> WHEEL RPM
        // -----------------------------------------------------

        float speedMetersPerSecond =
            speedKmh /
            3.6f;

        float wheelRpm =
            (
                speedMetersPerSecond /
                Mathf.Max(
                    wheelCircumferenceMeters,
                    0.01f
                )
            ) *
            60f;

        // -----------------------------------------------------
        // WHEEL RPM -> CADENCE
        // -----------------------------------------------------

        float targetCadence =
            wheelRpm *
            Mathf.Max(
                gearRatio,
                0.01f
            );

        targetCadence =
            Mathf.Clamp(
                targetCadence,
                minimumCadenceRpm,
                maximumCadenceRpm
            );

        // -----------------------------------------------------
        // SMOOTH CADENCE
        // -----------------------------------------------------

        currentCadenceRpm =
            Mathf.Lerp(
                currentCadenceRpm,
                targetCadence,
                1f -
                Mathf.Exp(
                    -smoothing *
                    Time.deltaTime
                )
            );

        // -----------------------------------------------------
        // WRITE TO CENTRAL DATA
        // -----------------------------------------------------

        rideData.SetRpm(
            currentCadenceRpm
        );

        if (showDebug)
        {
            Debug.DrawRay(
                Vector3.zero,
                Vector3.up *
                (
                    currentCadenceRpm /
                    10f
                ),
                Color.green
            );
        }
    }
}