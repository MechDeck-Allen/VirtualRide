# Resource Inventory and Recovery Checklist

This inventory distinguishes the documentation currently committed from the working-project resources described by the development history. Use it to decide whether a fresh clone can reproduce VirtualRide.

## Documentation and reference assets in this repository

- `README.md`: project overview, architecture, photographs, initial setup and return-to-project instructions.
- `.gitignore` and `.gitattributes`: repository hygiene and line-ending rules.
- `docs/HARDWARE.md`: hardware photo record, measurement and calibration requirements.
- `docs/SOFTWARE_ARCHITECTURE.md`: Unity components, telemetry flow, ride lifecycle and timer debugging notes.
- `docs/GIT_WORKFLOW.md`: source-control workflow, large data and backup strategy.
- `docs/TEST_PLAN.md`: Unity, sensor, BLE, UI, chunk and fresh-checkout checks.
- `docs/images/`: five supplied photographs showing the app UI and hardware prototypes.
- `firmware/esp32/README.md`: firmware integration contract and configuration record.
- `hardware/cad/README.md`, `hardware/schematics/README.md`: CAD and verified wiring requirements.
- `route_data/README.md`, `world_data/README.md`: route and OSM data layout/provenance notes.

## Core project resources to recover from the development archive

The multi-gigabyte working archive is described as containing OSM chunks, project code and Python scripts. Extract a copy into a separate recovery directory and inspect it before adding any files to the repository. The following are expected resources from the documented architecture; verify actual filenames and contents against the archive.

| Resource | Why it matters | Expected/possible location |
|---|---|---|
| Unity `Assets/` and all `.meta` files | Scenes, scripts, prefabs, materials, models, textures and runtime data | `VirtualRide/Assets/` |
| `Packages/manifest.json` and `packages-lock.json` | Package dependency versions | `VirtualRide/Packages/` |
| `ProjectSettings/` | Unity player, graphics, input and Android settings | `VirtualRide/ProjectSettings/` |
| `MainRide.unity` | Primary runtime scene | Existing scene path, often `Assets/Scenes/` |
| `RideWorld.unity` | Additional/staging scene described in project history | Preserve actual path |
| Ride, HUD and route C# scripts | Telemetry, bike motion, ride state and summary | Existing script folders |
| `BLEManager.cs` and `VirtualRideBleBridge.java` | Unity/Android BLE transport | Existing script and plugin paths |
| Android manifest and Gradle templates | Permissions and repeatable Android builds | Existing project paths |
| OSM world generator/enhancer/chunk baker scripts | Rebuilds generated world and chunk prefabs | Existing Editor/tool folders |
| Baked OSM chunk prefabs plus referenced assets | Needed at runtime for streaming | Existing prefab/generated asset paths |
| `world_data/alappuzha/processed/chunk_*.json` | Inputs expected by the documented chunk baker | Workspace-level path, if confirmed in current code |
| GPX route and route conversion inputs/outputs | Reconstructable bike trajectory | `route_data/` or existing referenced path |
| ESP32 sketch (`.ino`) and local headers | Rotation, telemetry and BLE command behavior | `firmware/esp32/` or original path until audited |
| CAD/STEP/STL files and actual schematic | Physical rebuild | `hardware/cad/`, `hardware/schematics/` |
| Version/build record | Allows consistent setup later | Add tested Unity, Android and ESP32 versions to docs |

## Fresh-checkout acceptance checklist

- [ ] Unity project folders `Assets/`, `Packages/`, and `ProjectSettings/` are present.
- [ ] Unity `.meta` files are tracked and preserved.
- [ ] Required scenes, scripts, Android bridge, prefabs and referenced runtime assets are present.
- [ ] OSM inputs, Python processing scripts, baked chunk prefabs and their runtime dependencies are accounted for.
- [ ] GPX source and route processing steps are documented.
- [ ] The exact tested ESP32 firmware is present, with pin map and sensor calibration details.
- [ ] Firmware, Java BLE bridge and Unity parser agree on packet format and UUIDs.
- [ ] No secrets, signing keys or private credentials are in Git.
- [ ] Unity opens without compile errors and the primary scene references are valid.
- [ ] The test plan has recorded results from the actual Android device and physical prototype.

## Data provenance and licensing

Record source, acquisition date, geographic coverage, processing versions and license/attribution for the route, OpenStreetMap-derived data, models, textures, fonts, audio, plugins and other third-party resources. Follow [OpenStreetMap attribution requirements](https://www.openstreetmap.org/copyright) and applicable ODbL obligations for redistributed derived data. Do not assume third-party assets can be publicly redistributed merely because they exist in the local archive.

## Backup policy

Keep the original multi-gigabyte ZIP unchanged and outside the Git working tree, ideally with a second copy stored separately. Use Git for incremental changes to source and documentation. For assets that exceed normal Git size expectations, evaluate Git LFS or versioned external data/artifact storage. The complete archive is a recovery copy; it is not a substitute for the structured project files needed to review and version changes.
