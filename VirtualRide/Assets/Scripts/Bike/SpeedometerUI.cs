using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeedometerUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Data")]
    public RideData rideData;

    // =========================================================
    // GAUGE RANGE
    // =========================================================

    [Header("Gauge Range")]
    public float minimumSpeed = 0f;
    public float maximumSpeed = 60f;

    // =========================================================
    // GAUGE ANGLES
    // =========================================================
    //
    // IMPORTANT:
    //
    // 0 km/h   = 225°   = bottom-left
    // 60 km/h  = -45°   = bottom-right
    //
    // This creates the automotive-style clockwise sweep.
    //
    // =========================================================

    [Header("Gauge Angles")]
    public float startAngle = 225f;

    public float endAngle = -45f;

    // =========================================================
    // TICKS
    // =========================================================

    [Header("Ticks")]
    public int majorTickCount = 13;

    public int minorTicksBetweenMajor = 4;

    public float majorTickLength = 22f;

    public float minorTickLength = 10f;

    public float majorTickWidth = 3f;

    public float minorTickWidth = 1.5f;

    public float tickRadius = 170f;

    // =========================================================
    // COLOURS
    // =========================================================

    [Header("Colours")]

    public Color backgroundColor =
        new Color(
            0.018f,
            0.025f,
            0.038f,
            0.94f
        );

    public Color outerRingColor =
        new Color(
            0.12f,
            0.17f,
            0.23f,
            1f
        );

    public Color activeColor =
        new Color(
            0.08f,
            0.78f,
            1.00f,
            1f
        );

    public Color needleColor =
        new Color(
            0.95f,
            0.08f,
            0.10f,
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
            0.42f,
            0.49f,
            0.57f,
            1f
        );

    // =========================================================
    // BACKGROUND
    // =========================================================

    [Header("Background")]
    public float backgroundRadius = 214f;

    // =========================================================
    // RINGS
    // =========================================================

    [Header("Rings")]

    public float outerRingRadius = 205f;

    public float outerRingThickness = 3f;

    public float activeArcRadius = 193f;

    public float activeArcThickness = 7f;

    // =========================================================
    // NEEDLE
    // =========================================================

    [Header("Needle")]

    public float needleLength = 145f;

    public float needleWidth = 7f;

    public float needleSmoothing = 8f;

    public float needleHubRadius = 11f;

    // =========================================================
    // TYPOGRAPHY
    // =========================================================

    [Header("Typography")]

    public TMP_FontAsset fontAsset;

    public float speedFontSize = 54f;

    public float unitFontSize = 15f;

    public float tickFontSize = 14f;

    // =========================================================
    // CENTER TEXT
    // =========================================================

    [Header("Center Text")]

    public float centerOffsetY = -10f;

    // =========================================================
    // INTERNAL
    // =========================================================

    private RectTransform root;

    private ProceduralArcGraphic outerRing;

    private ProceduralArcGraphic activeArc;

    private NeedleGraphic needleGraphic;

    private CircularGraphic needleHub;

    private TMP_Text speedText;

    private TMP_Text unitText;

    private readonly List<GameObject> generatedObjects =
        new List<GameObject>();

    private float displayedSpeed;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // Force correct automotive layout.
        // -----------------------------------------------------

        startAngle = 225f;

        endAngle = -45f;

        if (rideData == null)
        {
            rideData =
                FindFirstObjectByType<RideData>();
        }

        if (rideData == null)
        {
            Debug.LogError(
                "SpeedometerUI: RideData was not found."
            );

            return;
        }

        BuildGauge();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (rideData == null)
        {
            return;
        }

        float targetSpeed =
            Mathf.Clamp(
                rideData.speedKmh,
                minimumSpeed,
                maximumSpeed
            );

        displayedSpeed =
            Mathf.Lerp(
                displayedSpeed,
                targetSpeed,
                1f -
                Mathf.Exp(
                    -needleSmoothing *
                    Time.deltaTime
                )
            );

        UpdateGauge(
            displayedSpeed
        );
    }

    // =========================================================
    // BUILD GAUGE
    // =========================================================

    private void BuildGauge()
    {
        root =
            GetComponent<RectTransform>();

        if (root == null)
        {
            root =
                gameObject.AddComponent<RectTransform>();
        }

        ClearGauge();

        CreateCircularBackground();

        CreateOuterRing();

        CreateActiveArc();

        CreateTicks();

        CreateNumbers();

        CreateNeedle();

        CreateCenterText();
    }

    // =========================================================
    // CIRCULAR BACKGROUND
    // =========================================================

    private void CreateCircularBackground()
    {
        GameObject background =
            CreateUIObject(
                "GaugeBackground",
                root
            );

        CircularGraphic graphic =
            background.AddComponent<
                CircularGraphic
            >();

        graphic.color =
            backgroundColor;

        graphic.radius =
            backgroundRadius;

        graphic.segments =
            128;
    }

    // =========================================================
    // OUTER RING
    // =========================================================

    private void CreateOuterRing()
    {
        GameObject ring =
            CreateUIObject(
                "OuterRing",
                root
            );

        outerRing =
            ring.AddComponent<
                ProceduralArcGraphic
            >();

        outerRing.color =
            outerRingColor;

        outerRing.radius =
            outerRingRadius;

        outerRing.thickness =
            outerRingThickness;

        outerRing.startAngle =
            startAngle;

        outerRing.endAngle =
            endAngle;

        outerRing.segments =
            128;
    }

    // =========================================================
    // ACTIVE ARC
    // =========================================================

    private void CreateActiveArc()
    {
        GameObject arc =
            CreateUIObject(
                "ActiveArc",
                root
            );

        activeArc =
            arc.AddComponent<
                ProceduralArcGraphic
            >();

        activeArc.color =
            activeColor;

        activeArc.radius =
            activeArcRadius;

        activeArc.thickness =
            activeArcThickness;

        activeArc.startAngle =
            startAngle;

        activeArc.endAngle =
            startAngle;

        activeArc.segments =
            128;
    }

    // =========================================================
    // TICKS
    // =========================================================

    private void CreateTicks()
    {
        int totalIntervals =
            (
                majorTickCount - 1
            ) *
            (
                minorTicksBetweenMajor + 1
            );

        float angleStep =
            (
                endAngle -
                startAngle
            ) /
            totalIntervals;

        for (
            int i = 0;
            i <= totalIntervals;
            i++
        )
        {
            bool isMajor =
                i %
                (
                    minorTicksBetweenMajor + 1
                ) ==
                0;

            float angle =
                startAngle +
                angleStep *
                i;

            float radians =
                angle *
                Mathf.Deg2Rad;

            Vector2 direction =
                new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                );

            float length =
                isMajor
                    ? majorTickLength
                    : minorTickLength;

            float width =
                isMajor
                    ? majorTickWidth
                    : minorTickWidth;

            Vector2 center =
                direction *
                tickRadius;

            GameObject tick =
                CreateUIObject(
                    isMajor
                        ? "MajorTick"
                        : "MinorTick",
                    root
                );

            Image image =
                tick.AddComponent<Image>();

            image.raycastTarget =
                false;

            image.color =
                isMajor
                    ? primaryTextColor
                    : secondaryTextColor;

            RectTransform rect =
                tick.GetComponent<
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
                center;

            rect.sizeDelta =
                new Vector2(
                    width,
                    length
                );

            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle - 90f
                );
        }
    }

    // =========================================================
    // NUMBERS
    // =========================================================

    private void CreateNumbers()
    {
        for (
            int i = 0;
            i < majorTickCount;
            i++
        )
        {
            float t =
                i /
                (float)(
                    majorTickCount - 1
                );

            float angle =
                Mathf.Lerp(
                    startAngle,
                    endAngle,
                    t
                );

            float value =
                Mathf.Lerp(
                    minimumSpeed,
                    maximumSpeed,
                    t
                );

            CreateNumber(
                Mathf.RoundToInt(
                    value
                ),
                angle
            );
        }
    }

    private void CreateNumber(
        int value,
        float angle
    )
    {
        float radians =
            angle *
            Mathf.Deg2Rad;

        Vector2 direction =
            new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            );

        Vector2 position =
            direction *
            (
                tickRadius -
                31f
            );

        GameObject numberObject =
            CreateUIObject(
                "SpeedNumber",
                root
            );

        TMP_Text text =
            numberObject.AddComponent<
                TextMeshProUGUI
            >();

        text.text =
            value.ToString();

        text.fontSize =
            tickFontSize;

        text.alignment =
            TextAlignmentOptions.Center;

        text.color =
            primaryTextColor;

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

        rect.sizeDelta =
            new Vector2(
                50f,
                30f
            );

        rect.anchoredPosition =
            position;
    }

    // =========================================================
    // NEEDLE
    // =========================================================

    private void CreateNeedle()
    {
        GameObject needle =
            CreateUIObject(
                "Needle",
                root
            );

        needleGraphic =
            needle.AddComponent<
                NeedleGraphic
            >();

        needleGraphic.color =
            needleColor;

        needleGraphic.length =
            needleLength;

        needleGraphic.width =
            needleWidth;

        // -----------------------------------------------------
        // HUB
        // -----------------------------------------------------

        GameObject hub =
            CreateUIObject(
                "NeedleHub",
                root
            );

        needleHub =
            hub.AddComponent<
                CircularGraphic
            >();

        needleHub.color =
            needleColor;

        needleHub.radius =
            needleHubRadius;

        needleHub.segments =
            64;
    }

    // =========================================================
    // CENTER TEXT
    // =========================================================

    private void CreateCenterText()
    {
        // -----------------------------------------------------
        // SPEED VALUE
        // -----------------------------------------------------

        GameObject speedObject =
            CreateUIObject(
                "SpeedValue",
                root
            );

        speedText =
            speedObject.AddComponent<
                TextMeshProUGUI
            >();

        speedText.text =
            "0.0";

        speedText.fontSize =
            speedFontSize;

        speedText.fontStyle =
            FontStyles.Bold;

        speedText.alignment =
            TextAlignmentOptions.Center;

        speedText.color =
            primaryTextColor;

        speedText.raycastTarget =
            false;

        if (fontAsset != null)
        {
            speedText.font =
                fontAsset;
        }

        RectTransform speedRect =
            speedText.GetComponent<
                RectTransform
            >();

        speedRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        speedRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        speedRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        speedRect.sizeDelta =
            new Vector2(
                220f,
                80f
            );

        speedRect.anchoredPosition =
            new Vector2(
                0f,
                centerOffsetY
            );

        // -----------------------------------------------------
        // UNIT
        // -----------------------------------------------------

        GameObject unitObject =
            CreateUIObject(
                "SpeedUnit",
                root
            );

        unitText =
            unitObject.AddComponent<
                TextMeshProUGUI
            >();

        unitText.text =
            "KM/H";

        unitText.fontSize =
            unitFontSize;

        unitText.alignment =
            TextAlignmentOptions.Center;

        unitText.characterSpacing =
            6f;

        unitText.color =
            secondaryTextColor;

        unitText.raycastTarget =
            false;

        if (fontAsset != null)
        {
            unitText.font =
                fontAsset;
        }

        RectTransform unitRect =
            unitText.GetComponent<
                RectTransform
            >();

        unitRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        unitRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        unitRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        unitRect.sizeDelta =
            new Vector2(
                180f,
                30f
            );

        unitRect.anchoredPosition =
            new Vector2(
                0f,
                -58f
            );
    }

    // =========================================================
    // UPDATE GAUGE
    // =========================================================

    private void UpdateGauge(
        float speed
    )
    {
        if (activeArc == null)
        {
            return;
        }

        float normalized =
            Mathf.InverseLerp(
                minimumSpeed,
                maximumSpeed,
                speed
            );

        normalized =
            Mathf.Clamp01(
                normalized
            );

        float currentAngle =
            Mathf.Lerp(
                startAngle,
                endAngle,
                normalized
            );

        // -----------------------------------------------------
        // ACTIVE ARC
        // -----------------------------------------------------

        activeArc.endAngle =
            currentAngle;

        activeArc.SetVerticesDirty();

        // -----------------------------------------------------
        // NEEDLE
        // -----------------------------------------------------

        if (needleGraphic != null)
        {
            needleGraphic.angle =
                currentAngle;

            needleGraphic.SetVerticesDirty();
        }

        // -----------------------------------------------------
        // SPEED VALUE
        // -----------------------------------------------------

        if (speedText != null)
        {
            speedText.text =
                speed.ToString(
                    "0.0"
                );
        }
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

        generatedObjects.Add(
            obj
        );

        return obj;
    }

    // =========================================================
    // CLEAR
    // =========================================================

    private void ClearGauge()
    {
        for (
            int i =
                generatedObjects.Count - 1;
            i >= 0;
            i--
        )
        {
            if (
                generatedObjects[i] != null
            )
            {
                Destroy(
                    generatedObjects[i]
                );
            }
        }

        generatedObjects.Clear();
    }

    // =========================================================
    // CIRCULAR GRAPHIC
    // =========================================================

    [RequireComponent(
        typeof(CanvasRenderer)
    )]
    public class CircularGraphic :
        MaskableGraphic
    {
        public float radius = 100f;

        public int segments = 64;

        protected override void OnPopulateMesh(
            VertexHelper vh
        )
        {
            vh.Clear();

            int segmentCount =
                Mathf.Max(
                    16,
                    segments
                );

            // Center
            vh.AddVert(
                Vector2.zero,
                color,
                Vector2.zero
            );

            // Outer ring
            for (
                int i = 0;
                i <= segmentCount;
                i++
            )
            {
                float angle =
                    (
                        i /
                        (float)segmentCount
                    ) *
                    Mathf.PI *
                    2f;

                Vector2 position =
                    new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle)
                    ) *
                    radius;

                vh.AddVert(
                    position,
                    color,
                    Vector2.zero
                );
            }

            // Triangles
            for (
                int i = 0;
                i < segmentCount;
                i++
            )
            {
                vh.AddTriangle(
                    0,
                    i + 1,
                    i + 2
                );
            }
        }
    }

    // =========================================================
    // PROCEDURAL ARC
    // =========================================================

    [RequireComponent(
        typeof(CanvasRenderer)
    )]
    public class ProceduralArcGraphic :
        MaskableGraphic
    {
        public float radius = 100f;

        public float thickness = 5f;

        public float startAngle = 0f;

        public float endAngle = 90f;

        public int segments = 64;

        protected override void OnPopulateMesh(
            VertexHelper vh
        )
        {
            vh.Clear();

            // -------------------------------------------------
            // Allow both clockwise and counter-clockwise arcs.
            // -------------------------------------------------

            float angleRange =
                endAngle -
                startAngle;

            if (
                Mathf.Abs(
                    angleRange
                ) < 0.001f
            )
            {
                return;
            }

            int segmentCount =
                Mathf.Max(
                    2,
                    Mathf.CeilToInt(
                        segments *
                        (
                            Mathf.Abs(
                                angleRange
                            ) /
                            360f
                        )
                    )
                );

            float outerRadius =
                radius +
                thickness *
                0.5f;

            float innerRadius =
                radius -
                thickness *
                0.5f;

            for (
                int i = 0;
                i < segmentCount;
                i++
            )
            {
                float t0 =
                    i /
                    (float)segmentCount;

                float t1 =
                    (
                        i + 1
                    ) /
                    (float)segmentCount;

                float a0 =
                    (
                        startAngle +
                        angleRange *
                        t0
                    ) *
                    Mathf.Deg2Rad;

                float a1 =
                    (
                        startAngle +
                        angleRange *
                        t1
                    ) *
                    Mathf.Deg2Rad;

                Vector2 outer0 =
                    new Vector2(
                        Mathf.Cos(a0),
                        Mathf.Sin(a0)
                    ) *
                    outerRadius;

                Vector2 outer1 =
                    new Vector2(
                        Mathf.Cos(a1),
                        Mathf.Sin(a1)
                    ) *
                    outerRadius;

                Vector2 inner0 =
                    new Vector2(
                        Mathf.Cos(a0),
                        Mathf.Sin(a0)
                    ) *
                    innerRadius;

                Vector2 inner1 =
                    new Vector2(
                        Mathf.Cos(a1),
                        Mathf.Sin(a1)
                    ) *
                    innerRadius;

                int index =
                    vh.currentVertCount;

                vh.AddVert(
                    outer0,
                    color,
                    Vector2.zero
                );

                vh.AddVert(
                    outer1,
                    color,
                    Vector2.zero
                );

                vh.AddVert(
                    inner1,
                    color,
                    Vector2.zero
                );

                vh.AddVert(
                    inner0,
                    color,
                    Vector2.zero
                );

                vh.AddTriangle(
                    index,
                    index + 1,
                    index + 2
                );

                vh.AddTriangle(
                    index,
                    index + 2,
                    index + 3
                );
            }
        }
    }

    // =========================================================
    // NEEDLE
    // =========================================================

    [RequireComponent(
        typeof(CanvasRenderer)
    )]
    public class NeedleGraphic :
        MaskableGraphic
    {
        public float angle = 225f;

        public float length = 145f;

        public float width = 7f;

        protected override void OnPopulateMesh(
            VertexHelper vh
        )
        {
            vh.Clear();

            float radians =
                angle *
                Mathf.Deg2Rad;

            Vector2 direction =
                new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                );

            Vector2 perpendicular =
                new Vector2(
                    -direction.y,
                    direction.x
                );

            // -------------------------------------------------
            // Tail
            // -------------------------------------------------

            Vector2 tail =
                -direction *
                18f;

            // -------------------------------------------------
            // Tip
            // -------------------------------------------------

            Vector2 tip =
                direction *
                length;

            // -------------------------------------------------
            // Base
            // -------------------------------------------------

            Vector2 leftBase =
                tail +
                perpendicular *
                (
                    width *
                    0.5f
                );

            Vector2 rightBase =
                tail -
                perpendicular *
                (
                    width *
                    0.5f
                );

            // -------------------------------------------------
            // Narrow tip
            // -------------------------------------------------

            Vector2 leftTip =
                tip +
                perpendicular *
                1.8f;

            Vector2 rightTip =
                tip -
                perpendicular *
                1.8f;

            // -------------------------------------------------
            // Vertices
            // -------------------------------------------------

            vh.AddVert(
                leftBase,
                color,
                Vector2.zero
            );

            vh.AddVert(
                rightBase,
                color,
                Vector2.zero
            );

            vh.AddVert(
                rightTip,
                color,
                Vector2.zero
            );

            vh.AddVert(
                leftTip,
                color,
                Vector2.zero
            );

            // -------------------------------------------------
            // Triangles
            // -------------------------------------------------

            vh.AddTriangle(
                0,
                1,
                2
            );

            vh.AddTriangle(
                0,
                2,
                3
            );
        }
    }
}