using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(OSLDeviceProductInfo))]
public sealed class OSLDeviceProductInfoDrawer : PropertyDrawer
{
    const string catalogPath = "Assets/OSLDevices/Store/Resources/OSLStoreProductCatalog.asset";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            nextLine(ref line);
            EditorGUI.PropertyField(line, property.FindPropertyRelative("isPurchasable"));

            nextLine(ref line);
            EditorGUI.PropertyField(line, property.FindPropertyRelative("requiresEntitlement"));

            nextLine(ref line);
            drawSkuField(line, property.FindPropertyRelative("storeSku"), new GUIContent("Store Sku"));

            nextLine(ref line);
            drawSkuArray(line, property.FindPropertyRelative("storeSkus"));
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = 1;
        if (property.isExpanded)
        {
            lineCount += 3;
            SerializedProperty storeSkus = property.FindPropertyRelative("storeSkus");
            lineCount += 1;
            if (storeSkus != null && storeSkus.isExpanded) lineCount += 1 + storeSkus.arraySize;
        }

        return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }

    static void drawSkuArray(Rect line, SerializedProperty storeSkus)
    {
        if (storeSkus == null) return;

        storeSkus.isExpanded = EditorGUI.Foldout(line, storeSkus.isExpanded, new GUIContent("Store Skus"), true);
        if (!storeSkus.isExpanded) return;

        EditorGUI.indentLevel++;
        nextLine(ref line);
        int nextSize = Mathf.Max(0, EditorGUI.IntField(line, "Size", storeSkus.arraySize));
        if (nextSize != storeSkus.arraySize) storeSkus.arraySize = nextSize;

        for (int i = 0; i < storeSkus.arraySize; ++i)
        {
            nextLine(ref line);
            drawSkuField(line, storeSkus.GetArrayElementAtIndex(i), new GUIContent("Element " + i));
        }

        EditorGUI.indentLevel--;
    }

    static void drawSkuField(Rect line, SerializedProperty skuProperty, GUIContent label)
    {
        if (skuProperty == null) return;

        SkuOptions options = OSLStoreProductCatalogEditorUtility.buildSkuOptions(skuProperty.stringValue);
        int selectedIndex = options.findValue(skuProperty.stringValue);
        int nextIndex = EditorGUI.Popup(line, label, selectedIndex, options.labels);
        if (nextIndex >= 0 && nextIndex < options.values.Length) skuProperty.stringValue = options.values[nextIndex];
    }

    static void nextLine(ref Rect line)
    {
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    [MenuItem("OpenSoundLab/Devices/Select Store Product Catalog")]
    static void SelectStoreProductCatalog()
    {
        OSLStoreProductCatalog catalog = AssetDatabase.LoadAssetAtPath<OSLStoreProductCatalog>(catalogPath);
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("OpenSoundLab Store Product Catalog", "No store product catalog asset exists at " + catalogPath + ".", "OK");
            return;
        }

        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);
    }
}

internal sealed class SkuOptions
{
    public GUIContent[] labels;
    public string[] values;

    public int findValue(string value)
    {
        if (values == null) return 0;

        for (int i = 0; i < values.Length; ++i)
        {
            if (values[i] == value) return i;
        }

        return 0;
    }
}

internal static class OSLStoreProductCatalogEditorUtility
{
    public static SkuOptions buildSkuOptions(string currentSku)
    {
        List<GUIContent> labels = new List<GUIContent>();
        List<string> values = new List<string>();

        labels.Add(new GUIContent("None"));
        values.Add("");

        OSLStoreProductCatalog[] catalogs = Resources.LoadAll<OSLStoreProductCatalog>("");
        for (int catalogIndex = 0; catalogs != null && catalogIndex < catalogs.Length; ++catalogIndex)
        {
            OSLStoreProductCatalog catalog = catalogs[catalogIndex];
            if (catalog == null || catalog.products == null) continue;

            for (int productIndex = 0; productIndex < catalog.products.Length; ++productIndex)
            {
                OSLStoreProductDefinition product = catalog.products[productIndex];
                if (product == null || string.IsNullOrWhiteSpace(product.sku)) continue;
                if (values.Contains(product.sku)) continue;

                values.Add(product.sku);
                labels.Add(new GUIContent(formatProductLabel(product)));
            }
        }

        if (!string.IsNullOrWhiteSpace(currentSku) && !values.Contains(currentSku))
        {
            values.Add(currentSku);
            labels.Add(new GUIContent("Missing: " + currentSku));
        }

        return new SkuOptions
        {
            labels = labels.ToArray(),
            values = values.ToArray()
        };
    }

    static string formatProductLabel(OSLStoreProductDefinition product)
    {
        string label = string.IsNullOrWhiteSpace(product.displayName) ? product.sku : product.displayName + " (" + product.sku + ")";
        if (product.kind == OSLStoreProductKind.Bundle) label += " [Bundle]";
        if (!product.enabled) label += " [Disabled]";
        return label;
    }
}
