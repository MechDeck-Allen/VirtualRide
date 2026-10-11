using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    [Tooltip("Distance behind the bike.")]
    public float distance = 4.0f;

    [Tooltip("Height above the bike.")]
    public float height = 2.2f;

    [Header("Look")]
    [Tooltip("How far ahead of the bike the camera looks.")]
    public float lookAhead = 3.0f;

    [Tooltip("Height of the point the camera looks at.")]
    public float lookHeight = 0.8f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.15f;
    public float rotationSmoothSpeed = 8f;

    [Header("Optional")]
    [Tooltip("Use the bike's forward direction.")]
    public bool useTargetForward = true;

    private Vector3 velocity = Vector3.zero;


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }


        // -----------------------------------------------------
        // Determine forward direction
        // -----------------------------------------------------

        Vector3 forward =
            target.forward;

        forward.y = 0f;


        if (forward.sqrMagnitude < 0.0001f)
        {
            forward =
                Vector3.forward;
        }


        forward.Normalize();


        // -----------------------------------------------------
        // Desired camera position
        // -----------------------------------------------------

        Vector3 desiredPosition;


        if (useTargetForward)
        {
            desiredPosition =
                target.position
                -
                forward * distance
                +
                Vector3.up * height;
        }
        else
        {
            desiredPosition =
                target.position
                -
                transform.forward * distance
                +
                Vector3.up * height;
        }


        // -----------------------------------------------------
        // Smooth position
        // -----------------------------------------------------

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                positionSmoothTime
            );


        // -----------------------------------------------------
        // Look target
        // -----------------------------------------------------

        Vector3 lookTarget =
            target.position
            +
            Vector3.up * lookHeight
            +
            forward * lookAhead;


        Vector3 direction =
            lookTarget -
            transform.position;


        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        Quaternion desiredRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );


        // -----------------------------------------------------
        // Smooth rotation
        // -----------------------------------------------------

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationSmoothSpeed *
                Time.deltaTime
            );
    }
}