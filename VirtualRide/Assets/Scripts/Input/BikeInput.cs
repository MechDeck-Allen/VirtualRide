using UnityEngine;

public class BikeInput : MonoBehaviour
{
    [Header("External Bike Input")]
    [SerializeField] private float maximumSpeedKmh = 100f;

    [Header("Current Telemetry")]
    [SerializeField] private float speedKmh;
    [SerializeField] private bool pedaling;
    [SerializeField] private bool braking;

    // =========================================================
    // PUBLIC READ ACCESS
    // =========================================================

    public float GetSpeedKmh()
    {
        return speedKmh;
    }

    public bool IsPedaling()
    {
        return pedaling;
    }

    public bool IsBraking()
    {
        return braking;
    }


    // =========================================================
    // EXTERNAL SPEED INPUT
    // =========================================================

    public void SetExternalSpeed(float newSpeedKmh)
    {
        speedKmh =
            Mathf.Clamp(
                newSpeedKmh,
                0f,
                maximumSpeedKmh
            );

        UpdatePedalingState();
    }


    // =========================================================
    // EXTERNAL TELEMETRY
    // =========================================================

    public void SetExternalTelemetry(
        float newSpeedKmh,
        float rpm
    )
    {
        speedKmh =
            Mathf.Clamp(
                newSpeedKmh,
                0f,
                maximumSpeedKmh
            );

        /*
         * Pedaling is now derived from actual bike
         * telemetry rather than a keyboard key.
         *
         * Any RPM above zero means the rider is
         * producing pedal movement.
         */
        pedaling =
            rpm > 0.5f;

        braking = false;
    }


    // =========================================================
    // PEDAL STATE
    // =========================================================

    private void UpdatePedalingState()
    {
        /*
         * Speed alone cannot reliably determine whether
         * the rider is pedaling.
         *
         * This method is retained for compatibility with
         * the existing movement system.
         *
         * The real telemetry path will use
         * SetExternalTelemetry(speed, rpm).
         */
        pedaling =
            speedKmh > 0.05f;

        braking = false;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetInput()
    {
        speedKmh = 0f;
        pedaling = false;
        braking = false;
    }
}