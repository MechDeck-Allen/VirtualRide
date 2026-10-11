using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BLEStatusLayout : MonoBehaviour
{
    [Header("BLE Manager")]
    [SerializeField] private BLEManager bleManager;

    [Header("Panel Layout")]
    [SerializeField] private float panelWidth = 300f;
    [SerializeField] private float panelHeight = 210f;
    [SerializeField] private float rightMargin = 24f;
    [SerializeField] private float topMargin = 24f;

    [Header("Rounded Panel")]
    [SerializeField] private int cornerRadius = 22;

    [Header("Font Sizes")]
    [Range(25f, 31f)]
    [SerializeField] private float headerFontSize = 28f;

    [Range(25f, 31f)]
    [SerializeField] private float statusFontSize = 26f;

    [Range(25f, 31f)]
    [SerializeField] private float buttonFontSize = 26f;

    [Header("Button")]
    [SerializeField] private float buttonWidth = 250f;
    [SerializeField] private float buttonHeight = 62f;

    [Header("Panel Color")]
    [SerializeField]
    private Color panelColor =
        new Color(
            0.008f,
            0.012f,
            0.018f,
            0.96f
        );

    private RectTransform panelRect;
    private Image panelImage;

    private TMP_Text headerText;
    private TMP_Text statusText;

    private Image statusDot;

    private Button findMyBikeButton;
    private TMP_Text buttonText;

    private bool runtimeInitialized = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CacheReferences();

        if (Application.isPlaying)
        {
            InitializeRuntime();
        }
    }


    private void OnEnable()
    {
        CacheReferences();

        if (Application.isPlaying)
        {
            InitializeRuntime();
        }
    }


    private void OnDisable()
    {
        if (Application.isPlaying &&
            findMyBikeButton != null)
        {
            findMyBikeButton.onClick.RemoveListener(
                HandleFindMyBike
            );
        }

        runtimeInitialized = false;
    }


    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (bleManager == null)
        {
            bleManager =
                FindFirstObjectByType<BLEManager>();
        }

        if (bleManager == null)
        {
            return;
        }

        UpdateBLEVisuals();
    }


    // =========================================================
    // CACHE REFERENCES
    // =========================================================

    private void CacheReferences()
    {
        panelRect =
            GetComponent<RectTransform>();

        panelImage =
            GetComponent<Image>();

        Transform headerTransform =
            transform.Find("BLEHeader");

        if (headerTransform != null)
        {
            headerText =
                headerTransform.GetComponent<TMP_Text>();
        }

        Transform statusTransform =
            transform.Find("BLEStatus");

        if (statusTransform != null)
        {
            statusText =
                statusTransform.GetComponent<TMP_Text>();
        }

        Transform dotTransform =
            transform.Find("BLEStatusDot");

        if (dotTransform != null)
        {
            statusDot =
                dotTransform.GetComponent<Image>();
        }

        Transform buttonTransform =
            transform.Find("FindMyBikeButton");

        if (buttonTransform != null)
        {
            findMyBikeButton =
                buttonTransform.GetComponent<Button>();

            buttonText =
                buttonTransform.GetComponentInChildren<TMP_Text>();
        }
    }


    // =========================================================
    // RUNTIME INITIALIZATION
    // =========================================================

    private void InitializeRuntime()
    {
        if (runtimeInitialized)
        {
            return;
        }

        CacheReferences();
        // Recreate the rounded panel background at runtime.
        if (panelImage != null)
        {
            panelImage.sprite =
                CreateRoundedSprite(
                    128,
                    cornerRadius
                );

            panelImage.type =
                Image.Type.Sliced;

            panelImage.color =
                panelColor;
        }

        if (bleManager == null)
        {
            bleManager =
                FindFirstObjectByType<BLEManager>();
        }

        if (findMyBikeButton != null)
        {
            findMyBikeButton.onClick.RemoveListener(
                HandleFindMyBike
            );

            findMyBikeButton.onClick.AddListener(
                HandleFindMyBike
            );
        }

        /*
         * IMPORTANT:
         *
         * Do not read the old text value here.
         *
         * BLEManager starts with isConnected = false.
         * Therefore the UI must start disconnected.
         */

        SetDisconnectedVisual();

        runtimeInitialized = true;
    }


    // =========================================================
    // EDITOR BUILD
    // =========================================================

#if UNITY_EDITOR

    [ContextMenu("Build BLE Panel")]
    public void BuildPanel()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning(
                "BLEStatusLayout: Build BLE Panel should be used in Edit Mode."
            );

            return;
        }

        if (bleManager == null)
        {
            bleManager =
                FindFirstObjectByType<BLEManager>();
        }

        FindOrCreatePanel();
        FindOrCreateHeader();
        FindOrCreateStatus();
        FindOrCreateStatusDot();
        FindOrCreateButton();

        ConfigurePanel();
        ConfigureHeader();
        ConfigureStatus();
        ConfigureStatusDot();
        ConfigureButton();

        EditorUtility.SetDirty(gameObject);

        Debug.Log(
            "BLEStatusLayout: Panel built successfully."
        );
    }

#endif


    // =========================================================
    // PANEL
    // =========================================================

    private void FindOrCreatePanel()
    {
        panelRect =
            GetComponent<RectTransform>();

        if (panelRect == null)
        {
            panelRect =
                gameObject.AddComponent<RectTransform>();
        }

        panelImage =
            GetComponent<Image>();

        if (panelImage == null)
        {
            panelImage =
                gameObject.AddComponent<Image>();
        }
    }


    // =========================================================
    // HEADER
    // =========================================================

    private void FindOrCreateHeader()
    {
        Transform existing =
            transform.Find("BLEHeader");

        if (existing == null)
        {
            GameObject obj =
                new GameObject("BLEHeader");

            obj.transform.SetParent(
                transform,
                false
            );

            headerText =
                obj.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            headerText =
                existing.GetComponent<TMP_Text>();

            if (headerText == null)
            {
                headerText =
                    existing.gameObject.AddComponent<
                        TextMeshProUGUI
                    >();
            }
        }
    }


    // =========================================================
    // STATUS
    // =========================================================

    private void FindOrCreateStatus()
    {
        Transform existing =
            transform.Find("BLEStatus");

        if (existing == null)
        {
            GameObject obj =
                new GameObject("BLEStatus");

            obj.transform.SetParent(
                transform,
                false
            );

            statusText =
                obj.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            statusText =
                existing.GetComponent<TMP_Text>();

            if (statusText == null)
            {
                statusText =
                    existing.gameObject.AddComponent<
                        TextMeshProUGUI
                    >();
            }
        }
    }


    // =========================================================
    // STATUS DOT
    // =========================================================

    private void FindOrCreateStatusDot()
    {
        Transform existing =
            transform.Find("BLEStatusDot");

        if (existing == null)
        {
            GameObject obj =
                new GameObject("BLEStatusDot");

            obj.transform.SetParent(
                transform,
                false
            );

            statusDot =
                obj.AddComponent<Image>();
        }
        else
        {
            statusDot =
                existing.GetComponent<Image>();

            if (statusDot == null)
            {
                statusDot =
                    existing.gameObject.AddComponent<Image>();
            }
        }
    }


    // =========================================================
    // FIND MY BIKE BUTTON
    // =========================================================

    private void FindOrCreateButton()
    {
        Transform existing =
            transform.Find("FindMyBikeButton");

        if (existing == null)
        {
            GameObject obj =
                new GameObject("FindMyBikeButton");

            obj.transform.SetParent(
                transform,
                false
            );

            Image buttonImage =
                obj.AddComponent<Image>();

            findMyBikeButton =
                obj.AddComponent<Button>();

            findMyBikeButton.targetGraphic =
                buttonImage;

            GameObject textObject =
                new GameObject("Text");

            textObject.transform.SetParent(
                obj.transform,
                false
            );

            buttonText =
                textObject.AddComponent<
                    TextMeshProUGUI
                >();
        }
        else
        {
            findMyBikeButton =
                existing.GetComponent<Button>();

            if (findMyBikeButton == null)
            {
                findMyBikeButton =
                    existing.gameObject.AddComponent<Button>();
            }

            buttonText =
                existing.GetComponentInChildren<TMP_Text>();

            if (buttonText == null)
            {
                GameObject textObject =
                    new GameObject("Text");

                textObject.transform.SetParent(
                    existing,
                    false
                );

                buttonText =
                    textObject.AddComponent<
                        TextMeshProUGUI
                    >();
            }

            Image buttonImage =
                existing.GetComponent<Image>();

            if (buttonImage == null)
            {
                buttonImage =
                    existing.gameObject.AddComponent<Image>();
            }

            findMyBikeButton.targetGraphic =
                buttonImage;
        }
    }


    // =========================================================
    // PANEL CONFIGURATION
    // =========================================================

    private void ConfigurePanel()
    {
        panelRect.anchorMin =
            new Vector2(1f, 1f);

        panelRect.anchorMax =
            new Vector2(1f, 1f);

        panelRect.pivot =
            new Vector2(1f, 1f);

        panelRect.anchoredPosition =
            new Vector2(
                -rightMargin,
                -topMargin
            );

        panelRect.sizeDelta =
            new Vector2(
                panelWidth,
                panelHeight
            );

        panelImage.color =
            panelColor;

        panelImage.sprite =
            CreateRoundedSprite(
                128,
                cornerRadius
            );

        panelImage.type =
            Image.Type.Sliced;

        panelImage.raycastTarget =
            false;
    }


    // =========================================================
    // HEADER CONFIGURATION
    // =========================================================

    private void ConfigureHeader()
    {
        RectTransform rect =
            headerText.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.offsetMin =
            new Vector2(
                22f,
                -42f
            );

        rect.offsetMax =
            new Vector2(
                -22f,
                -12f
            );

        headerText.text =
            "BIKE CONNECTION";

        headerText.fontSize =
            Mathf.Clamp(
                headerFontSize,
                25f,
                31f
            );

        headerText.fontStyle =
            FontStyles.Bold;

        headerText.alignment =
            TextAlignmentOptions.Left;

        headerText.textWrappingMode =
            TextWrappingModes.NoWrap;

        headerText.color =
            Color.white;

        headerText.raycastTarget =
            false;
    }


    // =========================================================
    // STATUS CONFIGURATION
    // =========================================================

    private void ConfigureStatus()
    {
        RectTransform rect =
            statusText.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.offsetMin =
            new Vector2(
                55f,
                -94f
            );

        rect.offsetMax =
            new Vector2(
                -20f,
                -54f
            );

        statusText.text =
            "BLE: DISCONNECTED";

        statusText.fontSize =
            Mathf.Clamp(
                statusFontSize,
                25f,
                31f
            );

        statusText.fontStyle =
            FontStyles.Bold;

        statusText.alignment =
            TextAlignmentOptions.Left;

        statusText.textWrappingMode =
            TextWrappingModes.NoWrap;

        statusText.color =
            new Color(
                0.65f,
                0.68f,
                0.73f,
                1f
            );

        statusText.raycastTarget =
            false;
    }


    // =========================================================
    // DOT CONFIGURATION
    // =========================================================

    private void ConfigureStatusDot()
    {
        RectTransform rect =
            statusDot.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            new Vector2(
                30f,
                -74f
            );

        rect.sizeDelta =
            new Vector2(
                16f,
                16f
            );

        statusDot.sprite =
            CreateCircleSprite(64);

        statusDot.color =
            new Color(
                0.65f,
                0.68f,
                0.73f,
                1f
            );

        statusDot.raycastTarget =
            false;
    }


    // =========================================================
    // BUTTON CONFIGURATION
    // =========================================================

    private void ConfigureButton()
    {
        RectTransform buttonRect =
            findMyBikeButton.GetComponent<RectTransform>();

        buttonRect.anchorMin =
            new Vector2(
                0.5f,
                0f
            );

        buttonRect.anchorMax =
            new Vector2(
                0.5f,
                0f
            );

        buttonRect.pivot =
            new Vector2(
                0.5f,
                0f
            );

        buttonRect.anchoredPosition =
            new Vector2(
                0f,
                18f
            );

        buttonRect.sizeDelta =
            new Vector2(
                buttonWidth,
                buttonHeight
            );

        Image image =
            findMyBikeButton.GetComponent<Image>();

        if (image == null)
        {
            image =
                findMyBikeButton.gameObject.AddComponent<Image>();
        }

        image.sprite =
            CreateRoundedSprite(
                96,
                12
            );

        image.type =
            Image.Type.Sliced;

        image.color =
            new Color(
                0.018f,
                0.024f,
                0.034f,
                1f
            );

        ColorBlock colors =
            findMyBikeButton.colors;

        colors.normalColor =
            new Color(
                0.018f,
                0.024f,
                0.034f,
                1f
            );

        colors.highlightedColor =
            new Color(
                0.040f,
                0.048f,
                0.060f,
                1f
            );

        colors.pressedColor =
            new Color(
                0.065f,
                0.073f,
                0.085f,
                1f
            );

        colors.selectedColor =
            colors.normalColor;

        colors.disabledColor =
            new Color(
                0.018f,
                0.024f,
                0.034f,
                0.45f
            );

        colors.fadeDuration =
            0.08f;

        findMyBikeButton.colors =
            colors;

        RectTransform textRect =
            buttonText.GetComponent<RectTransform>();

        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;

        buttonText.text =
            "FIND MY BIKE";

        buttonText.fontSize =
            Mathf.Clamp(
                buttonFontSize,
                25f,
                31f
            );

        buttonText.fontStyle =
            FontStyles.Bold;

        buttonText.alignment =
            TextAlignmentOptions.Center;

        buttonText.textWrappingMode =
            TextWrappingModes.NoWrap;

        buttonText.color =
            Color.white;

        buttonText.raycastTarget =
            false;
    }


    // =========================================================
    // FIND MY BIKE
    // =========================================================

    private void HandleFindMyBike()
    {
        if (bleManager == null)
        {
            bleManager =
                FindFirstObjectByType<BLEManager>();
        }

        if (bleManager == null)
        {
            Debug.LogError(
                "BLEStatusLayout: BLEManager not found."
            );

            return;
        }

        /*
         * Visual feedback happens immediately.
         * BLEManager then becomes the source of truth.
         */

        SetScanningVisual();

        bleManager.StartBLEScan();
    }


    // =========================================================
    // BLE STATE
    // =========================================================

    private void UpdateBLEVisuals()
    {
        if (bleManager == null)
        {
            return;
        }


        /*
         * IMPORTANT:
         *
         * We now use the ACTUAL BLE state from BLEManager,
         * not the text currently displayed on screen.
         *
         * This prevents an old saved "CONNECTED" string
         * from making the UI appear connected at startup.
         */


        if (bleManager.IsConnected())
        {
            SetConnectedVisual();

            return;
        }


        if (bleManager.IsReconnecting())
        {
            SetReconnectingVisual();

            return;
        }


        /*
         * Android/Editor status messages such as:
         *
         * SCANNING...
         * BIKE FOUND
         * CONNECTING...
         * ANDROID ONLY
         *
         * come through BLEManager.statusText.
         *
         * These are display states, not connection state.
         */

        if (bleManager.statusText != null)
        {
            string source =
                bleManager.statusText.text;

            if (
                source.Contains("SCANNING")
            )
            {
                SetScanningVisual();

                return;
            }

            if (
                source.Contains("BIKE FOUND")
            )
            {
                SetScanningVisual();

                return;
            }

            if (
                source.Contains("CONNECTING")
            )
            {
                SetScanningVisual();

                return;
            }

            if (
                source.Contains("ERROR")
            )
            {
                SetErrorVisual();

                return;
            }
        }


        SetDisconnectedVisual();
    }


    // =========================================================
    // VISUAL STATES
    // =========================================================

    public void SetDisconnectedVisual()
    {
        SetState(
            "BLE: DISCONNECTED",
            new Color(
                0.65f,
                0.68f,
                0.73f,
                1f
            )
        );
    }


    public void SetScanningVisual()
    {
        SetState(
            "BLE: SCANNING...",
            new Color(
                0.85f,
                0.65f,
                0.25f,
                1f
            )
        );
    }


    public void SetReconnectingVisual()
    {
        SetState(
            "BLE: RECONNECTING...",
            new Color(
                0.85f,
                0.65f,
                0.25f,
                1f
            )
        );
    }


    public void SetConnectedVisual()
    {
        SetState(
            "BLE: CONNECTED",
            new Color(
                0.30f,
                0.85f,
                0.45f,
                1f
            )
        );
    }


    public void SetErrorVisual()
    {
        SetState(
            "BLE: ERROR",
            new Color(
                0.90f,
                0.35f,
                0.35f,
                1f
            )
        );
    }


    private void SetState(
        string message,
        Color stateColor
    )
    {
        if (statusText != null)
        {
            statusText.text =
                message;

            statusText.color =
                stateColor;
        }

        if (statusDot != null)
        {
            statusDot.color =
                stateColor;
        }
    }


    // =========================================================
    // ROUNDED RECTANGLE SPRITE
    // =========================================================

    private Sprite CreateRoundedSprite(
        int textureSize,
        int radius
    )
    {
        Texture2D texture =
            new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false
            );

        texture.hideFlags =
            HideFlags.HideAndDontSave;

        for (
            int y = 0;
            y < textureSize;
            y++
        )
        {
            for (
                int x = 0;
                x < textureSize;
                x++
            )
            {
                bool inside =
                    IsInsideRoundedRect(
                        x,
                        y,
                        textureSize,
                        textureSize,
                        radius
                    );

                texture.SetPixel(
                    x,
                    y,
                    inside
                        ? Color.white
                        : new Color(
                            1f,
                            1f,
                            1f,
                            0f
                        )
                );
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                textureSize,
                textureSize
            ),
            new Vector2(
                0.5f,
                0.5f
            ),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(
                radius,
                radius,
                radius,
                radius
            )
        );
    }


    private bool IsInsideRoundedRect(
        int x,
        int y,
        int width,
        int height,
        float radius
    )
    {
        float px =
            x + 0.5f;

        float py =
            y + 0.5f;

        float left =
            radius;

        float right =
            width - radius;

        float bottom =
            radius;

        float top =
            height - radius;


        if (
            px >= left &&
            px <= right
        )
        {
            return true;
        }


        if (
            py >= bottom &&
            py <= top
        )
        {
            return true;
        }


        float cx =
            px < left
                ? left
                : right;

        float cy =
            py < bottom
                ? bottom
                : top;

        float dx =
            px - cx;

        float dy =
            py - cy;

        return
            dx * dx +
            dy * dy <=
            radius * radius;
    }


    // =========================================================
    // CIRCLE SPRITE
    // =========================================================

    private Sprite CreateCircleSprite(
        int size
    )
    {
        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

        texture.hideFlags =
            HideFlags.HideAndDontSave;

        float center =
            size * 0.5f;

        float radius =
            size * 0.5f;


        for (
            int y = 0;
            y < size;
            y++
        )
        {
            for (
                int x = 0;
                x < size;
                x++
            )
            {
                float dx =
                    x + 0.5f -
                    center;

                float dy =
                    y + 0.5f -
                    center;

                bool inside =
                    dx * dx +
                    dy * dy <=
                    radius * radius;

                texture.SetPixel(
                    x,
                    y,
                    inside
                        ? Color.white
                        : new Color(
                            1f,
                            1f,
                            1f,
                            0f
                        )
                );
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                size,
                size
            ),
            new Vector2(
                0.5f,
                0.5f
            ),
            100f
        );
    }
}