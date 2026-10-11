using UnityEngine;

public class RideData : MonoBehaviour
{
    // =========================================================
    // LIVE RIDE DATA
    // =========================================================

    [Header("Live Ride Data")]
    public float speedKmh;
    public float rpm;
    public float distanceKm;

    // =========================================================
    // ADDITIONAL RIDE DATA
    // =========================================================

    [Header("Ride Information")]
    public float elapsedSeconds;

    [Header("Target")]
    public float targetSpeedKmh = 20f;

    // =========================================================
    // RIDE STATE
    // =========================================================

    public enum RideState
    {
        Ready,
        Riding,
        Paused,
        Finished
    }

    [Header("State")]
    public RideState state = RideState.Ready;

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (state != RideState.Riding)
        {
            return;
        }

        elapsedSeconds += Time.deltaTime;
    }

    // =========================================================
    // SET ALL DATA
    // =========================================================

    public void SetData(
        float speed,
        float newRpm,
        float newDistance
    )
    {
        speedKmh =
            Mathf.Max(
                0f,
                speed
            );

        rpm =
            Mathf.Max(
                0f,
                newRpm
            );

        distanceKm =
            Mathf.Max(
                0f,
                newDistance
            );
    }

    // =========================================================
    // INDIVIDUAL SETTERS
    // =========================================================

    public void SetSpeed(
        float speed
    )
    {
        speedKmh =
            Mathf.Max(
                0f,
                speed
            );
    }

    public void SetRpm(
        float newRpm
    )
    {
        rpm =
            Mathf.Max(
                0f,
                newRpm
            );
    }

    public void SetDistance(
        float newDistance
    )
    {
        distanceKm =
            Mathf.Max(
                0f,
                newDistance
            );
    }

    public void SetTargetSpeed(
        float newTargetSpeed
    )
    {
        targetSpeedKmh =
            Mathf.Max(
                0f,
                newTargetSpeed
            );
    }

    // =========================================================
    // RIDE CONTROL
    // =========================================================

    public void StartRide()
    {
        if (state == RideState.Finished)
        {
            return;
        }

        state =
            RideState.Riding;
    }

    public void PauseRide()
    {
        if (state == RideState.Riding)
        {
            state =
                RideState.Paused;
        }
    }

    public void ResumeRide()
    {
        if (state == RideState.Paused)
        {
            state =
                RideState.Riding;
        }
    }

    public void FinishRide()
    {
        state =
            RideState.Finished;
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetRide()
    {
        speedKmh = 0f;
        rpm = 0f;
        distanceKm = 0f;
        elapsedSeconds = 0f;

        state =
            RideState.Ready;
    }
}