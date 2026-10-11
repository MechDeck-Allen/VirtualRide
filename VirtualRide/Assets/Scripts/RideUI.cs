using UnityEngine;
using TMPro;

public class RideUI : MonoBehaviour
{
    public RideData rideData;
    public RideController rideController;
    public TargetSpeed targetSpeed;

    public TMP_Text speedText;
    public TMP_Text rpmText;
    public TMP_Text distanceText;
    public TMP_Text targetSpeedText;
    public TMP_Text sessionTimerText;

    private void Start()
    {
        // Keep the existing references working.
        if (rideController == null)
        {
            rideController =
                FindFirstObjectByType<RideController>();
        }

        // If Session Timer Text was not assigned manually,
        // look for the object created for the timer.
        if (sessionTimerText == null)
        {
            TMP_Text[] texts =
                GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text.gameObject.name == "SessionTimeText")
                {
                    sessionTimerText = text;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        // SESSION TIMER
        // This is the same timer used by RideSummaryUI.
        if (sessionTimerText != null &&
            rideController != null)
        {
            int totalSeconds =
                Mathf.FloorToInt(
                    rideController.rideTime
                );

            int minutes =
                (totalSeconds % 3600) / 60;

            int seconds =
                totalSeconds % 60;

            sessionTimerText.text =
                minutes.ToString("00") +
                ":" +
                seconds.ToString("00");
        }

        // Existing Ride Data UI
        if (rideData == null)
            return;

        speedText.text =
            rideData.speedKmh.ToString("0.0");

        rpmText.text =
            "RPM " + rideData.rpm.ToString("0");

        distanceText.text =
            "DISTANCE " +
            rideData.distanceKm.ToString("0.00") +
            " km";

        if (targetSpeed != null)
        {
            targetSpeedText.text =
                "TARGET " +
                targetSpeed.GetTargetSpeed().ToString("0.0") +
                " km/h";
        }
    }
}