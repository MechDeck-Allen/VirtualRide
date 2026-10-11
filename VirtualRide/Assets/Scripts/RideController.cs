using UnityEngine;

public class RideController : MonoBehaviour
{
    public RideData rideData;
    public RideStatistics rideStatistics;

    public RouteBikeController routeBikeController;
    public WorldStreamer worldStreamer;
    public BLEManager bleManager;

    public bool rideActive = false;
    public bool ridePaused = false;
    public bool rideFinished = false;

    public float rideTime = 0f;


    // =========================================================
    // UNITY STARTUP
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // Keep the phone screen awake while VirtualRide
        // is running.
        // -----------------------------------------------------

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (
            !rideActive ||
            ridePaused ||
            rideFinished
        )
        {
            return;
        }

        rideTime += Time.deltaTime;
    }


    // =========================================================
    // START RIDE
    // =========================================================

    public void StartRide()
    {
        ResetRideSession();

        rideActive = true;

        ridePaused = false;

        rideFinished = false;

        rideTime = 0f;


        if (rideStatistics != null)
        {
            rideStatistics.ResetStatistics();
        }


        Debug.Log(
            "RIDE STARTED"
        );
    }


    // =========================================================
    // PAUSE
    // =========================================================

    public void PauseRide()
    {
        if (
            !rideActive ||
            rideFinished
        )
        {
            return;
        }

        ridePaused = true;

        Debug.Log(
            "RIDE PAUSED"
        );
    }


    // =========================================================
    // RESUME
    // =========================================================

    public void ResumeRide()
    {
        if (
            !rideActive ||
            rideFinished
        )
        {
            return;
        }

        ridePaused = false;

        Debug.Log(
            "RIDE RESUMED"
        );
    }


    // =========================================================
    // FINISH
    // =========================================================

    public void FinishRide()
    {
        if (!rideActive)
        {
            return;
        }

        /*
         * IMPORTANT:
         *
         * Do NOT reset ride data here.
         *
         * The final ride values must remain available
         * for the summary UI.
         */

        rideActive = false;

        ridePaused = false;

        rideFinished = true;


        Debug.Log(
            "RIDE FINISHED"
        );
    }


    // =========================================================
    // RESET COMPLETE RIDE SESSION
    // =========================================================

    private void ResetRideSession()
    {
        // -----------------------------------------------------
        // Reset ESP32 physical/session telemetry.
        // -----------------------------------------------------

        if (bleManager != null)
        {
            bleManager.ResetRideSession();
        }


        // -----------------------------------------------------
        // Reset Unity HUD data.
        // -----------------------------------------------------

        if (rideData != null)
        {
            rideData.SetData(
                0f,
                0f,
                0f
            );
        }


        // -----------------------------------------------------
        // Reset route progression.
        // -----------------------------------------------------

        if (routeBikeController != null)
        {
            routeBikeController.ResetToRouteStart();
        }


        // -----------------------------------------------------
        // Reset streamed world.
        // -----------------------------------------------------

        if (worldStreamer != null)
        {
            worldStreamer.ResetToRouteStart();
        }


        Debug.Log(
            "VirtualRide: Complete ride session reset."
        );
    }


    // =========================================================
    // NEW RIDE
    // =========================================================

    public void NewRide()
    {
        rideActive = false;

        ridePaused = false;

        rideFinished = false;

        rideTime = 0f;


        if (rideStatistics != null)
        {
            rideStatistics.ResetStatistics();
        }


        ResetRideSession();


        Debug.Log(
            "NEW RIDE"
        );
    }
}