using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RideSummaryUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Ride References")]
    public RideController rideController;
    public RideData rideData;
    public RideStatistics rideStatistics;

    [Header("Summary Root")]
    public GameObject summaryPanel;

    // Kept for compatibility with your existing scene.
    public TMP_Text summaryStatsText;

    [Header("Typography")]
    public TMP_FontAsset fontAsset;

    // =========================================================
    // PANEL
    // =========================================================

    [Header("Panel")]
    public float panelWidth = 660f;
    public float panelHeight = 500f;
    public float panelCornerRadius = 28f;

    // =========================================================
    // CALORIE ESTIMATION
    // =========================================================

    [Header("Energy Estimate")]
    [Tooltip("Used only for estimated energy calculation.")]
    public float riderMassKg = 70f;

    // =========================================================
    // COLOURS
    // =========================================================

    [Header("Colours")]

    public Color overlayColor =
        new Color(
            0.002f,
            0.004f,
            0.008f,
            0.78f
        );

    public Color panelColor =
        new Color(
            0.008f,
            0.014f,
            0.024f,
            0.985f
        );

    public Color panelBorderColor =
        new Color(
            0.13f,
            0.19f,
            0.25f,
            1f
        );

    public Color accentColor =
        new Color(
            0.08f,
            0.78f,
            1.00f,
            1f
        );

    public Color primaryTextColor =
        new Color(
            0.94f,
            0.96f,
            0.98f,
            1f
        );

    public Color secondaryTextColor =
        new Color(
            0.43f,
            0.49f,
            0.56f,
            1f
        );

    public Color buttonBackgroundColor =
        new Color(
            0.025f,
            0.055f,
            0.075f,
            1f
        );

    public Color buttonHighlightColor =
        new Color(
            0.04f,
            0.09f,
            0.115f,
            1f
        );

    public Color buttonPressedColor =
        new Color(
            0.02f,
            0.045f,
            0.06f,
            1f
        );

    public Color buttonBorderColor =
        new Color(
            0.08f,
            0.78f,
            1f,
            0.85f
        );

    // =========================================================
    // INTERNAL
    // =========================================================

    private RectTransform panelRoot;

    private GameObject mainCard;

    private TMP_Text distanceValueText;
    private TMP_Text timeValueText;
    private TMP_Text averageSpeedValueText;
    private TMP_Text maxSpeedValueText;
    private TMP_Text caloriesValueText;
    private TMP_Text calorieRateValueText;

    // THIS WAS MISSING
    private Button newRideButton;

    private bool interfaceBuilt = false;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (rideController == null)
        {
            rideController =
                FindFirstObjectByType<RideController>();
        }

        if (rideData == null)
        {
            rideData =
                FindFirstObjectByType<RideData>();
        }

        if (rideStatistics == null)
        {
            rideStatistics =
                FindFirstObjectByType<RideStatistics>();
        }

        HideSummary();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (rideController == null)
        {
            return;
        }

        if (rideController.rideFinished)
        {
            ShowSummary();
        }
        else
        {
            HideSummary();
        }
    }

    // =========================================================
    // SHOW
    // =========================================================

    private void ShowSummary()
    {
        if (summaryPanel == null)
        {
            return;
        }

        if (!interfaceBuilt)
        {
            BuildInterface();

            interfaceBuilt = true;
        }

        summaryPanel.SetActive(true);

        // -----------------------------------------------------
        // Put summary above gauges and all other UI.
        // -----------------------------------------------------

        summaryPanel.transform.SetAsLastSibling();

        Canvas canvas =
            summaryPanel.GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }

        UpdateValues();
    }

    // =========================================================
    // HIDE
    // =========================================================

    private void HideSummary()
    {
        if (
            summaryPanel != null &&
            summaryPanel.activeSelf
        )
        {
            summaryPanel.SetActive(false);
        }
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void BuildInterface()
    {
        panelRoot =
            summaryPanel.GetComponent<RectTransform>();

        if (panelRoot == null)
        {
            panelRoot =
                summaryPanel.AddComponent<
                    RectTransform
                >();
        }

        // -----------------------------------------------------
        // Full screen root.
        // -----------------------------------------------------

        panelRoot.anchorMin =
            Vector2.zero;

        panelRoot.anchorMax =
            Vector2.one;

        panelRoot.offsetMin =
            Vector2.zero;

        panelRoot.offsetMax =
            Vector2.zero;

        // -----------------------------------------------------
        // Dedicated overlay canvas.
        // -----------------------------------------------------

        Canvas canvas =
            summaryPanel.GetComponent<Canvas>();

        if (canvas == null)
        {
            canvas =
                summaryPanel.AddComponent<Canvas>();
        }

        canvas.overrideSorting =
            true;

        canvas.sortingOrder =
            100;

        if (
            summaryPanel.GetComponent<
                GraphicRaycaster
            >() == null
        )
        {
            summaryPanel.AddComponent<
                GraphicRaycaster
            >();
        }

        // -----------------------------------------------------
        // Clean previous generated UI.
        // -----------------------------------------------------

        ClearChildren();

        // -----------------------------------------------------
        // Build.
        // -----------------------------------------------------

        CreateOverlay();

        CreateMainCard();

        CreateHeader();

        CreatePrimaryStatistics();

        CreateSecondaryStatistics();

        CreateEnergySection();

        CreateNewRideButton();
    }

    // =========================================================
    // OVERLAY
    // =========================================================

    private void CreateOverlay()
    {
        GameObject overlay =
            CreateUIObject(
                "SummaryOverlay",
                panelRoot
            );

        Image image =
            overlay.AddComponent<Image>();

        image.color =
            overlayColor;

        image.raycastTarget =
            true;

        RectTransform rect =
            overlay.GetComponent<
                RectTransform
            >();

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    // =========================================================
    // MAIN CARD
    // =========================================================

    private void CreateMainCard()
    {
        mainCard =
            CreateUIObject(
                "SummaryCard",
                panelRoot
            );

        RoundedPanelGraphic graphic =
            mainCard.AddComponent<
                RoundedPanelGraphic
            >();

        graphic.width =
            panelWidth;

        graphic.height =
            panelHeight;

        graphic.cornerRadius =
            panelCornerRadius;

        graphic.color =
            panelColor;

        graphic.borderColor =
            panelBorderColor;

        graphic.borderThickness =
            1f;

        RectTransform rect =
            mainCard.GetComponent<
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
    // HEADER
    // =========================================================

    private void CreateHeader()
    {
        CreateText(
            "Title",
            "RIDE COMPLETE",
            new Vector2(
                0f,
                200f
            ),
            28f,
            FontStyles.Bold,
            primaryTextColor,
            TextAlignmentOptions.Center,
            new Vector2(
                560f,
                45f
            )
        );

        CreateText(
            "Subtitle",
            "SESSION SUMMARY",
            new Vector2(
                0f,
                168f
            ),
            11f,
            FontStyles.Normal,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            new Vector2(
                500f,
                25f
            )
        );

        CreateAccentLine(
            new Vector2(
                0f,
                143f
            ),
            250f
        );
    }

    // =========================================================
    // PRIMARY STATISTICS
    // =========================================================

    private void CreatePrimaryStatistics()
    {
        // -----------------------------------------------------
        // DISTANCE
        // -----------------------------------------------------

        CreateText(
            "DistanceLabel",
            "DISTANCE",
            new Vector2(
                -165f,
                105f
            ),
            11f,
            FontStyles.Bold,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            new Vector2(
                230f,
                25f
            )
        );

        distanceValueText =
            CreateText(
                "DistanceValue",
                "0.00 km",
                new Vector2(
                    -165f,
                    70f
                ),
                32f,
                FontStyles.Bold,
                accentColor,
                TextAlignmentOptions.Center,
                new Vector2(
                    270f,
                    45f
                )
            );

        // -----------------------------------------------------
        // SESSION TIME
        // -----------------------------------------------------

        CreateText(
            "TimeLabel",
            "SESSION TIME",
            new Vector2(
                165f,
                105f
            ),
            11f,
            FontStyles.Bold,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            new Vector2(
                230f,
                25f
            )
        );

        timeValueText =
            CreateText(
                "TimeValue",
                "00:00",
                new Vector2(
                    165f,
                    70f
                ),
                32f,
                FontStyles.Bold,
                primaryTextColor,
                TextAlignmentOptions.Center,
                new Vector2(
                    270f,
                    45f
                )
            );

        // -----------------------------------------------------
        // Divider
        // -----------------------------------------------------

        CreateDivider(
            new Vector2(
                0f,
                40f
            ),
            500f
        );
    }

    // =========================================================
    // SECONDARY STATISTICS
    // =========================================================

    private void CreateSecondaryStatistics()
    {
        // -----------------------------------------------------
        // AVERAGE
        // -----------------------------------------------------

        CreateText(
            "AverageLabel",
            "AVERAGE SPEED",
            new Vector2(
                -165f,
                5f
            ),
            11f,
            FontStyles.Bold,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            new Vector2(
                230f,
                25f
            )
        );

        averageSpeedValueText =
            CreateText(
                "AverageValue",
                "0.0 km/h",
                new Vector2(
                    -165f,
                    -28f
                ),
                23f,
                FontStyles.Bold,
                primaryTextColor,
                TextAlignmentOptions.Center,
                new Vector2(
                    240f,
                    40f
                )
            );

        // -----------------------------------------------------
        // MAXIMUM
        // -----------------------------------------------------

        CreateText(
            "MaxLabel",
            "MAX SPEED",
            new Vector2(
                165f,
                5f
            ),
            11f,
            FontStyles.Bold,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            new Vector2(
                230f,
                25f
            )
        );

        maxSpeedValueText =
            CreateText(
                "MaxValue",
                "0.0 km/h",
                new Vector2(
                    165f,
                    -28f
                ),
                23f,
                FontStyles.Bold,
                primaryTextColor,
                TextAlignmentOptions.Center,
                new Vector2(
                    240f,
                    40f
                )
            );
    }

    // =========================================================
    // ENERGY
    // =========================================================

    private void CreateEnergySection()
    {
        CreateDivider(
            new Vector2(
                0f,
                -62f
            ),
            500f
        );

        CreateText(
            "CaloriesLabel",
            "EST. ENERGY BURN",
            new Vector2(
                0f,
                -91f
            ),
            11f,
            FontStyles.Bold,
            secondaryTextColor,
            TextAlignmentOptions.Center,
            new Vector2(
                300f,
                25f
            )
        );

        caloriesValueText =
            CreateText(
                "CaloriesValue",
                "0 kcal",
                new Vector2(
                    0f,
                    -120f
                ),
                24f,
                FontStyles.Bold,
                primaryTextColor,
                TextAlignmentOptions.Center,
                new Vector2(
                    280f,
                    40f
                )
            );

        calorieRateValueText =
            CreateText(
                "CalorieRate",
                "0.0 kcal/min",
                new Vector2(
                    0f,
                    -148f
                ),
                11f,
                FontStyles.Normal,
                secondaryTextColor,
                TextAlignmentOptions.Center,
                new Vector2(
                    220f,
                    24f
                )
            );
    }

    // =========================================================
    // NEW RIDE BUTTON
    // =========================================================

    private void CreateNewRideButton()
    {
        GameObject buttonObject =
            CreateUIObject(
                "NewRideButton",
                mainCard.transform
            );

        RoundedButtonGraphic buttonGraphic =
            buttonObject.AddComponent<
                RoundedButtonGraphic
            >();

        buttonGraphic.normalColor =
            buttonBackgroundColor;

        buttonGraphic.highlightColor =
            buttonHighlightColor;

        buttonGraphic.pressedColor =
            buttonPressedColor;

        buttonGraphic.borderColor =
            buttonBorderColor;

        buttonGraphic.cornerRadius =
            24f;

        buttonGraphic.borderThickness =
            1.5f;

        newRideButton =
            buttonObject.AddComponent<Button>();

        newRideButton.targetGraphic =
            buttonGraphic;

        // -----------------------------------------------------
        // Button colour block.
        //
        // We keep the Button transition but allow the custom
        // graphic itself to control the rounded appearance.
        // -----------------------------------------------------

        ColorBlock colors =
            newRideButton.colors;

        colors.normalColor =
            Color.white;

        colors.highlightedColor =
            Color.white;

        colors.pressedColor =
            Color.white;

        colors.selectedColor =
            Color.white;

        colors.disabledColor =
            Color.white;

        colors.colorMultiplier =
            1f;

        colors.fadeDuration =
            0.08f;

        newRideButton.colors =
            colors;

        newRideButton.onClick.AddListener(
            OnNewRidePressed
        );

        RectTransform rect =
            buttonObject.GetComponent<
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
            new Vector2(
                0f,
                -195f
            );

        rect.sizeDelta =
            new Vector2(
                190f,
                48f
            );

        // -----------------------------------------------------
        // Button label
        // -----------------------------------------------------

        CreateText(
            "NewRideText",
            "NEW RIDE",
            Vector2.zero,
            14f,
            FontStyles.Bold,
            primaryTextColor,
            TextAlignmentOptions.Center,
            new Vector2(
                170f,
                40f
            ),
            buttonObject.transform
        );
    }

    // =========================================================
    // NEW RIDE ACTION
    // =========================================================

    private void OnNewRidePressed()
    {
        if (rideController == null)
        {
            return;
        }

        rideController.NewRide();

        HideSummary();
    }

    // =========================================================
    // UPDATE VALUES
    // =========================================================

    private void UpdateValues()
    {
        if (
            rideController == null ||
            rideData == null
        )
        {
            return;
        }

        // -----------------------------------------------------
        // Distance
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
        // Time
        // -----------------------------------------------------

        if (timeValueText != null)
        {
            timeValueText.text =
                FormatTime(
                    rideController.rideTime
                );
        }

        // -----------------------------------------------------
        // Statistics
        // -----------------------------------------------------

        if (rideStatistics != null)
        {
            if (
                averageSpeedValueText != null
            )
            {
                averageSpeedValueText.text =
                    rideStatistics.averageSpeed.ToString(
                        "0.0"
                    ) +
                    " km/h";
            }

            if (
                maxSpeedValueText != null
            )
            {
                maxSpeedValueText.text =
                    rideStatistics.maxSpeed.ToString(
                        "0.0"
                    ) +
                    " km/h";
            }

            // -------------------------------------------------
            // Energy estimate
            // -------------------------------------------------

            float averageSpeed =
                Mathf.Max(
                    0f,
                    rideStatistics.averageSpeed
                );

            float minutes =
                rideController.rideTime /
                60f;

            float calories =
                EstimateCalories(
                    averageSpeed,
                    minutes
                );

            if (caloriesValueText != null)
            {
                caloriesValueText.text =
                    Mathf.RoundToInt(
                        calories
                    ) +
                    " kcal";
            }

            if (
                calorieRateValueText != null
            )
            {
                float rate =
                    minutes > 0.01f
                        ? calories / minutes
                        : 0f;

                calorieRateValueText.text =
                    rate.ToString(
                        "0.0"
                    ) +
                    " kcal/min";
            }
        }
    }

    // =========================================================
    // CALORIE ESTIMATE
    // =========================================================

    private float EstimateCalories(
        float averageSpeedKmh,
        float minutes
    )
    {
        if (minutes <= 0f)
        {
            return 0f;
        }

        float met;

        if (averageSpeedKmh < 10f)
        {
            met = 4f;
        }
        else if (averageSpeedKmh < 15f)
        {
            met = 6f;
        }
        else if (averageSpeedKmh < 20f)
        {
            met = 8f;
        }
        else if (averageSpeedKmh < 25f)
        {
            met = 10f;
        }
        else
        {
            met = 12f;
        }

        float caloriesPerMinute =
            (
                met *
                3.5f *
                riderMassKg
            ) /
            200f;

        return
            caloriesPerMinute *
            minutes;
    }

    // =========================================================
    // TIME FORMAT
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
    // TEXT CREATION
    // =========================================================

    private TMP_Text CreateText(
        string objectName,
        string textValue,
        Vector2 position,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 size
    )
    {
        return CreateText(
            objectName,
            textValue,
            position,
            fontSize,
            style,
            color,
            alignment,
            size,
            mainCard.transform
        );
    }

    private TMP_Text CreateText(
        string objectName,
        string textValue,
        Vector2 position,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 size,
        Transform parent
    )
    {
        GameObject obj =
            CreateUIObject(
                objectName,
                parent
            );

        TMP_Text text =
            obj.AddComponent<
                TextMeshProUGUI
            >();

        text.text =
            textValue;

        text.fontSize =
            fontSize;

        text.fontStyle =
            style;

        text.color =
            color;

        text.alignment =
            alignment;

        // -----------------------------------------------------
        // Unity 6 / current TMP API
        // -----------------------------------------------------

        text.textWrappingMode =
            TextWrappingModes.NoWrap;

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
            size;

        return text;
    }

    // =========================================================
    // DIVIDER
    // =========================================================

    private void CreateDivider(
        Vector2 position,
        float width
    )
    {
        GameObject obj =
            CreateUIObject(
                "Divider",
                mainCard.transform
            );

        Image image =
            obj.AddComponent<Image>();

        image.color =
            new Color(
                panelBorderColor.r,
                panelBorderColor.g,
                panelBorderColor.b,
                0.60f
            );

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
                1f
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
                "AccentLine",
                mainCard.transform
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
    // CREATE UI OBJECT
    // =========================================================

    private GameObject CreateUIObject(
        string objectName,
        Transform parent
    )
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        return obj;
    }

    // =========================================================
    // CLEAR
    // =========================================================

    private void ClearChildren()
    {
        if (panelRoot == null)
        {
            return;
        }

        for (
            int i =
                panelRoot.childCount - 1;
            i >= 0;
            i--
        )
        {
            Destroy(
                panelRoot.GetChild(i).gameObject
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

        private const int CornerSegments = 12;

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

            AddRoundedShape(
                vh,
                halfWidth,
                halfHeight,
                radius,
                color
            );

            // -------------------------------------------------
            // Border
            // -------------------------------------------------

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

                AddRoundedShape(
                    vh,
                    innerHalfWidth,
                    innerHalfHeight,
                    innerRadius,
                    borderColor
                );
            }
        }

        private void AddRoundedShape(
            VertexHelper vh,
            float halfWidth,
            float halfHeight,
            float radius,
            Color shapeColor
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
                shapeColor,
                Vector2.zero
            );

            for (
                int i = 0;
                i < points.Count;
                i++
            )
            {
                vh.AddVert(
                    points[i],
                    shapeColor,
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

    // =========================================================
    // ROUNDED BUTTON GRAPHIC
    // =========================================================
    //
    // IMPORTANT:
    // MaskableGraphic does NOT have OnPointerEnter,
    // OnPointerExit, OnPointerDown or OnPointerUp methods
    // to override.
    //
    // We therefore implement the Unity event interfaces.
    //
    // =========================================================

    [RequireComponent(
        typeof(CanvasRenderer)
    )]
    public class RoundedButtonGraphic :
        MaskableGraphic,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        public float cornerRadius = 24f;

        public float borderThickness = 1.5f;

        public Color normalColor =
            Color.white;

        public Color highlightColor =
            Color.white;

        public Color pressedColor =
            Color.white;

        public Color borderColor =
            Color.white;

        private bool highlighted = false;

        private bool pressed = false;

        private const int CornerSegments = 12;

        // -----------------------------------------------------
        // POINTER EVENTS
        // -----------------------------------------------------

        public void OnPointerEnter(
            PointerEventData eventData
        )
        {
            highlighted = true;

            SetVerticesDirty();
        }

        public void OnPointerExit(
            PointerEventData eventData
        )
        {
            highlighted = false;

            pressed = false;

            SetVerticesDirty();
        }

        public void OnPointerDown(
            PointerEventData eventData
        )
        {
            pressed = true;

            SetVerticesDirty();
        }

        public void OnPointerUp(
            PointerEventData eventData
        )
        {
            pressed = false;

            SetVerticesDirty();
        }

        // -----------------------------------------------------
        // DRAW
        // -----------------------------------------------------

        protected override void OnPopulateMesh(
            VertexHelper vh
        )
        {
            vh.Clear();

            Color currentColor =
                pressed
                    ? pressedColor
                    : highlighted
                        ? highlightColor
                        : normalColor;

            Rect rect =
                GetPixelAdjustedRect();

            float radius =
                Mathf.Min(
                    cornerRadius,
                    Mathf.Min(
                        rect.width,
                        rect.height
                    ) *
                    0.5f
                );

            AddRoundedRectangle(
                vh,
                rect,
                radius,
                currentColor
            );

            // -------------------------------------------------
            // Border
            // -------------------------------------------------

            if (borderThickness > 0f)
            {
                Rect innerRect =
                    new Rect(
                        rect.xMin +
                        borderThickness,
                        rect.yMin +
                        borderThickness,
                        rect.width -
                        borderThickness *
                        2f,
                        rect.height -
                        borderThickness *
                        2f
                    );

                float innerRadius =
                    Mathf.Max(
                        0f,
                        radius -
                        borderThickness
                    );

                AddRoundedRectangle(
                    vh,
                    innerRect,
                    innerRadius,
                    borderColor
                );
            }
        }

        private void AddRoundedRectangle(
            VertexHelper vh,
            Rect rect,
            float radius,
            Color fillColor
        )
        {
            List<Vector2> points =
                new List<Vector2>();

            AddCorner(
                points,
                new Vector2(
                    rect.xMax - radius,
                    rect.yMax - radius
                ),
                radius,
                0f,
                90f
            );

            AddCorner(
                points,
                new Vector2(
                    rect.xMin + radius,
                    rect.yMax - radius
                ),
                radius,
                90f,
                180f
            );

            AddCorner(
                points,
                new Vector2(
                    rect.xMin + radius,
                    rect.yMin + radius
                ),
                radius,
                180f,
                270f
            );

            AddCorner(
                points,
                new Vector2(
                    rect.xMax - radius,
                    rect.yMin + radius
                ),
                radius,
                270f,
                360f
            );

            int start =
                vh.currentVertCount;

            vh.AddVert(
                Vector2.zero,
                fillColor,
                Vector2.zero
            );

            for (
                int i = 0;
                i < points.Count;
                i++
            )
            {
                vh.AddVert(
                    points[i],
                    fillColor,
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