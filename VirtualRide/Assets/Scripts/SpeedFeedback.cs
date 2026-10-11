using UnityEngine;
using TMPro;

public class SpeedFeedback : MonoBehaviour
{
    public RideData rideData;
    public TargetSpeed targetSpeed;

    public TMP_Text feedbackText;

    public float tolerance = 1f;

    void Update()
    {
        if (rideData == null || targetSpeed == null)
            return;

        float actualSpeed = rideData.speedKmh;
        float target = targetSpeed.GetTargetSpeed();

        float difference = actualSpeed - target;

        if (Mathf.Abs(difference) <= tolerance)
        {
            feedbackText.text = "✓ ON TARGET";
        }
        else if (difference < 0)
        {
            feedbackText.text = "↑ SPEED UP";
        }
        else
        {
            feedbackText.text = "↓ SLOW DOWN";
        }
    }
}