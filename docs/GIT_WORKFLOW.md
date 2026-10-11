# Git Workflow, Large Assets and Backups

## Repository layout

The intended workspace layout keeps the Unity project and any workspace-level OSM data inside one repository:

```text
VirtualRide/                 # Unity project root
  Assets/
  Packages/
  ProjectSettings/
world_data/
  alappuzha/processed/        # chunk_*.json if the current baker expects this path
firmware/esp32/
hardware/cad/
hardware/schematics/
route_data/
docs/
```

Preserve actual Unity asset paths. The documented OSM chunk baker expects `world_data/alappuzha/processed/` beside the Unity directory; inspect the actual script before moving the data or changing the path.

## Track

- `Assets/` including every `.meta` file.
- `Packages/manifest.json`, `Packages/packages-lock.json` and `ProjectSettings/`.
- Required scripts, Java bridge source, Android manifest and build templates.
- Baked chunk prefabs and all meshes/materials/textures they reference at runtime.
- Required chunk-baker inputs/scripts, route source and derived data, firmware, CAD/schematics and project documentation.

## Ignore by default

Unity's regenerable `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, local IDE output, local build outputs, signing keys, secrets and backup archives. Do not automatically ignore referenced generated assets or baked OSM prefabs merely because they were generated. They may be essential to open or run the project.

Keep Unity `.meta` files tracked. They preserve Unity asset GUID references.

## Daily workflow

Start by checking repository state:

```bash
git status
git pull --rebase
```

After making a small, tested change:

```bash
git status --short
git diff
git add path/to/changed-file.cs docs/relevant-document.md
git diff --cached
git commit -m "Describe the tested change"
git push
```

Use focused commits, for example: `Fix ride timer display`, `Document roller-frame prototype`, `Update BLE packet parser`, or `Add baked route chunk data`.

## Initial local setup

If the repository has no local clone yet:

```bash
git clone https://github.com/MechDeck-Allen/VirtualRide.git
cd VirtualRide
git status
```

When recovering the Unity project, keep the original ZIP untouched and extract a copy outside this Git working folder. Compare directory contents and resolve differences before moving recovered source into the repository. Do not overwrite the existing source tree blindly.

## Large assets and OSM data

Do not commit the complete multi-gigabyte workspace ZIP into normal Git. Commit the extracted, useful source tree so changes are individually reviewable. For large binary assets, review file sizes and GitHub limits; consider Git LFS or a separately versioned data/artifact store. Check LFS storage and bandwidth quotas before adopting it. Unity YAML scenes/prefabs and scripts should normally remain in Git; large binary media/model sources are better candidates for LFS.

For OSM-derived data, retain provenance, retrieval date, geographic coverage, processing scripts/version and required OpenStreetMap attribution/ODbL compliance. Confirm redistribution rights for all third-party assets before publishing.

## Backup policy

Before Unity upgrades or large world rebakes, close Unity and create a dated archive of the complete workspace, including required workspace-level `world_data`. Store it outside the repository and maintain another copy on a separate drive or trusted backup destination. Git history is for source changes; the archive is an additional recovery copy, not the primary version-control format.

Never commit tokens, passwords, signing keystores, private signing credentials or personal data.
