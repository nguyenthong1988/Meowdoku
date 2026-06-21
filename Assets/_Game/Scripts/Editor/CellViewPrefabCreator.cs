using System.IO;
using Cast.Game.Board;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class CellViewPrefabCreator
{
    private const string PrefabPath = "Assets/_Game/Prefabs/Board/CellView.prefab";
    private const string AddressableKey = "CellView";
    private const string AddressableGroup = "Default Local Group";

    [MenuItem("Tools/Setup Addressables/Create CellView Prefab")]
    public static void CreateCellViewPrefab()
    {
        
        string dir = Path.GetDirectoryName(PrefabPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var root = new GameObject("CellView");

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(root.transform, false);
        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sortingOrder = 0;

        var catGo = new GameObject("Cat");
        catGo.transform.SetParent(root.transform, false);
        var catSr = catGo.AddComponent<SpriteRenderer>();
        catSr.sortingOrder = 1;

        var markGo = new GameObject("Mark");
        markGo.transform.SetParent(root.transform, false);
        var markSr = markGo.AddComponent<SpriteRenderer>();
        markSr.sortingOrder = 1;

        var cellView = root.AddComponent<CellView>();
        var so = new SerializedObject(cellView);
        so.FindProperty("_background").objectReferenceValue = bgSr;
        so.FindProperty("_cat").objectReferenceValue = catSr;
        so.FindProperty("_mark").objectReferenceValue = markSr;
        so.ApplyModifiedPropertiesWithoutUndo();

        bool success;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out success);
        Object.DestroyImmediate(root);

        if (!success || prefab == null)
        {
            Debug.LogError($"[CellViewCreator] Failed to save prefab at {PrefabPath}");
            return;
        }

        AssetDatabase.Refresh();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[CellViewCreator] Prefab saved but Addressables not configured — mark it manually.");
            return;
        }

        var group = settings.FindGroup(AddressableGroup)
                    ?? settings.DefaultGroup;

        string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
        var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        entry.address = AddressableKey;

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CellViewCreator] Prefab created at '{PrefabPath}' and marked Addressable as '{AddressableKey}' in group '{group.Name}'.");
    }
}
