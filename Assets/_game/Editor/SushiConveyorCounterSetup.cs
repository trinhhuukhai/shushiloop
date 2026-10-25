using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Saves a regular world TextMeshPro object that designers can style in the scene.
public static class SushiConveyorCounterSetup
{
    private const string GameplayPath = "Assets/_game/Scenes/GamePlay.unity";
    private const string FontPath = "Assets/_game/Font/for localize sub/NotoSans-Black SDF.asset";

    [MenuItem("SushiKhai/UI/Author Conveyor Slot Text")]
    public static void Setup()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before authoring the conveyor counter.");

        var scene = EditorSceneManager.OpenScene(GameplayPath);
        ConveyorManager conveyor = Object.FindFirstObjectByType<ConveyorManager>();
        if (conveyor == null) throw new InvalidOperationException("GamePlay needs ConveyorManager.");
        var config = new SerializedObject(conveyor);
        ConveyorSpline spline = config.FindProperty("spline").objectReferenceValue as ConveyorSpline;
        if (spline == null) throw new InvalidOperationException("ConveyorManager needs its spline reference.");

        var label = config.FindProperty("capacityLabel").objectReferenceValue as TextMeshPro;
        if (label == null)
        {
            Transform child = conveyor.transform.Find("Slot count");
            if (child != null) label = child.GetComponent<TextMeshPro>();
        }
        if (label == null)
        {
            label = new GameObject("Slot count").AddComponent<TextMeshPro>();
            label.transform.SetParent(conveyor.transform, false);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) label.font = font;
            label.fontSize = 7.5f;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = false;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            float width = Mathf.Max(0.7f, spline.halfSize.x * 2f - 2.15f);
            float height = Mathf.Clamp(spline.halfSize.y * 2f - 1.3f, 0.28f, 1.15f);
            label.rectTransform.sizeDelta = new Vector2(width, height);
            label.GetComponent<MeshRenderer>().sortingOrder = -8;
        }

        label.transform.position = new Vector3(spline.center.x, spline.center.y,
            config.FindProperty("positionZ").floatValue);
        label.SetText("{0}/{1}", 0, config.FindProperty("slotCount").intValue);
        label.gameObject.SetActive(false); // Init shows it only after a level has slots.
        config.FindProperty("capacityLabel").objectReferenceValue = label;
        config.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(conveyor);
        EditorUtility.SetDirty(label);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CONVEYOR_COUNTER_SETUP_OK: ConveyorManager/Slot count is editable text only.");
    }
}
