# Software Architecture

## Overview

VirtualRide links physical rotation telemetry to a Unity Android riding experience. Its main software areas are Android BLE transport, telemetry/state adapters, ride and route control, streamed environment chunks, and the HUD/summary UI.

## Main components

| Component | Intended responsibility |
|---|---|
| `VirtualRideBleBridge.java` | Android BLE scanning/connection, GATT characteristics, notifications and commands |
| `BLEManager.cs` | Unity-facing connection/status layer, packet parsing, reconnect handling and reset command |
| `RideData.cs` | Shared speed, RPM and physical session-distance values |
| `BikeInput.cs` | Adapts external telemetry for the virtual bike controller |
| `RouteBikeController.cs` | Advances the virtual cyclist along route points and exposes route progress |
| `WorldStreamer.cs` | Loads/unloads baked chunk prefabs around route progress |
| `RideController.cs` | Owns active/paused/finished state and elapsed ride time |
| Ride UI and summary scripts | Display gauges, connection status, ride information and end-of-ride statistics |
| OSM generator/enhancer/baker tools | Build road/environment geometry and bake runtime chunk prefabs |

Confirm each filename and responsibility against the recovered Unity project; names may differ from this documentation if scripts were renamed during development.

## Data path

```text
Wheel/roller rotation
  -> magnet + Hall sensor
  -> ESP32 pulse timing, RPM/speed estimate and distance integration
  -> BLE telemetry
  -> VirtualRideBleBridge.java
  -> BLEManager.cs
  -> RideData.cs / BikeInput.cs
  -> HUD and RouteBikeController.cs
  -> WorldStreamer.cs and baked chunk prefabs
```

## Telemetry contract

The documented text packet is:

```text
VR1,SPEED_KMH,RPM,DISTANCE_KM
```

Example only: `VR1,18.6,74.0,2.430`. Confirm field order, units, reset commands and parsing behavior in the recovered firmware and Unity code before modifying either side.

The recorded BLE identifiers are:

| Purpose | UUID |
|---|---|
| Service | `12345678-1234-1234-1234-1234567890AB` |
| Telemetry characteristic | `12345678-1234-1234-1234-1234567890AC` |
| Command characteristic | `12345678-1234-1234-1234-1234567890AD` |
| CCCD | `00002902-0000-1000-8000-00805F9B34FB` |

Treat these as recorded integration values and verify them against the actual firmware and bridge source. The advertised device name has been `VirtualRide`.

## Ride lifecycle and reset behavior

The intended New/Start Ride sequence clears session telemetry and HUD values, resets virtual route progress to the start, and reloads the initial world chunk. Finish Ride should preserve final values for the summary; avoid resetting data before an immutable summary snapshot is captured.

The physical ESP32 distance and the virtual route position serve different purposes. The recorded design has `WorldStreamer` read route progress from `RouteBikeController`, not use the HUD's displayed ESP32 distance to choose chunks.

## Known debugging item: Ride Info timer

In the captured build, the Ride Info timer stays at `00:00`. The documented expectation is that `RideController.rideTime` increments while an active ride is neither paused nor finished. Investigate the Ride Info text reference, binding and UI update path before changing ride-summary behavior. The summary display is a separate path and should be tested independently.

## Android integration notes

- The Java bridge has called `UnityPlayer.UnitySendMessage("BLEManager", ...)`; the Unity GameObject must match the name exactly if the bridge still uses this callback.
- Preserve working Android manifest permissions and Gradle templates. Compare before changing target SDK or replacing existing configuration.
- Test BLE on the actual Android device; Editor play mode is not sufficient to validate the complete BLE path.
