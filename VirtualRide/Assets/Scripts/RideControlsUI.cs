using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RideControlsUI : MonoBehaviour
{
    // =========================================================
    // RIDE CONTROLLER
    // =========================================================

    [Header("Ride Controller")]
    public RideController rideController;

    // =========================================================
    // EXISTING BUTTONS
    // =========================================================
    // These old buttons are disabled.
    // Fresh buttons are generated automatically.
    // =========================================================

    [Header("Existing Buttons - Disable")]
    public Button startButton;
    public Button pauseButton;
    public Button resumeButton;
    public Button finishButton;

    // =========================================================
    // LAYOUT
    // =========================================================

    [Header("Layout")]

    // Distance from bottom of screen.
    public float bottomMargin = 85f;

    // ---------------------------------------------------------
    // START BUTTON
    // ---------------------------------------------------------

    public float startWidth = 400f;
    public float startHeight = 88f;

    // ---------------------------------------------------------
    // PAUSE / RESUME / FINISH
    // ---------------------------------------------------------

    public float actionWidth = 270f;
    public float actionHeight = 74f;

    public float actionSpacing = 28f;

    // =========================================================
    // COLOURS
    // =========================================================
    // Neutral colours only.
    // =========================================================

    [Header("Colours")]

    public Color buttonBackground =
        new Color(
            0.015f,
            0.020f,
            0.028f,
            0.92f
        );

    public Color buttonHover =
        new Color(
            0.035f,
            0.042f,
            0.052f,
            0.95f
        );

    public Color buttonPressed =
        new Color(
            0.050f,
            0.058f,
            0.068f,
            0.98f
        );

    public Color buttonBorder =
        new Color(
            0.45f,
            0.48f,
            0.52f,
            0.65f
        );

    public Color textColor =
        new Color(
            0.94f,
            0.95f,
            0.97f,
            1f
        );

    // =========================================================
    // TYPOGRAPHY
    // =========================================================

    [Header("Typography")]

    public TMP_FontAsset fontAsset;

    public float startTextSize = 23f;

    public float actionTextSize = 19f;

    // =========================================================
    // BUTTON STYLE
    // =========================================================

    [Header("Button Style")]

    public float cornerRadius = 20f;

    public float borderThickness = 1.5f;

    // =========================================================
    // INTERNAL
    // =========================================================

    private RectTransform dock;

    private Button startRuntimeButton;
    private Button pauseRuntimeButton;
    private Button resumeRuntimeButton;
    private Button finishRuntimeButton;

    private bool built = false;

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

        // -----------------------------------------------------
        // Disable the old scene buttons.
        // -----------------------------------------------------

        DisableOldButton(startButton);
        DisableOldButton(pauseButton);
        DisableOldButton(resumeButton);
        DisableOldButton(finishButton);

        // -----------------------------------------------------
        // Build our clean controls.
        // -----------------------------------------------------

        BuildControls();

        UpdateButtonState();
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

        UpdateButtonState();
    }

    // =========================================================
    // DISABLE OLD BUTTON
    // =========================================================

    private void DisableOldButton(
        Button button
    )
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(false);
    }

    // =========================================================
    // BUILD CONTROLS
    // =========================================================

    private void BuildControls()
    {
        if (built)
        {
            return;
        }

        Canvas canvas =
            GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            canvas =
                FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError(
                "RideControlsUI: Canvas not found."
            );

            return;
        }

        // =====================================================
        // CONTROL DOCK
        // =====================================================

        GameObject dockObject =
            new GameObject(
                "VirtualRideControls",
                typeof(RectTransform)
            );

        dockObject.transform.SetParent(
            canvas.transform,
            false
        );

        dock =
            dockObject.GetComponent<RectTransform>();

        dock.anchorMin =
            new Vector2(
                0.5f,
                0f
            );

        dock.anchorMax =
            new Vector2(
                0.5f,
                0f
            );

        dock.pivot =
            new Vector2(
                0.5f,
                0f
            );

        dock.anchoredPosition =
            new Vector2(
                0f,
                bottomMargin
            );

        dock.sizeDelta =
            new Vector2(
                700f,
                120f
            );

        // =====================================================
        // START
        // =====================================================

        startRuntimeButton =
            CreateButton(
                "StartRide",
                "START RIDE",
                startWidth,
                startHeight,
                startTextSize
            );

        // =====================================================
        // PAUSE
        // =====================================================

        pauseRuntimeButton =
            CreateButton(
                "PauseRide",
                "PAUSE",
                actionWidth,
                actionHeight,
                actionTextSize
            );

        // =====================================================
        // RESUME
        // =====================================================

        resumeRuntimeButton =
            CreateButton(
                "ResumeRide",
                "RESUME",
                actionWidth,
                actionHeight,
                actionTextSize
            );

        // =====================================================
        // FINISH
        // =====================================================

        finishRuntimeButton =
            CreateButton(
                "FinishRide",
                "FINISH",
                actionWidth,
                actionHeight,
                actionTextSize
            );

        // =====================================================
        // LISTENERS
        // =====================================================

        startRuntimeButton.onClick.AddListener(
            StartRide
        );

        pauseRuntimeButton.onClick.AddListener(
            PauseRide
        );

        resumeRuntimeButton.onClick.AddListener(
            ResumeRide
        );

        finishRuntimeButton.onClick.AddListener(
            FinishRide
        );

        built = true;
    }

    // =========================================================
    // CREATE BUTTON
    // =========================================================

    private Button CreateButton(
        string objectName,
        string buttonText,
        float width,
        float height,
        float fontSize
    )
    {
        GameObject buttonObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer)
            );

        buttonObject.transform.SetParent(
            dock,
            false
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

        rect.sizeDelta =
            new Vector2(
                width,
                height
            );

        // =====================================================
        // CUSTOM VISUAL
        // =====================================================

        RideButtonGraphic graphic =
            buttonObject.AddComponent<
                RideButtonGraphic
            >();

        graphic.normalColor =
            buttonBackground;

        graphic.highlightColor =
            buttonHover;

        graphic.pressedColor =
            buttonPressed;

        graphic.borderColor =
            buttonBorder;

        graphic.cornerRadius =
            cornerRadius;

        graphic.borderThickness =
            borderThickness;

        // =====================================================
        // BUTTON
        // =====================================================

        Button button =
            buttonObject.AddComponent<Button>();

        button.targetGraphic =
            graphic;

        // =====================================================
        // LABEL
        // =====================================================

        GameObject textObject =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer)
            );

        textObject.transform.SetParent(
            buttonObject.transform,
            false
        );

        TMP_Text label =
            textObject.AddComponent<
                TextMeshProUGUI
            >();

        label.text =
            buttonText;

        label.fontSize =
            fontSize;

        label.fontStyle =
            FontStyles.Bold;

        label.alignment =
            TextAlignmentOptions.Center;

        label.color =
            textColor;

        label.textWrappingMode =
            TextWrappingModes.NoWrap;

        label.raycastTarget =
            false;

        if (fontAsset != null)
        {
            label.font =
                fontAsset;
        }

        RectTransform labelRect =
            textObject.GetComponent<
                RectTransform
            >();

        labelRect.anchorMin =
            Vector2.zero;

        labelRect.anchorMax =
            Vector2.one;

        labelRect.offsetMin =
            Vector2.zero;

        labelRect.offsetMax =
            Vector2.zero;

        return button;
    }

    // =========================================================
    // UPDATE BUTTON STATE
    // =========================================================

    private void UpdateButtonState()
    {
        if (!built)
        {
            return;
        }

        bool active =
            rideController.rideActive;

        bool paused =
            rideController.ridePaused;

        bool finished =
            rideController.rideFinished;

        // =====================================================
        // START
        // =====================================================

        bool showStart =
            !active &&
            !finished;

        startRuntimeButton.gameObject.SetActive(
            showStart
        );

        if (showStart)
        {
            SetPosition(
                startRuntimeButton,
                0f,
                0f
            );
        }

        // =====================================================
        // PAUSE
        // =====================================================

        bool showPause =
            active &&
            !paused &&
            !finished;

        pauseRuntimeButton.gameObject.SetActive(
            showPause
        );

        if (showPause)
        {
            SetPosition(
                pauseRuntimeButton,
                -(
                    actionWidth *
                    0.5f
                ) -
                (
                    actionSpacing *
                    0.5f
                ),
                0f
            );
        }

        // =====================================================
        // RESUME
        // =====================================================

        bool showResume =
            active &&
            paused &&
            !finished;

        resumeRuntimeButton.gameObject.SetActive(
            showResume
        );

        if (showResume)
        {
            SetPosition(
                resumeRuntimeButton,
                -(
                    actionWidth *
                    0.5f
                ) -
                (
                    actionSpacing *
                    0.5f
                ),
                0f
            );
        }

        // =====================================================
        // FINISH
        // =====================================================

        bool showFinish =
            active &&
            !finished;

        finishRuntimeButton.gameObject.SetActive(
            showFinish
        );

        if (showFinish)
        {
            SetPosition(
                finishRuntimeButton,
                (
                    actionWidth *
                    0.5f
                ) +
                (
                    actionSpacing *
                    0.5f
                ),
                0f
            );
        }
    }

    // =========================================================
    // POSITION
    // =========================================================

    private void SetPosition(
        Button button,
        float x,
        float y
    )
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect =
            button.GetComponent<
                RectTransform
            >();

        rect.anchoredPosition =
            new Vector2(
                x,
                y
            );
    }

    // =========================================================
    // ACTIONS
    // =========================================================

    private void StartRide()
    {
        if (rideController == null)
        {
            return;
        }

        rideController.StartRide();
    }

    private void PauseRide()
    {
        if (rideController == null)
        {
            return;
        }

        rideController.PauseRide();
    }

    private void ResumeRide()
    {
        if (rideController == null)
        {
            return;
        }

        rideController.ResumeRide();
    }

    private void FinishRide()
    {
        if (rideController == null)
        {
            return;
        }

        rideController.FinishRide();
    }

    // =========================================================
    // ROUNDED BUTTON GRAPHIC
    // =========================================================

    [RequireComponent(
        typeof(CanvasRenderer)
    )]
    private class RideButtonGraphic :
        MaskableGraphic,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        public Color normalColor =
            Color.white;

        public Color highlightColor =
            Color.white;

        public Color pressedColor =
            Color.white;

        public Color borderColor =
            Color.white;

        public float borderThickness =
            1.5f;

        public float cornerRadius =
            20f;

        private bool highlighted;
        private bool pressed;

        private const int CornerSegments =
            12;

        // =====================================================
        // POINTER ENTER
        // =====================================================

        public void OnPointerEnter(
            PointerEventData eventData
        )
        {
            highlighted =
                true;

            SetVerticesDirty();
        }

        // =====================================================
        // POINTER EXIT
        // =====================================================

        public void OnPointerExit(
            PointerEventData eventData
        )
        {
            highlighted =
                false;

            pressed =
                false;

            SetVerticesDirty();
        }

        // =====================================================
        // POINTER DOWN
        // =====================================================

        public void OnPointerDown(
            PointerEventData eventData
        )
        {
            pressed =
                true;

            SetVerticesDirty();
        }

        // =====================================================
        // POINTER UP
        // =====================================================

        public void OnPointerUp(
            PointerEventData eventData
        )
        {
            pressed =
                false;

            SetVerticesDirty();
        }

        // =====================================================
        // DRAW
        // =====================================================

        protected override void OnPopulateMesh(
            VertexHelper vh
        )
        {
            vh.Clear();

            Rect rect =
                GetPixelAdjustedRect();

            Color fill =
                pressed
                    ? pressedColor
                    : highlighted
                        ? highlightColor
                        : normalColor;

            float radius =
                Mathf.Min(
                    cornerRadius,
                    Mathf.Min(
                        rect.width,
                        rect.height
                    ) *
                    0.5f
                );

            DrawRoundedRectangle(
                vh,
                rect,
                radius,
                fill
            );

            // -------------------------------------------------
            // Border
            // -------------------------------------------------

            if (
                borderThickness >
                0f
            )
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

                DrawRoundedRectangle(
                    vh,
                    innerRect,
                    innerRadius,
                    borderColor
                );
            }
        }

        // =====================================================
        // ROUNDED RECTANGLE
        // =====================================================

        private void DrawRoundedRectangle(
            VertexHelper vh,
            Rect rect,
            float radius,
            Color fill
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

            // Center vertex.
            vh.AddVert(
                rect.center,
                fill,
                Vector2.zero
            );

            // Outer vertices.
            for (
                int i = 0;
                i < points.Count;
                i++
            )
            {
                vh.AddVert(
                    points[i],
                    fill,
                    Vector2.zero
                );
            }

            // Triangles.
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

        // =====================================================
        // CORNER
        // =====================================================

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