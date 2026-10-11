#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldStreamer))]
public class WorldStreamerEditor : Editor
{
    private const string CHUNK_FOLDER =
        "Assets/Prefabs/OSMChunks";

    public override void OnInspectorGUI()
    {
        // Draw the normal WorldStreamer inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(15);

        EditorGUILayout.LabelField(
            "VirtualRide Chunk Tools",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Automatically finds all baked OSM chunk prefabs " +
            "inside Assets/Prefabs/OSMChunks and assigns them " +
            "to WorldStreamer in numerical order.",
            MessageType.Info
        );

        EditorGUILayout.Space(5);

        if (GUILayout.Button(
            "AUTO ASSIGN ALL OSM CHUNKS",
            GUILayout.Height(35)))
        {
            AutoAssignChunks();
        }
    }

    private void AutoAssignChunks()
    {
        WorldStreamer streamer =
            (WorldStreamer)target;

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { CHUNK_FOLDER }
            );

        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Chunks Found",
                "No prefab chunks were found in:\n\n" +
                CHUNK_FOLDER +
                "\n\nCheck that your baked prefabs are located there.",
                "OK"
            );

            return;
        }

        List<GameObject> chunks =
            new List<GameObject>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path
                );

            if (prefab != null)
            {
                chunks.Add(prefab);
            }
        }

        // -----------------------------------------------------
        // SORT BY CHUNK NUMBER
        // -----------------------------------------------------

        chunks = chunks
            .OrderBy(GetChunkNumber)
            .ToList();

        // -----------------------------------------------------
        // ASSIGN TO WORLDSSTREAMER
        // -----------------------------------------------------

        Undo.RecordObject(
            streamer,
            "Auto Assign OSM Chunks"
        );

        streamer.chunkPrefabs =
            chunks;

        EditorUtility.SetDirty(
            streamer
        );

        AssetDatabase.SaveAssets();

        // -----------------------------------------------------
        // LOG RESULTS
        // -----------------------------------------------------

        Debug.Log(
            $"VirtualRide: Automatically assigned " +
            $"{chunks.Count} OSM chunk prefabs."
        );

        for (
            int i = 0;
            i < chunks.Count;
            i++
        )
        {
            Debug.Log(
                $"Chunk {i:000} → " +
                chunks[i].name
            );
        }

        EditorUtility.DisplayDialog(
            "OSM Chunks Assigned",
            $"Successfully assigned {chunks.Count} OSM chunk prefabs.",
            "OK"
        );
    }

    private int GetChunkNumber(
        GameObject prefab
    )
    {
        if (prefab == null)
        {
            return int.MaxValue;
        }

        string name =
            prefab.name;

        // Expected:
        // OSM_Chunk_000
        // OSM_Chunk_001
        // OSM_Chunk_067

        string[] parts =
            name.Split('_');

        if (parts.Length > 0)
        {
            string lastPart =
                parts[parts.Length - 1];

            if (int.TryParse(
                lastPart,
                out int number))
            {
                return number;
            }
        }

        return int.MaxValue;
    }
}

#endif