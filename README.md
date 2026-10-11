# VirtualRide — Indoor Cycling Simulator

VirtualRide is a Unity-based indoor cycling simulator connecting a physical roller prototype to a 3D route environment. The prototype combines an ESP32, magnetic rotation sensing, Bluetooth Low Energy (BLE), and an Android app displaying ride telemetry.

> **Project status:** Prototype documentation and reference photographs are committed. The photographed app shows BLE disconnected and the Ride Info session timer at `00:00`; both remain known validation/debugging items. The full Unity source, firmware, GPX route, and processed OSM data must also be recovered from the preserved development archive and versioned before this repository can be considered a complete reproducible backup.

## Project at a glance

- **Engine:** Unity 6, Android landscape
- **Test device:** Samsung Galaxy A50, Android 11
- **Controller:** ESP32 development board
- **Telemetry:** BLE
- **Rotation sensing:** Magnetic/Hall-effect pulse-sensing prototype; final wiring and calibration must be verified against hardware
- **Environment:** Route-based 3D world using baked OSM-derived road/environment chunks
- **Route described in project history:** Approximately 67.8 km, Kochi toward Alappuzha/Nedumudy; verify against the source GPX
- **Primary scene:** `MainRide`
- **Additional/staging scene:** `RideWorld`

## Screenshots and prototype record

### Android app UI

![VirtualRide Android app UI](docs/images/virtualride-mobile-ui.jpg)

The captured landscape screen shows a road scene, route-following cyclist, speed and RPM gauges, Start Ride control, BLE status, and Ride Info. At capture time the UI showed `BIKE DISCONNECTED`, zero speed/distance, a target speed of 20 km/h, and `SESSION TIME 00:00`. This is a UI baseline, not proof of a successful live BLE session.

### Mechanical roller frame

![VirtualRide roller trainer frame](docs/images/roller-trainer-frame.jpg)

The frame uses extrusion profiles, rollers, and support brackets. Record measured dimensions, roller spacing, shaft and bearing details, and the wheel/roller coupling before attempting a faithful rebuild.

### ESP32 and power electronics

![ESP32 and power electronics enclosure](docs/images/esp32-power-electronics-enclosure.jpg)

The enclosure photograph shows an ESP32 board, perfboard, and an adjustable DC-DC converter. The exact module and voltage settings are not confirmed by the photo; measure them before documenting final wiring.

### Magnetic rotation test rig

![Rotation test rig top view](docs/images/magnetic-rotation-test-rig-top.jpg)

![Rotation test rig front view](docs/images/magnetic-rotation-test-rig-front.jpg)

These photos show a motor, rotating disc/pulley with visible markers, and a discrete-component test circuit. They are build records, not circuit diagrams. Verify the sensor type, magnet count, actual pulse count per revolution, component values, and pin mapping.

## Documentation index

- [Hardware and mechanical prototype](docs/HARDWARE.md)
- [Software architecture](docs/SOFTWARE_ARCHITECTURE.md)
- [Git workflow, large assets, and backups](docs/GIT_WORKFLOW.md)
- [Test plan](docs/TEST_PLAN.md)
- [Resource inventory and recovery checklist](docs/RESOURCE_INVENTORY.md)
- [Workspace-level OSM data layout](world_data/README.md)
- [Route source data](route_data/README.md)
- [ESP32 firmware integration requirements](firmware/esp32/README.md)
- [CAD source files](hardware/cad/README.md)
- [Wiring and schematics](hardware/schematics/README.md)

## System architecture

```text
Mechanical roller / rotating wheel
              |
              v
      Magnet(s) + Hall sensor
              |
              v
          ESP32 firmware
       pulse timing / RPM
       estimated speed
       session distance
       BLE telemetry + RESET
              |
              v
       Bluetooth Low Energy
              |
              v
 VirtualRideBleBridge.java (Android)
              |
              v
           BLEManager.cs
          /             \
         v               v
   BikeInput.cs       RideData.cs
         |               |
         v               +--> HUD, gauges, statistics, summary
 RouteBikeController.cs
         |
         v
 WorldStreamer.cs --> baked world chunk prefabs
```

### Telemetry packet

The currently documented text packet is:

```text
VR1,SPEED_KMH,RPM,DISTANCE_KM
```

Example format only: `VR1,18.6,74.0,2.430`. `VR1` identifies the packet version; numeric fields use decimal points. Confirm parser and firmware behavior against the recovered source.

### BLE identifiers recorded by the project

| Purpose | UUID |
|---|---|
| Service | `12345678-1234-1234-1234-1234567890AB` |
| Telemetry characteristic | `12345678-1234-1234-1234-1234567890AC` |
| Command characteristic | `12345678-1234-1234-1234-1234567890AD` |
| Notification descriptor (CCCD) | `00002902-0000-1000-8000-00805F9B34FB` |

The prototype uses the advertised device name `VirtualRide`. Verify these values against firmware and Android bridge sources; the firmware and app must agree.

## Ride lifecycle and state

The documented responsibilities are:

- `RideData`: shared HUD telemetry for speed, RPM, and ESP32 session distance.
- `BikeInput`: adapter exposing external speed/RPM to the virtual bike controller.
- `RouteBikeController`: advances along route points and tracks virtual route progress.
- `WorldStreamer`: loads/unloads baked chunks around the virtual cyclist using route progress rather than the displayed ESP32 distance.
- `RideController`: owns ride state and elapsed ride time.

Starting a new ride is intended to clear session/HUD telemetry, reset route progression to the first point, and reload the starting world chunk. Finishing a ride should preserve the final statistics for the summary; avoid clearing data before a summary snapshot is captured.

## Repository and Unity setup

The intended workspace keeps the Unity project and any workspace-level chunk inputs beside one another:

```text
VirtualRide/                 # Unity project root
  Assets/
  Packages/
  ProjectSettings/
world_data/
  alappuzha/processed/        # chunk_*.json inputs, if the baker still expects this path
firmware/esp32/               # ESP32 source and notes
route_data/                   # GPX source and documented derived data
hardware/                     # CAD, schematics and measured build notes
docs/                         # Project documentation and photographs
```

Preserve the actual Unity asset paths if the existing project differs from this suggested layout. The workspace-level `world_data` path matters if the OSM chunk baker uses a relative path next to the Unity folder; confirm that path in the actual script before moving folders.

### Open the Unity project

1. Recover and extract the full project archive into a separate working/recovery directory. Keep the original archive unchanged as a backup.
2. Compare its files against [the resource inventory](docs/RESOURCE_INVENTORY.md) before merging files into this repository.
3. In Unity Hub, open the directory containing `Assets/`, `Packages/`, and `ProjectSettings/`.
4. Prefer the exact recorded Unity 6 version, if available; allow package resolution and import to complete.
5. Open `MainRide`, inspect the Console for compile errors, and validate scene references for `RideController`, `RouteBikeController`, `WorldStreamer`, `RideData`, `RideStatistics`, and `BLEManager`.
6. Build and test on the Android device. BLE behavior cannot be fully validated in the Editor.

For the Java bridge callback `UnityPlayer.UnitySendMessage("BLEManager", ...)`, the Unity GameObject must be named exactly `BLEManager`. Review the Android manifest and current permission configuration before changing Android target SDK or replacing templates.

## Unity files to version

Track `Assets/` including all `.meta` files, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/`, the required Java BLE bridge and Android configuration, firmware source, route data, required OSM inputs or baked runtime chunks, and needed CAD/schematic sources. Do **not** track regenerable Unity caches such as `Library/`, `Temp/`, `Obj/`, `Logs/`, local IDE output, or secrets. Some generated content, especially referenced baked prefabs, meshes and materials, is runtime content and must not be excluded merely because it was generated.

## Known issues and next engineering steps

1. **Ride Info session timer stays at `00:00`.** `RideController.rideTime` is intended to increment while a ride is active and not paused/finished. Inspect the display binding/reference/update path before changing summary logic. `RideSummaryUI` is a separate display path.
2. **BLE not validated in captured UI.** Capture a working connection and live telemetry test with relevant logs.
3. **Screen-awake behavior.** If using `Screen.sleepTimeout = SleepTimeout.NeverSleep`, confirm it in a device build during a long untouched ride.
4. **Hardware reproducibility.** Record sensor part number, pin mapping, circuit, magnet count/pulses per revolution, effective roller diameter, drive ratio, power supply ratings, dimensions, and calibration procedure.
5. **Data reproducibility.** Include the GPX source and all required OSM processing inputs, scripts, baked chunk prefabs, and runtime dependencies—or document an explicit data storage and retrieval process.
6. **Build metadata.** Record Unity editor/package versions, Android min/target SDK and app version, ESP32 board/core/BLE library versions, and tested phone/OS build.

Use [`docs/TEST_PLAN.md`](docs/TEST_PLAN.md) as the acceptance checklist and record pass/fail evidence rather than relying on screenshots alone.

## Returning to this project later

1. Start with this README, then read `docs/RESOURCE_INVENTORY.md` and `docs/GIT_WORKFLOW.md`.
2. Locate the original multi-gigabyte development ZIP and preserve it unchanged. Extract a working copy elsewhere; do not commit the whole ZIP into this repository.
3. Recover the Unity project, route files, OSM data and ESP32 source from the extracted archive. Preserve `.meta` files and verify expected folder paths.
4. Compare recovered contents with the resource inventory. Do not overwrite a newer project or combine duplicate folders without reviewing their differences.
5. Open `MainRide` in Unity, resolve compile/package problems, and run the test plan.
6. Record what changed and commit small, meaningful updates to source and documentation.

The documentation release itself is not the complete multi-gigabyte project backup. Keep the full archive as a separate recovery copy and put the extracted, reviewed source files into version control or a documented large-data storage system.

## Resource attribution and licensing

The route/world is described as using a GPX route and OpenStreetMap-derived content. Include required [OpenStreetMap attribution](https://www.openstreetmap.org/copyright) and comply with applicable ODbL obligations for redistributed OSM-derived data. Keep a separate provenance/license list for models, textures, fonts, audio, packages and other third-party assets. Do not add a license until you have decided what rights you intend to grant and checked your rights to redistribute all included assets.

## Change discipline

Where practical, keep firmware calibration, BLE integration, Unity gameplay/UI, OSM processing and documentation changes in separate commits. Update these documents when a pin assignment, packet format, UUID, route transform, chunk layout or build requirement changes. Never commit tokens, passwords, signing keystores or personal credentials.
