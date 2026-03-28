using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class OneShotGroundColliderFix
{
    private static readonly string[] TargetScenePaths =
    {
        "Assets/Scenes/Game.unity",
        "Assets/Scenes/Tutorial.unity"
    };

    private static readonly string[] TargetPrefabFolders =
    {
        "Assets/MapPrefabs",
        "Assets/MapPrefabs/Miniboss"
    };

    [MenuItem("Tools/One-shot/Apply Ground Collider Fix")]
    public static void ApplyGroundColliderFix()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "One-shot Ground Fix",
            "This will permanently modify prefabs and scenes by fixing Ground collider setup. Continue?",
            "Run",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        int prefabCount = 0;
        int sceneCount = 0;
        int fixedColliders = 0;

        // 1) Fix prefabs only in target map folders
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", TargetPrefabFolders);
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (string.IsNullOrEmpty(prefabPath))
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                continue;
            }

            bool changed = FixGroundInHierarchy(root.transform, ref fixedColliders);
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                prefabCount++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        // 2) Fix only target scenes
        for (int i = 0; i < TargetScenePaths.Length; i++)
        {
            string scenePath = TargetScenePaths[i];
            if (string.IsNullOrEmpty(scenePath))
            {
                continue;
            }

            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OneShotGroundColliderFix] Skipping scene '{scenePath}': {ex.Message}");
                continue;
            }

            bool changedInScene = false;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                if (FixGroundInHierarchy(roots[r].transform, ref fixedColliders))
                {
                    changedInScene = true;
                }
            }

            if (changedInScene)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sceneCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "One-shot Ground Fix",
            $"Done.\nUpdated prefabs: {prefabCount}\nUpdated scenes: {sceneCount}\nFixed tilemap colliders: {fixedColliders}",
            "OK");

        Debug.Log($"[OneShotGroundColliderFix] Updated prefabs: {prefabCount}, scenes: {sceneCount}, colliders fixed: {fixedColliders}");
    }

    private static bool FixGroundInHierarchy(Transform root, ref int fixedColliders)
    {
        bool changedAny = false;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null || t.name != "Ground")
            {
                continue;
            }

            if (FixGroundObject(t.gameObject, ref fixedColliders))
            {
                changedAny = true;
            }
        }

        return changedAny;
    }

    private static bool FixGroundObject(GameObject ground, ref int fixedColliders)
    {
        bool changed = false;
        TilemapCollider2D[] colliders = ground.GetComponentsInChildren<TilemapCollider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            TilemapCollider2D tilemapCollider = colliders[i];
            if (tilemapCollider == null)
            {
                continue;
            }

            GameObject go = tilemapCollider.gameObject;
            bool localChanged = false;

            if (!tilemapCollider.usedByComposite)
            {
                tilemapCollider.usedByComposite = true;
                localChanged = true;
            }

            if (tilemapCollider.isTrigger)
            {
                tilemapCollider.isTrigger = false;
                localChanged = true;
            }

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody2D>();
                localChanged = true;
            }
            if (rb.bodyType != RigidbodyType2D.Static)
            {
                rb.bodyType = RigidbodyType2D.Static;
                localChanged = true;
            }

            CompositeCollider2D composite = go.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = go.AddComponent<CompositeCollider2D>();
                localChanged = true;
            }
            if (composite.geometryType != CompositeCollider2D.GeometryType.Outlines)
            {
                composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
                localChanged = true;
            }

            if (localChanged)
            {
                EditorUtility.SetDirty(go);
                fixedColliders++;
                changed = true;
            }
        }

        return changed;
    }
}
