using System.Collections.Generic;
using System.IO;
using Cast.Game;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

public static class ViewGameplayPrefabCreator
{
    private const string PrefabPath = "Assets/_Game/Prefabs/UI/Views/ViewGamePlay.prefab";
    private const string AddressableKey = "ViewGameplay";
    private const string AddressableGroup = "Default Local Group";
    private const int DefaultHearts = 5;

    [MenuItem("Tools/Setup Addressables/Create ViewGameplay Prefab")]
    public static void CreateViewGameplayPrefab()
    {
        string dir = Path.GetDirectoryName(PrefabPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var root = new GameObject("ViewGameplay");
        root.AddComponent<RectTransform>();

        // Level label
        var labelGo = CreateUIObject("LevelLabel", root.transform);
        SetAnchors(labelGo.GetComponent<RectTransform>(), new Vector2(0.1f, 0.88f), new Vector2(0.9f, 1f));
        var levelLabel = labelGo.AddComponent<Text>();
        levelLabel.text = "Level 1";
        levelLabel.alignment = TextAnchor.MiddleCenter;
        levelLabel.fontSize = 36;

        // Heart bar
        var heartBarGo = CreateUIObject("HeartBar", root.transform);
        SetAnchors(heartBarGo.GetComponent<RectTransform>(), new Vector2(0f, 0.78f), new Vector2(0.55f, 0.88f));
        var heartLayout = heartBarGo.AddComponent<HorizontalLayoutGroup>();
        heartLayout.childControlWidth = false;
        heartLayout.childControlHeight = false;
        heartLayout.childForceExpandWidth = false;
        heartLayout.spacing = 8f;
        var heartBar = heartBarGo.AddComponent<HeartBar>();

        var hearts = new List<HeartIcon>();
        HeartIcon heartTemplate = null;
        for (int i = 0; i < DefaultHearts; i++)
        {
            var icon = CreateHeartIcon(heartBarGo.transform, $"Heart{i}");
            hearts.Add(icon);
            if (heartTemplate == null) heartTemplate = icon;
        }

        var heartBarSo = new SerializedObject(heartBar);
        heartBarSo.FindProperty("_heartPrefab").objectReferenceValue = heartTemplate;
        heartBarSo.FindProperty("_container").objectReferenceValue = heartBarGo.transform;
        var heartsProp = heartBarSo.FindProperty("_hearts");
        heartsProp.arraySize = hearts.Count;
        for (int i = 0; i < hearts.Count; i++)
            heartsProp.GetArrayElementAtIndex(i).objectReferenceValue = hearts[i];
        heartBarSo.ApplyModifiedPropertiesWithoutUndo();

        // Cat counter
        var catGo = CreateUIObject("CatCounter", root.transform);
        SetAnchors(catGo.GetComponent<RectTransform>(), new Vector2(0.6f, 0.78f), new Vector2(0.95f, 0.88f));
        var catLabel = catGo.AddComponent<Text>();
        catLabel.text = "0/0";
        catLabel.alignment = TextAnchor.MiddleCenter;
        catLabel.fontSize = 32;
        var catCounter = catGo.AddComponent<CatCounter>();
        var catSo = new SerializedObject(catCounter);
        catSo.FindProperty("_label").objectReferenceValue = catLabel;
        catSo.ApplyModifiedPropertiesWithoutUndo();

        // Action buttons
        var actionParent = CreateUIObject("ActionButtons", root.transform);
        SetAnchors(actionParent.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.12f));
        var hintButton    = CreateButton(actionParent.transform, "HintButton",    "Hint");
        var undoButton    = CreateButton(actionParent.transform, "UndoButton",    "Undo");
        var restartButton = CreateButton(actionParent.transform, "RestartButton", "Restart");

        // Booster buttons
        var boosterParent = CreateUIObject("BoosterButtons", root.transform);
        SetAnchors(boosterParent.GetComponent<RectTransform>(), new Vector2(0f, 0.12f), new Vector2(1f, 0.24f));
        var boosterHint      = CreateButton(boosterParent.transform, "BoosterHint",       "Hint");
        var boosterReveal    = CreateButton(boosterParent.transform, "BoosterRevealCell",  "Reveal");
        var boosterAddHeart  = CreateButton(boosterParent.transform, "BoosterAddHeart",   "+Heart");

        // Attach ViewGameplay and wire serialized fields
        var view = root.AddComponent<ViewGameplay>();
        var so = new SerializedObject(view);
        so.FindProperty("_levelLabel").objectReferenceValue      = levelLabel;
        so.FindProperty("_heartBar").objectReferenceValue        = heartBar;
        so.FindProperty("_catCounter").objectReferenceValue      = catCounter;
        so.FindProperty("_hintButton").objectReferenceValue      = hintButton;
        so.FindProperty("_undoButton").objectReferenceValue      = undoButton;
        so.FindProperty("_restartButton").objectReferenceValue   = restartButton;
        so.FindProperty("_boosterHint").objectReferenceValue     = boosterHint;
        so.FindProperty("_boosterRevealCell").objectReferenceValue = boosterReveal;
        so.FindProperty("_boosterAddHeart").objectReferenceValue = boosterAddHeart;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
        Object.DestroyImmediate(root);

        if (!success || prefab == null)
        {
            Debug.LogError($"[ViewGameplayCreator] Failed to save prefab at {PrefabPath}");
            return;
        }

        AssetDatabase.Refresh();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[ViewGameplayCreator] Prefab saved but Addressables not configured — mark it manually.");
            return;
        }

        var group = settings.FindGroup(AddressableGroup) ?? settings.DefaultGroup;
        string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
        var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        entry.address = AddressableKey;

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ViewGameplayCreator] Prefab saved at '{PrefabPath}', Addressable key = '{AddressableKey}'.");
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = CreateUIObject("Text", go.transform);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 24;

        return btn;
    }

    private static HeartIcon CreateHeartIcon(Transform parent, string name)
    {
        var go = CreateUIObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48f, 48f);
        var img = go.AddComponent<Image>();
        img.color = Color.red;
        var icon = go.AddComponent<HeartIcon>();
        var so = new SerializedObject(icon);
        so.FindProperty("_image").objectReferenceValue = img;
        so.ApplyModifiedPropertiesWithoutUndo();
        return icon;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }
}
