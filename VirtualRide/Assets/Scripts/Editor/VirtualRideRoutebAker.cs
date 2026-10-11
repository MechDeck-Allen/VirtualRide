#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VirtualRideRouteBaker
{
    [MenuItem("VirtualRide/Bake Route")]
    public static void BakeRoute()
    {
        GameObject routeSystem =
            GameObject.Find("RouteSystem");

        if (routeSystem == null)
        {
            Debug.LogError(
                "VirtualRide: RouteSystem not found."
            );
            return;
        }

        RouteDataLoader loader =
            routeSystem.GetComponent<RouteDataLoader>();

        RouteWorldGenerator generator =
            routeSystem.GetComponent<RouteWorldGenerator>();

        if (loader == null)
        {
            Debug.LogError(
                "VirtualRide: RouteDataLoader missing from RouteSystem."
            );
            return;
        }

        if (generator == null)
        {
            Debug.LogError(
                "VirtualRide: RouteWorldGenerator missing from RouteSystem."
            );
            return;
        }

        if (loader.routeFile == null)
        {
            Debug.LogError(
                "VirtualRide: Route File is not assigned."
            );
            return;
        }

        Debug.Log(
            "=========================================="
        );

        Debug.Log(
            "VirtualRide: BAKING ROUTE"
        );

        // Explicitly load the JSON in Editor mode.
        loader.LoadRoute();

        if (loader.Data == null)
        {
            Debug.LogError(
                "VirtualRide: Route data failed to load."
            );
            return;
        }

        if (
            loader.Data.points == null ||
            loader.Data.points.Length < 2
        )
        {
            Debug.LogError(
                $"VirtualRide: Route contains only " +
                $"{loader.Data.points?.Length ?? 0} points."
            );
            return;
        }

        // Generate the road in the actual scene.
        generator.GenerateRoute();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );

        Debug.Log(
            "VirtualRide: ROUTE BAKE COMPLETE."
        );

        Debug.Log(
            "Generated Road should now exist under RouteSystem."
        );
    }
}

#endif