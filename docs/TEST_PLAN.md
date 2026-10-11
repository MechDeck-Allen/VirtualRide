# VirtualRide Test Plan

Use this checklist after changes to firmware, BLE, ride state, route transforms, chunk streaming, UI or Android build settings. Record results; a screenshot by itself is not proof that a feature works.

## Test record template

- Date:
- Git commit:
- Unity editor and package versions:
- Android device / OS:
- ESP32 board / firmware revision:
- Hall sensor model and pulses per revolution:
- Test area / route:
- Result: Pass / Fail / Blocked
- Logs, screenshot or notes:

## A. Unity project and scene

- [ ] Fresh checkout contains `Assets/`, `Packages/`, `ProjectSettings/` and all required `.meta` files.
- [ ] Unity package resolution completes and there are no red Console compile errors.
- [ ] `MainRide` opens with valid references assigned.
- [ ] Route loader has valid route data and at least two points.
- [ ] Bike begins at the expected route start and road surface.
- [ ] All required chunk prefabs and referenced meshes/materials are present.
- [ ] A fresh clone does not depend on an untracked folder outside the repository.

## B. Sensor and firmware

- [ ] Record the exact sensor part and GPIO pin.
- [ ] Rotate the shaft one full revolution by hand and count real sensor pulses.
- [ ] Confirm no false pulses occur while stationary.
- [ ] Verify RPM against a known rotation rate.
- [ ] Confirm speed calibration matches measured effective circumference and drive ratio.
- [ ] Confirm session distance increments and reset behavior is consistent.

## C. BLE and Android

- [ ] Android permission prompts and BLE scanning work on the target OS.
- [ ] Bridge and firmware use the same service/characteristic UUIDs.
- [ ] Successful connection is shown accurately in the HUD.
- [ ] Speed, RPM and distance update from live telemetry.
- [ ] Invalid or partial packets do not crash the app or corrupt displayed values.
- [ ] Disconnect/reconnect behavior is tested.
- [ ] Reset command behavior is verified against firmware.

## D. Ride lifecycle and UI

- [ ] New/Start Ride resets session values and starts at the initial route point/chunk.
- [ ] Pause freezes ride time and route progression; resume continues both.
- [ ] Finish stops ride timing and preserves summary statistics.
- [ ] Right-side Ride Info timer increments (known failure in the captured build: it remains `00:00`).
- [ ] Screen-awake behavior is tested during a longer session.
- [ ] UI remains readable in Android landscape and BLE status matches actual connection state.

## E. World chunks and recovery

- [ ] Chunk baker can find its expected `world_data/alappuzha/processed/` inputs.
- [ ] Baked chunk prefabs are present and stream in/out around the cyclist.
- [ ] Route and chunk coordinate transforms agree.
- [ ] No visible gaps, misplaced roads or missing referenced assets along the tested route.
- [ ] Data source/provenance and OpenStreetMap attribution are documented.

Do not mark the project reproducible until a clean checkout can open in Unity and the essential route/chunk data, firmware and Android BLE path have been checked.
