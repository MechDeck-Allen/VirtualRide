# Workspace-Level OSM Data

The documented chunk-baking workflow expects processed JSON inputs under `world_data/alappuzha/processed/`, beside the Unity project directory at the workspace root. Preserve that layout if the current baker still uses the relative path; verify the actual script before reorganizing the workspace.

Store the `chunk_*.json` inputs needed to reproduce baked OSM chunk prefabs where license and repository size permit. Record data source, retrieval date, geographic extent/corridor, processing scripts and versions, chunk naming/ranges, coordinate conventions, runtime prefab dependencies and OpenStreetMap attribution/ODbL compliance.

Review file sizes before staging large datasets. Use Git LFS or a separately versioned data/artifact store if appropriate. Do not store the full multi-gigabyte backup ZIP here: it does not replace structured world inputs, scripts or baked runtime assets.
