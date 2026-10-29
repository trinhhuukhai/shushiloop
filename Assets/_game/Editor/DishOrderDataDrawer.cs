#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DishOrderData))]
public sealed class DishOrderDataDrawer : PropertyDrawer
{
    private static readonly string[] Kinds = { "Món có sẵn", "2 nguyên liệu — có cơm", "2 nguyên liệu — không cơm", "3 nguyên liệu — cơm đầu tiên" };
    private static readonly PoolType[] Types = Enum.GetValues(typeof(PoolType)).Cast<PoolType>().Where(SushiRecipeCatalog.IsFoodTopping).ToArray();
    private static readonly string[] Names = Types.Select(SushiRecipeCatalog.IngredientName).ToArray();
    private static float RowHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var kind = (SushiRecipeKind)property.FindPropertyRelative("recipeKind").intValue;
        int rows = kind == SushiRecipeKind.Catalog ? 4 : kind == SushiRecipeKind.ThreeWithRice ? 6 : 5;
        return rows * RowHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        position.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
        position.y += RowHeight;
        var kindProperty = property.FindPropertyRelative("recipeKind");
        kindProperty.intValue = EditorGUI.Popup(position, "Công thức", kindProperty.intValue, Kinds);
        position.y += RowHeight;
        var kind = (SushiRecipeKind)kindProperty.intValue;
        if (kind == SushiRecipeKind.Catalog)
            EditorGUI.PropertyField(position, property.FindPropertyRelative("dish"), new GUIContent("Món"));
        else
        {
            if (kind != SushiRecipeKind.TwoWithoutRice)
            {
                EditorGUI.LabelField(position, "Nguyên liệu 1", "Cơm (nền)");
                position.y += RowHeight;
            }
            DrawTopping(position, property.FindPropertyRelative("firstIngredient"), kind == SushiRecipeKind.TwoWithoutRice ? "Nguyên liệu 1" : "Nguyên liệu 2");
            if (kind != SushiRecipeKind.TwoWithRice)
            {
                position.y += RowHeight;
                DrawTopping(position, property.FindPropertyRelative("secondIngredient"), kind == SushiRecipeKind.TwoWithoutRice ? "Nguyên liệu 2" : "Nguyên liệu 3");
            }
        }
        position.y += RowHeight;
        var amount = property.FindPropertyRelative("amount");
        EditorGUI.PropertyField(position, amount, new GUIContent("Số phần"));
        amount.intValue = Mathf.Max(1, amount.intValue);
        EditorGUI.EndProperty();
    }

    private static void DrawTopping(Rect position, SerializedProperty property, string label)
    {
        int index = Array.IndexOf(Types, (PoolType)property.intValue);
        int selected = EditorGUI.Popup(position, label, index, Names);
        if (selected >= 0 && selected < Types.Length) property.intValue = (int)Types[selected];
    }
}
#endif
