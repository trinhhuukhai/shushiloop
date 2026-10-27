using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene authoring only: keep the existing visible queue and add offscreen arrival anchors.
public static class SushiCampaignSceneSetup
{
    private const string GameplayPath = "Assets/_game/Scenes/GamePlay.unity";

    [MenuItem("SushiKhai/Levels/Prepare Campaign Waiting Queue")]
    public static void Setup()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before authoring campaign waiting positions.");
        Scene scene = SceneManager.GetSceneByPath(GameplayPath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(GameplayPath);
        CustomerManager manager = FindInScene<CustomerManager>(scene);
        if (manager == null) throw new InvalidOperationException("GamePlay needs a CustomerManager.");

        var settings = new SerializedObject(manager);
        SerializedProperty positions = settings.FindProperty("waitingPositions");
        var used = new HashSet<Transform>
        {
            settings.FindProperty("firstSeat").objectReferenceValue as Transform,
            settings.FindProperty("secondSeat").objectReferenceValue as Transform
        };
        used.Remove(null);
        for (int i = 0; i < positions.arraySize; i++)
        {
            Transform position = positions.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if (position == null || !used.Add(position))
                throw new InvalidOperationException("Existing waiting anchor " + (i + 1) + " is missing or duplicated.");
        }
        var campaign = AssetDatabase.LoadAssetAtPath<AllLevelData>("Assets/_game/LevelData/_AllLevelData.asset");
        if (campaign == null || campaign.listAllLevel == null)
            throw new InvalidOperationException("Missing campaign data.");
        int required = 0;
        foreach (var level in campaign.listAllLevel)
            if (level != null) required = Mathf.Max(required, level.CustomerCount - 2);
        if (positions.arraySize >= required)
        {
            Debug.Log("CAMPAIGN_QUEUE_READY " + positions.arraySize + " waiting anchors.");
            return;
        }
        if (positions.arraySize == 0)
            throw new InvalidOperationException("Keep at least one authored visible waiting position before extending the queue.");

        int originalCount = positions.arraySize;
        Transform last = positions.GetArrayElementAtIndex(originalCount - 1).objectReferenceValue as Transform;
        Transform previous = originalCount > 1
            ? positions.GetArrayElementAtIndex(originalCount - 2).objectReferenceValue as Transform : null;
        Vector3 step = previous != null ? last.position - previous.position : Vector3.right * 1.2f;
        step.z = 0;
        if (step.sqrMagnitude < 0.01f) step = Vector3.right * 1.2f;
        float spacing = Mathf.Max(1.2f, step.magnitude);
        Vector3 direction = step.normalized;

        Vector2 playfieldSize = new Vector2(9, 16), playfieldCenter = Vector2.zero;
        CameraControl camera = FindInScene<CameraControl>(scene);
        if (camera != null)
        {
            var framing = new SerializedObject(camera);
            playfieldSize = framing.FindProperty("playfieldSize").vector2Value;
            playfieldCenter = framing.FindProperty("playfieldCenter").vector2Value;
        }
        // The first extra guest starts outside the portrait playfield. Subsequent guests
        // continue at the original queue spacing and enter the saved visible anchors normally.
        float firstAdvance = Mathf.Max(spacing, DistanceOutsidePlayfield(
            last.position, direction, playfieldCenter, playfieldSize, spacing * 2));
        Transform parent = last.parent != null ? last.parent : manager.transform;
        for (int i = originalCount; i < required; i++)
        {
            string anchorName = "Campaign Waiting " + (i + 1);
            Transform anchor = parent.Find(anchorName);
            if (anchor == null || used.Contains(anchor))
            {
                // An existing marker's name may not match its list index after scene edits.
                // Keep that binding and give the extra marker an unused sibling name.
                string uniqueName = anchorName;
                int suffix = 2;
                while (parent.Find(uniqueName) != null) uniqueName = anchorName + " (" + suffix++ + ")";
                anchor = new GameObject(uniqueName).transform;
                SceneManager.MoveGameObjectToScene(anchor.gameObject, scene);
                anchor.SetParent(parent, false);
            }
            if (!used.Add(anchor))
                throw new InvalidOperationException("Campaign waiting anchor is already referenced: " + anchorName);
            anchor.position = last.position + direction * (firstAdvance + spacing * (i - originalCount));
            anchor.rotation = last.rotation;
            anchor.localScale = Vector3.one;
            positions.InsertArrayElementAtIndex(i);
            positions.GetArrayElementAtIndex(i).objectReferenceValue = anchor;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save campaign waiting anchors in GamePlay.");
        Debug.Log("CAMPAIGN_QUEUE_READY " + positions.arraySize + " waiting anchors; preserved " +
            originalCount + " existing positions.");
    }

    private static float DistanceOutsidePlayfield(Vector3 origin, Vector3 direction,
        Vector2 center, Vector2 size, float margin)
    {
        float distance = float.PositiveInfinity;
        if (Mathf.Abs(direction.x) > 0.0001f)
        {
            float edge = center.x + Mathf.Sign(direction.x) * (Mathf.Abs(size.x) * 0.5f + margin);
            float along = (edge - origin.x) / direction.x;
            if (along >= 0) distance = Mathf.Min(distance, along);
        }
        if (Mathf.Abs(direction.y) > 0.0001f)
        {
            float edge = center.y + Mathf.Sign(direction.y) * (Mathf.Abs(size.y) * 0.5f + margin);
            float along = (edge - origin.y) / direction.y;
            if (along >= 0) distance = Mathf.Min(distance, along);
        }
        return float.IsPositiveInfinity(distance) ? margin : distance;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }
}
