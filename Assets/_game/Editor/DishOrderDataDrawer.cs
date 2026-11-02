#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DishOrderData))]
public sealed class DishOrderDataDrawer : PropertyDrawer
{
    private static readonly string[] Kinds = { "Món có sẵn", "2 nguyên liệu — có cơm", "2 nguyên liệu — không cơm", "3 nguyên liệu — cơm đầu tiên", "3 nguyên liệu — không cơm" };
    private static readonly string[] DishNames = SushiRecipeCatalog.All.Select(recipe => SushiRecipeCatalog.AssetName(recipe.Id)).ToArray();
    private static readonly SushiDishId[] DishIds = SushiRecipeCatalog.All.Select(recipe => recipe.Id).ToArray();
    private static readonly PoolType[] Types = Enum.GetValues(typeof(PoolType)).Cast<PoolType>().Where(SushiRecipeCatalog.IsFoodTopping).ToArray();
    private static readonly string[] Names = Types.Select(type => SushiRecipeCatalog.AssetName(type)).ToArray();
    private static readonly PoolType[] NigiriTypes = Types.Where(SushiRecipeCatalog.IsNigiriTopping).ToArray();
    private static readonly string[] NigiriNames = NigiriTypes.Select(type => SushiRecipeCatalog.AssetName(type)).ToArray();
    private static float RowHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var kind = (SushiRecipeKind)property.FindPropertyRelative("recipeKind").intValue;
        int rows = kind == SushiRecipeKind.Catalog ? 4 :
            kind == SushiRecipeKind.ThreeWithRice || kind == SushiRecipeKind.ThreeWithoutRice ? 6 : 5;
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
        {
            var dish = property.FindPropertyRelative("dish");
            int index = Array.IndexOf(DishIds, (SushiDishId)dish.intValue);
            int selected = EditorGUI.Popup(position, "Món", index >= 0 && index < DishNames.Length ? index : -1, DishNames);
            if (selected >= 0) dish.intValue = (int)SushiRecipeCatalog.All[selected].Id;
        }
        else
        {
            bool withoutRice = kind == SushiRecipeKind.TwoWithoutRice || kind == SushiRecipeKind.ThreeWithoutRice;
            if (!withoutRice)
            {
                EditorGUI.LabelField(position, "Nguyên liệu 1", "Cơm (nền)");
                position.y += RowHeight;
            }
            DrawTopping(position, property.FindPropertyRelative("firstIngredient"), withoutRice ? "Nguyên liệu 1" : "Nguyên liệu 2",
                kind == SushiRecipeKind.TwoWithRice);
            if (kind != SushiRecipeKind.TwoWithRice)
            {
                position.y += RowHeight;
                DrawTopping(position, property.FindPropertyRelative("secondIngredient"), withoutRice ? "Nguyên liệu 2" : "Nguyên liệu 3");
            }
            if (kind == SushiRecipeKind.ThreeWithoutRice)
            {
                position.y += RowHeight;
                DrawTopping(position, property.FindPropertyRelative("thirdIngredient"), "Nguyên liệu 3");
            }
        }
        position.y += RowHeight;
        var amount = property.FindPropertyRelative("amount");
        EditorGUI.PropertyField(position, amount, new GUIContent("Số phần"));
        amount.intValue = Mathf.Max(1, amount.intValue);
        EditorGUI.EndProperty();
    }

    private static void DrawTopping(Rect position, SerializedProperty property, string label, bool nigiriOnly = false)
    {
        var types = nigiriOnly ? NigiriTypes : Types;
        int index = Array.IndexOf(types, (PoolType)property.intValue);
        int selected = EditorGUI.Popup(position, label, index, nigiriOnly ? NigiriNames : Names);
        if (selected >= 0 && selected < types.Length) property.intValue = (int)types[selected];
    }
}
#endif
