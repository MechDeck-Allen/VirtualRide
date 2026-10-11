package com.virtualride.ble;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattDescriptor;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothProfile;

import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanRecord;
import android.bluetooth.le.ScanResult;
import android.bluetooth.le.ScanSettings;

import android.content.Context;
import android.os.Handler;
import android.os.Looper;

import com.unity3d.player.UnityPlayer;

import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.UUID;


public class VirtualRideBleBridge
{
    // ============================================================
    // UNITY
    // ============================================================

    private static final String UNITY_OBJECT =
            "BLEManager";


    // ============================================================
    // DEVICE
    // ============================================================

    private static final String DEVICE_NAME =
            "VirtualRide";


    // ============================================================
    // UUIDS
    // ============================================================

    private static final UUID SERVICE_UUID =
            UUID.fromString(
                    "12345678-1234-1234-1234-1234567890AB"
            );


    private static final UUID TELEMETRY_UUID =
            UUID.fromString(
                    "12345678-1234-1234-1234-1234567890AC"
            );


    private static final UUID COMMAND_UUID =
            UUID.fromString(
                    "12345678-1234-1234-1234-1234567890AD"
            );


    private static final UUID CLIENT_CHARACTERISTIC_CONFIG =
            UUID.fromString(
                    "00002902-0000-1000-8000-00805f9b34fb"
            );


    // ============================================================
    // BLE OBJECTS
    // ============================================================

    private static BluetoothLeScanner scanner;

    private static BluetoothGatt bluetoothGatt;


    private static final Handler handler =
            new Handler(
                    Looper.getMainLooper()
            );


    // ============================================================
    // STATE
    // ============================================================

    private static boolean scanning = false;

    private static boolean connecting = false;

    private static boolean gattReady = false;


    // ============================================================
    // SCAN TIMEOUT
    // ============================================================

    private static final long SCAN_DURATION_MS =
            10000;


    // ============================================================
    // START SCAN
    // ============================================================

    public static void startScan()
    {
        Context context =
                getContext();


        if (context == null)
        {
            sendMessage(
                    "OnBleError",
                    "Unity activity unavailable"
            );

            return;
        }


        BluetoothAdapter adapter =
                BluetoothAdapter.getDefaultAdapter();


        if (adapter == null)
        {
            sendMessage(
                    "OnBleError",
                    "Bluetooth not supported"
            );

            return;
        }


        if (!adapter.isEnabled())
        {
            sendMessage(
                    "OnBleError",
                    "Bluetooth is disabled"
            );

            return;
        }


        stopScan();

        closeGatt();


        scanner =
                adapter.getBluetoothLeScanner();


        if (scanner == null)
        {
            sendMessage(
                    "OnBleError",
                    "BLE scanner unavailable"
            );

            return;
        }


        scanning = true;

        connecting = false;

        gattReady = false;


        sendMessage(
                "OnBleStatus",
                "Scanning..."
        );


        ScanSettings settings =
                new ScanSettings.Builder()
                        .setScanMode(
                                ScanSettings
                                        .SCAN_MODE_LOW_LATENCY
                        )
                        .build();


        try
        {
            scanner.startScan(
                    null,
                    settings,
                    scanCallback
            );
        }
        catch (SecurityException e)
        {
            scanning = false;

            sendMessage(
                    "OnBleError",
                    "Bluetooth scan permission denied"
            );

            return;
        }
        catch (Exception e)
        {
            scanning = false;

            sendMessage(
                    "OnBleError",
                    "BLE scan error: " +
                            e.getMessage()
            );

            return;
        }


        handler.postDelayed(
                new Runnable()
                {
                    @Override
                    public void run()
                    {
                        if (!scanning)
                        {
                            return;
                        }


                        stopScan();


                        if (!connecting &&
                                !gattReady)
                        {
                            sendMessage(
                                    "OnBleStatus",
                                    "Bike not found"
                            );
                        }
                    }
                },
                SCAN_DURATION_MS
        );
    }


    // ============================================================
    // STOP SCAN
    // ============================================================

    public static void stopScan()
    {
        if (scanner == null)
        {
            scanning = false;

            return;
        }


        try
        {
            scanner.stopScan(
                    scanCallback
            );
        }
        catch (Exception ignored)
        {
        }


        scanner = null;

        scanning = false;
    }


    // ============================================================
    // SCAN CALLBACK
    // ============================================================

    private static final ScanCallback scanCallback =
            new ScanCallback()
            {
                @Override
                public void onScanResult(
                        int callbackType,
                        ScanResult result)
                {
                    if (
                            !scanning ||
                            connecting ||
                            gattReady
                    )
                    {
                        return;
                    }


                    BluetoothDevice device =
                            result.getDevice();


                    if (device == null)
                    {
                        return;
                    }


                    String name = null;


                    try
                    {
                        name =
                                device.getName();
                    }
                    catch (SecurityException ignored)
                    {
                    }


                    ScanRecord record =
                            result.getScanRecord();


                    if (
                            (
                                    name == null ||
                                    name.trim().isEmpty()
                            )
                            &&
                            record != null
                    )
                    {
                        name =
                                record.getDeviceName();
                    }


                    boolean nameMatch =
                            DEVICE_NAME.equals(
                                    name
                            );


                    boolean serviceMatch =
                            false;


                    if (record != null)
                    {
                        List<android.os.ParcelUuid>
                                serviceUuids =
                                record.getServiceUuids();


                        if (serviceUuids != null)
                        {
                            for (
                                    android.os.ParcelUuid serviceUuid :
                                    serviceUuids
                            )
                            {
                                if (
                                        serviceUuid != null &&
                                        SERVICE_UUID.equals(
                                                serviceUuid.getUuid()
                                        )
                                )
                                {
                                    serviceMatch = true;

                                    break;
                                }
                            }
                        }
                    }


                    /*
                     * Accept the device if either:
                     *
                     * 1. name is VirtualRide
                     * OR
                     * 2. it advertises our VirtualRide service
                     */

                    if (
                            !nameMatch &&
                            !serviceMatch
                    )
                    {
                        return;
                    }


                    connecting = true;


                    stopScan();


                    sendMessage(
                            "OnBleDeviceFound",
                            DEVICE_NAME
                    );


                    connectToDevice(
                            device
                    );
                }


                @Override
                public void onScanFailed(
                        int errorCode)
                {
                    scanning = false;

                    sendMessage(
                            "OnBleError",
                            "Scan failed: " +
                                    errorCode
                    );
                }
            };


    // ============================================================
    // CONNECT
    // ============================================================

    private static void connectToDevice(
            BluetoothDevice device
    )
    {
        closeGatt();


        sendMessage(
                "OnBleStatus",
                "Connecting..."
        );


        try
        {
            bluetoothGatt =
                    device.connectGatt(
                            getContext(),
                            false,
                            gattCallback
                    );
        }
        catch (SecurityException e)
        {
            connecting = false;

            sendMessage(
                    "OnBleError",
                    "Bluetooth connect permission denied"
            );
        }
        catch (Exception e)
        {
            connecting = false;

            sendMessage(
                    "OnBleError",
                    "Connection failed: " +
                            e.getMessage()
            );
        }
    }


    // ============================================================
    // GATT CALLBACK
    // ============================================================

    private static final BluetoothGattCallback gattCallback =
            new BluetoothGattCallback()
            {
                @Override
                public void onConnectionStateChange(
                        BluetoothGatt gatt,
                        int status,
                        int newState)
                {
                    if (
                            newState ==
                                    BluetoothProfile
                                            .STATE_CONNECTED
                    )
                    {
                        sendMessage(
                                "OnBleStatus",
                                "Connected. Discovering services..."
                        );


                        try
                        {
                            if (
                                    !gatt.discoverServices()
                            )
                            {
                                connecting = false;

                                sendMessage(
                                        "OnBleError",
                                        "Service discovery could not start"
                                );

                                closeGatt();
                            }
                        }
                        catch (Exception e)
                        {
                            connecting = false;

                            sendMessage(
                                    "OnBleError",
                                    "Service discovery error"
                            );

                            closeGatt();
                        }
                    }
                    else if (
                            newState ==
                                    BluetoothProfile
                                            .STATE_DISCONNECTED
                    )
                    {
                        connecting = false;

                        gattReady = false;


                        sendMessage(
                                "OnBleDisconnected",
                                DEVICE_NAME
                        );


                        closeGatt();
                    }
                }


                // ------------------------------------------------
                // SERVICES DISCOVERED
                // ------------------------------------------------

                @Override
                public void onServicesDiscovered(
                        BluetoothGatt gatt,
                        int status)
                {
                    if (
                            status !=
                                    BluetoothGatt.GATT_SUCCESS
                    )
                    {
                        connecting = false;

                        sendMessage(
                                "OnBleError",
                                "Service discovery failed"
                        );

                        closeGatt();

                        return;
                    }


                    BluetoothGattService service =
                            gatt.getService(
                                    SERVICE_UUID
                            );


                    if (service == null)
                    {
                        connecting = false;

                        sendMessage(
                                "OnBleError",
                                "VirtualRide service not found"
                        );

                        closeGatt();

                        return;
                    }


                    BluetoothGattCharacteristic telemetry =
                            service.getCharacteristic(
                                    TELEMETRY_UUID
                            );


                    BluetoothGattCharacteristic command =
                            service.getCharacteristic(
                                    COMMAND_UUID
                            );


                    if (telemetry == null)
                    {
                        connecting = false;

                        sendMessage(
                                "OnBleError",
                                "Telemetry characteristic not found"
                        );

                        closeGatt();

                        return;
                    }


                    if (command == null)
                    {
                        connecting = false;

                        sendMessage(
                                "OnBleError",
                                "Command characteristic not found"
                        );

                        closeGatt();

                        return;
                    }


                    // Enable local notifications.
                    boolean localEnabled =
                            gatt.setCharacteristicNotification(
                                    telemetry,
                                    true
                            );


                    if (!localEnabled)
                    {
                        connecting = false;

                        sendMessage(
                                "OnBleError",
                                "Could not enable notifications"
                        );

                        closeGatt();

                        return;
                    }


                    BluetoothGattDescriptor descriptor =
                            telemetry.getDescriptor(
                                    CLIENT_CHARACTERISTIC_CONFIG
                            );


                    if (descriptor == null)
                    {
                        connecting = false;

                        sendMessage(
                                "OnBleError",
                                "Notification descriptor not found"
                        );

                        closeGatt();

                        return;
                    }


                    try
                    {
                        descriptor.setValue(
                                BluetoothGattDescriptor
                                        .ENABLE_NOTIFICATION_VALUE
                        );


                        if (
                                !gatt.writeDescriptor(
                                        descriptor
                                )
                        )
                        {
                            connecting = false;

                            sendMessage(
                                    "OnBleError",
                                    "Notification setup failed"
                            );

                            closeGatt();
                        }
                    }
                    catch (Exception e)
                    {
                        connecting = false;

                        sendMessage(
                                "OnBleError",
                                "Notification setup exception"
                        );

                        closeGatt();
                    }
                }


                // ------------------------------------------------
                // DESCRIPTOR WRITE
                // ------------------------------------------------

                @Override
                public void onDescriptorWrite(
                        BluetoothGatt gatt,
                        BluetoothGattDescriptor descriptor,
                        int status)
                {
                    if (
                            status !=
                                    BluetoothGatt.GATT_SUCCESS
                    )
                    {
                        connecting = false;

                        sendMessage(
                                "OnBleError",
                                "Notification enable failed"
                        );

                        closeGatt();

                        return;
                    }


                    /*
                     * THIS is the actual connected state.
                     *
                     * At this moment:
                     *
                     * Bluetooth connection = OK
                     * Service = OK
                     * Telemetry characteristic = OK
                     * Command characteristic = OK
                     * Notification = OK
                     */

                    connecting = false;

                    gattReady = true;


                    sendMessage(
                            "OnBleConnected",
                            DEVICE_NAME
                    );
                }


                // ------------------------------------------------
                // TELEMETRY RECEIVED
                // ------------------------------------------------

                @Override
                public void onCharacteristicChanged(
                        BluetoothGatt gatt,
                        BluetoothGattCharacteristic characteristic)
                {
                    if (
                            characteristic == null
                    )
                    {
                        return;
                    }


                    if (
                            !TELEMETRY_UUID.equals(
                                    characteristic.getUuid()
                            )
                    )
                    {
                        return;
                    }


                    byte[] data =
                            characteristic.getValue();


                    if (
                            data == null ||
                            data.length == 0
                    )
                    {
                        return;
                    }


                    String telemetry =
                            new String(
                                    data,
                                    StandardCharsets.UTF_8
                            );


                    sendMessage(
                            "OnSpeedReceived",
                            telemetry
                    );
                }
            };


    // ============================================================
    // RESET RIDE SESSION
    // ============================================================

    public static void resetRideSession()
    {
        if (
                bluetoothGatt == null ||
                !gattReady
        )
        {
            sendMessage(
                    "OnBleStatus",
                    "RESET skipped - bike not connected"
            );

            return;
        }


        BluetoothGattService service =
                bluetoothGatt.getService(
                        SERVICE_UUID
                );


        if (service == null)
        {
            sendMessage(
                    "OnBleError",
                    "RESET failed - service unavailable"
            );

            return;
        }


        BluetoothGattCharacteristic command =
                service.getCharacteristic(
                        COMMAND_UUID
                );


        if (command == null)
        {
            sendMessage(
                    "OnBleError",
                    "RESET failed - command characteristic unavailable"
            );

            return;
        }


        try
        {
            command.setValue(
                    "RESET"
            );


            boolean started =
                    bluetoothGatt.writeCharacteristic(
                            command
                    );


            if (started)
            {
                sendMessage(
                        "OnBleStatus",
                        "Ride reset sent"
                );
            }
            else
            {
                sendMessage(
                        "OnBleError",
                        "Ride reset write failed"
                );
            }
        }
        catch (SecurityException e)
        {
            sendMessage(
                    "OnBleError",
                    "Bluetooth permission denied during reset"
            );
        }
        catch (Exception e)
        {
            sendMessage(
                    "OnBleError",
                    "Ride reset exception"
            );
        }
    }


    // ============================================================
    // CONTEXT
    // ============================================================

    private static Context getContext()
    {
        return UnityPlayer.currentActivity;
    }


    // ============================================================
    // UNITY MESSAGE
    // ============================================================

    private static void sendMessage(
            String method,
            String message
    )
    {
        UnityPlayer.UnitySendMessage(
                UNITY_OBJECT,
                method,
                message
        );
    }


    // ============================================================
    // CLOSE GATT
    // ============================================================

    private static void closeGatt()
    {
        if (bluetoothGatt != null)
        {
            try
            {
                bluetoothGatt.close();
            }
            catch (Exception ignored)
            {
            }


            bluetoothGatt = null;
        }


        gattReady = false;

        connecting = false;
    }
}