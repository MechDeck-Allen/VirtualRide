using UnityEngine;

public class RideStatistics : MonoBehaviour
{
    public RideData rideData;
    public RideController rideController;

    public float maxSpeed = 0f;
    public float averageSpeed = 0f;

    private float speedSum = 0f;
    private int speedSamples = 0;

    void Update()
    {
        if (rideData == null || rideController == null)
            return;

        // Only record data while the ride is active
        if (!rideController.rideActive ||
            rideController.ridePaused)
            return;

        float currentSpeed = rideData.speedKmh;

        // Maximum speed
        if (currentSpeed > maxSpeed)
        {
            maxSpeed = currentSpeed;
        }

        // Average speed
        speedSum += currentSpeed;
        speedSamples++;

        if (speedSamples > 0)
        {
            averageSpeed =
                speedSum / speedSamples;
        }
    }

    public void ResetStatistics()
    {
        maxSpeed = 0f;
        averageSpeed = 0f;

        speedSum = 0f;
        speedSamples = 0;
    }
}