# ESP32 Firmware

Keep the exact tested VirtualRide Arduino sketch (`.ino`), project-owned headers and firmware-specific configuration together. Do not commit compiled caches or secrets.

## Configuration record

Document the ESP32 board variant, Arduino IDE/CLI version, ESP32 board package version, BLE library/version, Hall sensor part number, GPIO wiring, verified pulses per revolution, effective wheel/roller diameter, drive ratio and a known-good serial/BLE packet. Record calibration method and results.

## Integration contract

The currently documented telemetry text format is:

```text
VR1,SPEED_KMH,RPM,DISTANCE_KM
```

Recorded BLE identifiers:

| Purpose | UUID |
|---|---|
| Service | `12345678-1234-1234-1234-1234567890AB` |
| Telemetry characteristic | `12345678-1234-1234-1234-1234567890AC` |
| Command characteristic | `12345678-1234-1234-1234-1234567890AD` |

The advertised device name has been `VirtualRide`. Verify the actual firmware against `BLEManager.cs` and `VirtualRideBleBridge.java` before making changes. The app and firmware must agree on UUIDs, field order, units and reset commands.

After recovering the exact sketch from the development archive, record the filename and tested commit. Do not describe firmware as validated until sensor pulse counting and live BLE telemetry have passed on the physical hardware.
