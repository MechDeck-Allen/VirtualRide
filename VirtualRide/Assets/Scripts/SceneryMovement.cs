using UnityEngine;

public class SceneryMovement : MonoBehaviour
{
    public RideData rideData;
    public RideController rideController;

    [Header("Movement")]
    public float movementMultiplier = 0.5f;
    public float acceleration = 10f;

    [Header("Recycling")]
    public float recycleZ = -20f;
    public float resetZ = 40f;

    private float visualSpeed = 0f;

    void Update()
    {
        if (rideData == null)
            return;

        // Stop scenery when ride is inactive or paused
        if (rideController != null &&
            (!rideController.rideActive ||
             rideController.ridePaused))
        {
            visualSpeed = 0f;
            return;
        }

        // Smooth visual speed
        visualSpeed = Mathf.MoveTowards(
            visualSpeed,
            rideData.speedKmh,
            acceleration * Time.deltaTime
        );

        float movement =
            visualSpeed *
            movementMultiplier *
            Time.deltaTime;

        foreach (Transform scenery in transform)
        {
            scenery.Translate(
                Vector3.back * movement,
                Space.World
            );

            if (scenery.position.z < recycleZ)
            {
                scenery.position = new Vector3(
                    scenery.position.x,
                    scenery.position.y,
                    resetZ
                );
            }
        }
    }
}