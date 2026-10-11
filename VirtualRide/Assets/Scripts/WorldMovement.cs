using UnityEngine;

public class WorldMovement : MonoBehaviour
{
    public RideData rideData;
    public RideController rideController;

    [Header("Movement")]
    public float movementMultiplier = 0.5f;
    public float acceleration = 10f;

    [Header("Recycling")]
    public float recycleZ = -15f;
    public float resetZ = 35f;

    private float visualSpeed = 0f;

    void Update()
    {
        if (rideData == null)
            return;

        // Stop visual movement when ride is not active or paused
        if (rideController != null &&
            (!rideController.rideActive ||
             rideController.ridePaused))
        {
            visualSpeed = 0f;
            return;
        }

        visualSpeed = Mathf.MoveTowards(
            visualSpeed,
            rideData.speedKmh,
            acceleration * Time.deltaTime
        );

        float movement =
            visualSpeed *
            movementMultiplier *
            Time.deltaTime;

        transform.Translate(
            Vector3.back * movement
        );

        foreach (Transform marker in transform)
        {
            if (marker.position.z < recycleZ)
            {
                marker.position = new Vector3(
                    marker.position.x,
                    marker.position.y,
                    resetZ
                );
            }
        }
    }
}