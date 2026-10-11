using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RideInfoPanel : MonoBehaviour
{
    // =========================================================
    // DATA
    // =========================================================

    [Header("Data")]
    public RideData rideData;

    public RideController rideController;

    // =========================================================
    // PANEL
    // =========================================================

    [Header("Panel")]
    public float panelWidth = 300f;
    public float panelHeight = 500f;

    public float cornerRadius = 28f;

    // =========================================================
    // COLOURS
    // =========================================================

    [Header("Colours")]

    public Color panelColor =
        new Color(
            0.018f,
            0.026f,
            0.040f,
            0.94f
        );

    public Color borderColor =
        new Color(
            0.14f,
            0.20f,
            0.27f,
            1f
        );

    public Color accentColor =
        new Color(
            0.08f,
            0.78f,
            1.00f,
            1f
        );

    public Color targetColor =
        new Color(
            1.00f,
            0.66f,
            0.22f,
            1f
        );

    public Color warningColor =
        new Color(
            1.00f,
            0.20f,
            0.18f,
            1f
        );

    public Color primaryTextColor =
        new Color(
            0.95f,
            0.97f,
            1.00f,
            1f
        );

    public Color secondaryTextColor =
        new Color(
            0.46f,
            0.53f,
            0.61f,
            1f
        );

    // =========================================================
    // TYPOGRAPHY
    // =========================================================

    [Header("Typography")]
    public TMP_FontAsset fontAsset;

    public float titleFontSize = 17f;
    public float labelFontSize = 13f;
    public float valueFontSize = 27f;
    public float feedbackFontSize = 18f;

    // =========================================================
    // FEEDBACK
    // =========================================================

    [Header("Target Feedback")]

    [Tooltip(
        "How far from target speed before the panel " +
        "shows SPEED UP or SLOW DOWN."
    )]
    public float targetToleranceKmh = 1.0f;

    // =========================================================
    // INTERNAL
    // =========================================================

    private RectTransform root;

    private TMP_Text targetValueText;
    private TMP_Text feedbackText;
    private TMP_Text distanceValueText;
    private TMP_Text sessionValueText;

    private bool built = false;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (rideData == null)
        {
            rideData =
                FindFirstObjectByType<RideData>();
        }

        if (rideData == null)
        {
            Debug.LogError(
                "RideInfoPanel: RideData was not found."
            );

            return;
        }

        // Use the same ride timer that drives the
        // working Ride Summary session time.
        if (rideController == null)
        {
            rideController =
                FindFirstObjectByType<RideController>();
        }

        if (rideController == null)
        {
            Debug.LogError(
                "RideInfoPanel: RideController was not found."
            );
        }

        BuildPanel();

        built = true;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (
            !built ||
            rideData == null
        )
        {
            return;
        }

        UpdatePanel();
    }

    // =========================================================
    // BUILD PANEL
    // =========================================================

    private void BuildPanel()
    {
        root =
            GetComponent<RectTransform>();

        if (root == null)
        {
            root =
                gameObject.AddComponent<RectTransform>();
        }

        ClearChildren();

        // -----------------------------------------------------
        // PANEL BACKGROUND
        // -----------------------------------------------------

        CreateRoundedPanel();

        // -----------------------------------------------------
        // TITLE
        // -----------------------------------------------------

        CreateText(
            "RideDataTitle",
            "RIDE DATA",
            new Vector2(
                0f,
                205f
            ),
            titleFontSize,
            primaryTextColor,
            TextAlignmentOptions.Center,
            out _
        );

        // -----------------------------------------------------
        // TOP ACCENT LINE
        // -----------------------------------------------------

        CreateAccentLine(
            new Vector2(
                0f,
                177f
            ),
            190f
        );

        // -----------------------------------------------------
        // TARGET
        // -----------------------------------------------------

        CreateText(
            "TargetLabel",
            "TARGET SPEED",
            new Vector2(
                0f,
                135f
            ),
            labelFontSize,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            out _
        );

        CreateText(
            "TargetValue",
            "20.0 km/h",
            new Vector2(
                0f,
                102f
            ),
            valueFontSize,
            targetColor,
            TextAlignmentOptions.Center,
            out targetValueText
        );

        // -----------------------------------------------------
        // TARGET DIVIDER
        // -----------------------------------------------------

        CreateDivider(
            new Vector2(
                0f,
                65f
            )
        );

        // -----------------------------------------------------
        // FEEDBACK
        // -----------------------------------------------------

        CreateText(
            "Feedback",
            "SPEED UP",
            new Vector2(
                0f,
                28f
            ),
            feedbackFontSize,
            accentColor,
            TextAlignmentOptions.Center,
            out feedbackText
        );

        // -----------------------------------------------------
        // FEEDBACK SUBTEXT
        // -----------------------------------------------------

        CreateText(
            "FeedbackHint",
            "Maintain your target pace",
            new Vector2(
                0f,
                -7f
            ),
            11f,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            out _
        );

        // -----------------------------------------------------
        // SECOND DIVIDER
        // -----------------------------------------------------

        CreateDivider(
            new Vector2(
                0f,
                -45f
            )
        );

        // -----------------------------------------------------
        // DISTANCE
        // -----------------------------------------------------

        CreateText(
            "DistanceLabel",
            "DISTANCE",
            new Vector2(
                0f,
                -82f
            ),
            labelFontSize,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            out _
        );

        CreateText(
            "DistanceValue",
            "0.00 km",
            new Vector2(
                0f,
                -112f
            ),
            valueFontSize - 4f,
            primaryTextColor,
            TextAlignmentOptions.Center,
            out distanceValueText
        );

        // -----------------------------------------------------
        // THIRD DIVIDER
        // -----------------------------------------------------

        CreateDivider(
            new Vector2(
                0f,
                -150f
            )
        );

        // -----------------------------------------------------
        // SESSION
        // -----------------------------------------------------

        CreateText(
            "SessionLabel",
            "SESSION TIME",
            new Vector2(
                0f,
                -185f
            ),
            labelFontSize,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            out _
        );

        CreateText(
            "SessionValue",
            "00:00",
            new Vector2(
                0f,
                -215f
            ),
            valueFontSize - 4f,
            primaryTextColor,
            TextAlignmentOptions.Center,
            out sessionValueText
        );
    }

    // =========================================================
    // UPDATE PANEL
    // =========================================================

    private void UpdatePanel()
    {
        float speed =
            Mathf.Max(
                0f,
                rideData.speedKmh
            );

        float target =
            Mathf.Max(
                0f,
                rideData.targetSpeedKmh
            );

        // -----------------------------------------------------
        // TARGET
        // -----------------------------------------------------

        if (targetValueText != null)
        {
            targetValueText.text =
                target.ToString(
                    "0.0"
                ) +
                " km/h";
        }

        // -----------------------------------------------------
        // FEEDBACK
        // -----------------------------------------------------

        float difference =
            speed -
            target;

        if (
            Mathf.Abs(
                difference
            ) <= targetToleranceKmh
        )
        {
            feedbackText.text =
                "ON TARGET";

            feedbackText.color =
                targetColor;
        }
        else if (
            difference < 0f
        )
        {
            feedbackText.text =
                "SPEED UP";

            feedbackText.color =
                accentColor;
        }
        else
        {
            feedbackText.text =
                "SLOW DOWN";

            feedbackText.color =
                warningColor;
        }

        // -----------------------------------------------------
        // DISTANCE
        // -----------------------------------------------------

        if (distanceValueText != null)
        {
            distanceValueText.text =
                rideData.distanceKm.ToString(
                    "0.00"
                ) +
                " km";
        }

        // -----------------------------------------------------
        // SESSION TIME
        // -----------------------------------------------------

        if (
            sessionValueText != null &&
            rideController != null
        )
        {
            sessionValueText.text =
                FormatTime(
                    rideController.rideTime
                );
        }
    }

    // =========================================================
    // FORMAT TIME
    // =========================================================

    private string FormatTime(
        float seconds
    )
    {
        seconds =
            Mathf.Max(
                0f,
                seconds
            );

        int totalSeconds =
            Mathf.FloorToInt(
                seconds
            );

        int hours =
            totalSeconds /
            3600;

        int minutes =
            (
                totalSeconds %
                3600
            ) /
            60;

        int remainingSeconds =
            totalSeconds %
            60;

        if (hours > 0)
        {
            return string.Format(
                "{0:00}:{1:00}:{2:00}",
                hours,
                minutes,
                remainingSeconds
            );
        }

        return string.Format(
            "{0:00}:{1:00}",
            minutes,
            remainingSeconds
        );
    }

    // =========================================================
    // ROUNDED PANEL
    // =========================================================

    private void CreateRoundedPanel()
    {
        GameObject panel =
            CreateUIObject(
                "RoundedBackground"
            );

        RoundedPanelGraphic graphic =
            panel.AddComponent<
                RoundedPanelGraphic
            >();

        graphic.color =
            panelColor;

        graphic.width =
            panelWidth;

        graphic.height =
            panelHeight;

        graphic.cornerRadius =
            cornerRadius;

        graphic.borderColor =
            borderColor;

        graphic.borderThickness =
            2f;

        RectTransform rect =
            panel.GetComponent<
                RectTransform
            >();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            Vector2.zero;

        rect.sizeDelta =
            new Vector2(
                panelWidth,
                panelHeight
            );
    }

    // =========================================================
    // ACCENT LINE
    // =========================================================

    private void CreateAccentLine(
        Vector2 position,
        float width
    )
    {
        GameObject obj =
            CreateUIObject(
                "AccentLine"
            );

        Image image =
            obj.AddComponent<Image>();

        image.color =
            accentColor;

        image.raycastTarget =
            false;

        RectTransform rect =
            obj.GetComponent<
                RectTransform
            >();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            new Vector2(
                width,
                2f
            );
    }

    // =========================================================
    // DIVIDER
    // =========================================================

    private void CreateDivider(
        Vector2 position
    )
    {
        GameObject obj =
            CreateUIObject(
                "Divider"
            );

        Image image =
            obj.AddComponent<Image>();

        image.color =
            borderColor;

        image.raycastTarget =
            false;

        RectTransform rect =
            obj.GetComponent<
                RectTransform
            >();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            new Vector2(
                190f,
                1f
            );
    }

    // =========================================================
    // TEXT
    // =========================================================

    private void CreateText(
        string objectName,
        string textValue,
        Vector2 position,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        out TMP_Text textComponent
    )
    {
        GameObject obj =
            CreateUIObject(
                objectName
            );

        TMP_Text text =
            obj.AddComponent<
                TextMeshProUGUI
            >();

        text.text =
            textValue;

        text.fontSize =
            fontSize;

        text.alignment =
            alignment;

        text.color =
            color;

        text.raycastTarget =
            false;

        if (fontAsset != null)
        {
            text.font =
                fontAsset;
        }

        RectTransform rect =
            text.GetComponent<
                RectTransform
            >();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            new Vector2(
                240f,
                40f
            );

        textComponent =
            text;
    }

    // =========================================================
    // CREATE UI OBJECT
    // =========================================================

    private GameObject CreateUIObject(
        string objectName
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer)
            );

        obj.transform.SetParent(
            root,
            false
        );

        return obj;
    }

    // =========================================================
    // CLEAR
    // =========================================================

    private void ClearChildren()
    {
        for (
            int i =
                root.childCount - 1;
            i >= 0;
            i--
        )
        {
            Destroy(
                root.GetChild(i).gameObject
            );
        }
    }

    // =========================================================
    // ROUNDED PANEL GRAPHIC
    // =========================================================

    [RequireComponent(
        typeof(CanvasRenderer)
    )]
    public class RoundedPanelGraphic :
        MaskableGraphic
    {
        public float width = 300f;
        public float height = 500f;

        public float cornerRadius = 28f;

        public Color borderColor =
            Color.white;

        public float borderThickness = 2f;

        private const int CornerSegments = 10;

        protected override void OnPopulateMesh(
            VertexHelper vh
        )
        {
            vh.Clear();

            float halfWidth =
                width *
                0.5f;

            float halfHeight =
                height *
                0.5f;

            float radius =
                Mathf.Clamp(
                    cornerRadius,
                    0f,
                    Mathf.Min(
                        halfWidth,
                        halfHeight
                    )
                );

            AddRoundedLayer(
                vh,
                halfWidth,
                halfHeight,
                radius,
                color,
                0f
            );

            if (borderThickness > 0f)
            {
                float innerHalfWidth =
                    Mathf.Max(
                        0f,
                        halfWidth -
                        borderThickness
                    );

                float innerHalfHeight =
                    Mathf.Max(
                        0f,
                        halfHeight -
                        borderThickness
                    );

                float innerRadius =
                    Mathf.Max(
                        0f,
                        radius -
                        borderThickness
                    );

                AddRoundedLayer(
                    vh,
                    innerHalfWidth,
                    innerHalfHeight,
                    innerRadius,
                    borderColor,
                    borderThickness
                );
            }
        }

        private void AddRoundedLayer(
            VertexHelper vh,
            float halfWidth,
            float halfHeight,
            float radius,
            Color layerColor,
            float inwardOffset
        )
        {
            List<Vector2> points =
                new List<Vector2>();

            AddCorner(
                points,
                new Vector2(
                    halfWidth - radius,
                    halfHeight - radius
                ),
                radius,
                0f,
                90f
            );

            AddCorner(
                points,
                new Vector2(
                    -halfWidth + radius,
                    halfHeight - radius
                ),
                radius,
                90f,
                180f
            );

            AddCorner(
                points,
                new Vector2(
                    -halfWidth + radius,
                    -halfHeight + radius
                ),
                radius,
                180f,
                270f
            );

            AddCorner(
                points,
                new Vector2(
                    halfWidth - radius,
                    -halfHeight + radius
                ),
                radius,
                270f,
                360f
            );

            int start =
                vh.currentVertCount;

            vh.AddVert(
                Vector2.zero,
                layerColor,
                Vector2.zero
            );

            foreach (
                Vector2 point in points
            )
            {
                vh.AddVert(
                    point,
                    layerColor,
                    Vector2.zero
                );
            }

            for (
                int i = 0;
                i < points.Count;
                i++
            )
            {
                int next =
                    (
                        i + 1
                    ) %
                    points.Count;

                vh.AddTriangle(
                    start,
                    start + i + 1,
                    start + next + 1
                );
            }
        }

        private void AddCorner(
            List<Vector2> points,
            Vector2 center,
            float radius,
            float startDegrees,
            float endDegrees
        )
        {
            for (
                int i = 0;
                i <= CornerSegments;
                i++
            )
            {
                float t =
                    i /
                    (float)CornerSegments;

                float angle =
                    Mathf.Lerp(
                        startDegrees,
                        endDegrees,
                        t
                    ) *
                    Mathf.Deg2Rad;

                points.Add(
                    center +
                    new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle)
                    ) *
                    radius
                );
            }
        }
    }
}