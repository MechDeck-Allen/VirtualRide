using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using System.Globalization;
using TMPro;

public class BLEManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text statusText;

    [Header("Ride Data")]
    public RideData rideData;
    public BikeInput bikeInput;

    [Header("Automatic Reconnect")]
    [SerializeField] private bool autoReconnect = true;

    [SerializeField] private float reconnectStartDelay = 2f;
    [SerializeField] private float reconnectMaxDelay = 15f;
    [SerializeField] private float reconnectBackoff = 1.7f;

    [Header("Reconnect Behaviour")]
    [Tooltip("0 = unlimited reconnect attempts")]
    [SerializeField] private int maxReconnectAttempts = 0;

    private Coroutine reconnectCoroutine;

    private bool isConnected = false;
    private bool userStartedScan = false;
    private bool reconnecting = false;

    private int reconnectAttempt = 0;


    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        RequestBlePermissionsIfNeeded();

#endif

        isConnected = false;
        reconnecting = false;
        reconnectAttempt = 0;

        SetStatus("DISCONNECTED");
    }


    private void OnDestroy()
    {
        StopReconnectRoutine();
    }


    // =========================================================
    // FIND MY BIKE
    // =========================================================

    public void StartBLEScan()
    {
        Debug.Log("BLEManager: FIND MY BIKE");

        // The user explicitly started the BLE process.
        userStartedScan = true;

        reconnecting = false;
        reconnectAttempt = 0;

        StopReconnectRoutine();

        StartBleScanInternal();
    }


    // =========================================================
    // START SCAN
    // =========================================================

    private void StartBleScanInternal()
    {
        if (isConnected)
        {
            Debug.Log("BLEManager: Already connected.");

            SetStatus("CONNECTED");

            return;
        }


#if UNITY_ANDROID && !UNITY_EDITOR

        if (!HasBlePermission())
        {
            RequestBlePermissionsIfNeeded();

            SetStatus("ALLOW BLUETOOTH PERMISSION");

            return;
        }


        SetStatus(
            reconnecting
                ? "RECONNECTING..."
                : "SCANNING..."
        );


        try
        {
            using (
                AndroidJavaClass bleClass =
                new AndroidJavaClass(
                    "com.virtualride.ble.VirtualRideBleBridge"
                )
            )
            {
                bleClass.CallStatic("startScan");
            }

            Debug.Log(
                "BLEManager: Android BLE scan started."
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "BLEManager: Failed to start BLE scan.\n" +
                e
            );

            SetStatus("SCAN ERROR");

            HandleConnectionLost();
        }

#else

        Debug.Log(
            "BLEManager: BLE scanning only works on Android."
        );

        SetStatus("ANDROID ONLY");

#endif
    }


    // =========================================================
    // ANDROID VERSION
    // =========================================================

#if UNITY_ANDROID && !UNITY_EDITOR

    private int GetAndroidSdkVersion()
    {
        using (
            AndroidJavaClass versionClass =
            new AndroidJavaClass(
                "android.os.Build$VERSION"
            )
        )
        {
            return versionClass.GetStatic<int>("SDK_INT");
        }
    }


    // =========================================================
    // BLE PERMISSIONS
    // =========================================================

    private bool HasBlePermission()
    {
        int sdkVersion =
            GetAndroidSdkVersion();


        // Android 12+
        if (sdkVersion >= 31)
        {
            bool scanPermission =
                Permission.HasUserAuthorizedPermission(
                    "android.permission.BLUETOOTH_SCAN"
                );

            bool connectPermission =
                Permission.HasUserAuthorizedPermission(
                    "android.permission.BLUETOOTH_CONNECT"
                );

            return scanPermission &&
                   connectPermission;
        }


        // Android 11 and older
        return Permission.HasUserAuthorizedPermission(
            Permission.FineLocation
        );
    }


    private void RequestBlePermissionsIfNeeded()
    {
        int sdkVersion =
            GetAndroidSdkVersion();


        // Android 12+
        if (sdkVersion >= 31)
        {
            bool scanPermission =
                Permission.HasUserAuthorizedPermission(
                    "android.permission.BLUETOOTH_SCAN"
                );

            bool connectPermission =
                Permission.HasUserAuthorizedPermission(
                    "android.permission.BLUETOOTH_CONNECT"
                );


            if (!scanPermission)
            {
                Permission.RequestUserPermission(
                    "android.permission.BLUETOOTH_SCAN"
                );

                return;
            }


            if (!connectPermission)
            {
                Permission.RequestUserPermission(
                    "android.permission.BLUETOOTH_CONNECT"
                );

                return;
            }


            return;
        }


        // Android 11 and older
        if (
            !Permission.HasUserAuthorizedPermission(
                Permission.FineLocation
            )
        )
        {
            Permission.RequestUserPermission(
                Permission.FineLocation
            );
        }
    }

#endif


    // =========================================================
    // BLE STATUS CALLBACK
    // =========================================================

    public void OnBleStatus(string message)
    {
        Debug.Log(
            "BLE STATUS: " +
            message
        );

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        SetStatus(message);
    }


    // =========================================================
    // DEVICE FOUND
    // =========================================================

    public void OnBleDeviceFound(string deviceName)
    {
        Debug.Log(
            "BLE DEVICE FOUND: " +
            deviceName
        );

        SetStatus("BIKE FOUND");
    }


    // =========================================================
    // CONNECTED
    // =========================================================

    public void OnBleConnected(string deviceName)
    {
        Debug.Log(
            "BLE CONNECTED: " +
            deviceName
        );

        isConnected = true;
        reconnecting = false;

        /*
         * IMPORTANT:
         *
         * Do NOT set userStartedScan to false here.
         *
         * Keeping this true allows the reconnect system
         * to automatically recover from an unexpected
         * bike disconnection.
         */

        reconnectAttempt = 0;

        StopReconnectRoutine();

        SetStatus("CONNECTED");
    }


    // =========================================================
    // DISCONNECTED
    // =========================================================

    public void OnBleDisconnected(string deviceName)
    {
        Debug.LogWarning(
            "BLE DISCONNECTED: " +
            deviceName
        );

        HandleConnectionLost();
    }


    private void HandleConnectionLost()
    {
        isConnected = false;

        SetStatus("DISCONNECTED");


        if (!autoReconnect)
        {
            return;
        }


        /*
         * Only reconnect automatically if the user
         * previously pressed Find My Bike.
         */

        if (!userStartedScan)
        {
            return;
        }


        StartReconnectRoutine();
    }


    // =========================================================
    // AUTO RECONNECT
    // =========================================================

    private void StartReconnectRoutine()
    {
        if (reconnectCoroutine != null)
        {
            return;
        }

        reconnectCoroutine =
            StartCoroutine(
                ReconnectRoutine()
            );
    }


    private IEnumerator ReconnectRoutine()
    {
        reconnecting = true;


        while (
            autoReconnect &&
            !isConnected
        )
        {
            // ---------------------------------------------
            // Maximum attempts
            // ---------------------------------------------

            if (
                maxReconnectAttempts > 0 &&
                reconnectAttempt >=
                maxReconnectAttempts
            )
            {
                Debug.LogWarning(
                    "BLEManager: Maximum reconnect attempts reached."
                );

                reconnecting = false;

                SetStatus(
                    "CONNECTION FAILED"
                );

                break;
            }


            // ---------------------------------------------
            // Calculate delay
            // ---------------------------------------------

            float delay =
                Mathf.Min(
                    reconnectStartDelay *
                    Mathf.Pow(
                        reconnectBackoff,
                        reconnectAttempt
                    ),
                    reconnectMaxDelay
                );


            SetStatus(
                $"RECONNECTING IN {delay:F0}S"
            );


            Debug.Log(
                "BLEManager: Reconnect attempt " +
                $"{reconnectAttempt + 1} in {delay:F1}s."
            );


            yield return new WaitForSeconds(
                delay
            );


            if (isConnected)
            {
                break;
            }


            // ---------------------------------------------
            // Next attempt
            // ---------------------------------------------

            reconnectAttempt++;


            SetStatus(
                "RECONNECTING..."
            );


            StartBleScanInternal();


            /*
             * The Android bridge is responsible for:
             *
             * SCAN
             *   ↓
             * FIND VIRTUALRIDE
             *   ↓
             * CONNECT
             *   ↓
             * DISCOVER SERVICE
             *   ↓
             * ENABLE NOTIFICATIONS
             *   ↓
             * OnBleConnected()
             */


            // ---------------------------------------------
            // Give scan time to complete
            // ---------------------------------------------

            float scanWatchTime = 6f;
            float elapsed = 0f;


            while (
                elapsed < scanWatchTime &&
                !isConnected
            )
            {
                elapsed += Time.deltaTime;

                yield return null;
            }
        }


        reconnectCoroutine = null;


        if (isConnected)
        {
            reconnecting = false;
        }
    }


    private void StopReconnectRoutine()
    {
        if (reconnectCoroutine != null)
        {
            StopCoroutine(
                reconnectCoroutine
            );

            reconnectCoroutine = null;
        }

        reconnecting = false;
    }


    // =========================================================
    // ERROR
    // =========================================================

    public void OnBleError(string error)
    {
        Debug.LogError(
            "BLE ERROR: " +
            error
        );

        SetStatus("BLE ERROR");


        if (
            autoReconnect &&
            userStartedScan &&
            !isConnected
        )
        {
            StartReconnectRoutine();
        }
    }


    // =========================================================
    // SPEED
    // =========================================================

    public void OnSpeedReceived(string data)
    {
        Debug.Log(
            "BLE TELEMETRY: " +
            data
        );

        if (string.IsNullOrWhiteSpace(data))
        {
            return;
        }

        string[] parts =
            data.Trim().Split(',');

        if (parts.Length != 4)
        {
            Debug.LogWarning(
                "BLEManager: Invalid telemetry packet: " +
                data
            );

            return;
        }

        // ---------------------------------------------------------
        // Protocol version
        // ---------------------------------------------------------

        if (parts[0] != "VR1")
        {
            Debug.LogWarning(
                "BLEManager: Unsupported telemetry version: " +
                parts[0]
            );

            return;
        }


        // ---------------------------------------------------------
        // Parse values
        // VR1,SPEED,RPM,DISTANCE
        // ---------------------------------------------------------

        bool speedValid =
            float.TryParse(
                parts[1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float speedKmh
            );

        bool rpmValid =
            float.TryParse(
                parts[2],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float rpm
            );

        bool distanceValid =
            float.TryParse(
                parts[3],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float distanceKm
            );


        if (
            !speedValid ||
            !rpmValid ||
            !distanceValid
        )
        {
            Debug.LogWarning(
                "BLEManager: Failed to parse telemetry: " +
                data
            );

            return;
        }


        // ---------------------------------------------------------
        // Safety limits
        // ---------------------------------------------------------

        speedKmh =
            Mathf.Max(
                0f,
                speedKmh
            );

        rpm =
            Mathf.Max(
                0f,
                rpm
            );

        distanceKm =
            Mathf.Max(
                0f,
                distanceKm
            );


        // ---------------------------------------------------------
        // Feed BikeInput
        // ---------------------------------------------------------

        if (bikeInput != null)
        {
            bikeInput.SetExternalTelemetry(
                speedKmh,
                rpm
            );
        }


        // ---------------------------------------------------------
        // Feed RideData
        // ---------------------------------------------------------

        if (rideData != null)
        {
            rideData.SetData(
                speedKmh,
                rpm,
                distanceKm
            );
        }


        Debug.Log(
            $"RIDE TELEMETRY | " +
            $"Speed: {speedKmh:F1} km/h | " +
            $"RPM: {rpm:F0} | " +
            $"Distance: {distanceKm:F2} km"
        );
    }

    // =========================================================
    // UI STATUS
    // =========================================================

    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text =
            "BLE: " +
            message;
    }
    // =========================================================
    // RESET ESP32 RIDE SESSION
    // =========================================================

    public void ResetRideSession()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

    try
    {
        using (
            AndroidJavaClass bleClass =
            new AndroidJavaClass(
                "com.virtualride.ble.VirtualRideBleBridge"
            )
        )
        {
            bleClass.CallStatic(
                "resetRideSession"
            );
        }

        Debug.Log(
            "BLEManager: Ride reset command sent to ESP32."
        );
    }
    catch (System.Exception e)
    {
        Debug.LogError(
            "BLEManager: Failed to reset ESP32 ride session.\n" +
            e.Message
        );
    }

#else

        Debug.Log(
            "BLEManager: ESP32 ride reset is Android-only."
        );

#endif
    }


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool IsConnected()
    {
        return isConnected;
    }


    public bool IsReconnecting()
    {
        return reconnecting;
    }
}