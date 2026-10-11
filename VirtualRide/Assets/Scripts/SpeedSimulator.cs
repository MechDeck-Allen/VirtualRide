using UnityEngine;

public class SpeedSimulator : MonoBehaviour
{
    public RideData rideData;
    public RideController rideController;

    [Header("Simulation")]
    public float currentSpeed = 0f;
    public float acceleration = 5f;
    public float maxSpeed = 30f;

    void Update()
    {
        if (rideData == null)
            return;

        // Do nothing until the ride starts
        if (rideController != null &&
            (!rideController.rideActive ||
             rideController.ridePaused))
        {
            return;
        }

        // Simulated speed
        currentSpeed += acceleration * Time.deltaTime;

        if (currentSpeed > maxSpeed)
        {
            currentSpeed = 0f;
        }

        // Simulated RPM
        float currentRPM = currentSpeed * 7.5f;

        // Distance
        float distance = rideData.distanceKm;

        distance +=
            (currentSpeed / 3600f) *
            Time.deltaTime;

        // Send data to RideData
        rideData.SetData(
            currentSpeed,
            currentRPM,
            distance
        );
    }
}