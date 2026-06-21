using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressableSetupTool
{
    private const string OriginPath = "Assets/_Game/Data/Origin";
    private const string GroupName = "Data";

    [MenuItem("Tools/Setup Addressables/Mark Origin Data")]
    public static void MarkOriginDataAsAddressable()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressableSetup] AddressableAssetSettings not found. Please initialize Addressables first.");
            return;
        }

        var group = settings.FindGroup(GroupName);
        if (group == null)
        {
            Debug.LogError($"[AddressableSetup] Group '{GroupName}' not found. Please create it in the Addressables Groups window first.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { OriginPath });
        int count = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = fileName;

            count++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddressableSetup] Done! Marked {count} assets in group '{GroupName}'.");
    }
}
