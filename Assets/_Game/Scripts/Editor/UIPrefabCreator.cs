using System.IO;
using Cast.Game;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

public static class UIPrefabCreator
{
    private const string ViewDir = "Assets/_Game/Prefabs/UI/Views";
    private const string PopupDir = "Assets/_Game/Prefabs/UI/Popups";
    private const string AddressableGroup = "Default Local Group";

    [MenuItem("Tools/Setup Addressables/UI/Create All UI Prefabs")]
    public static void CreateAll()
    {
        CreateViewHome();
        CreatePopupSettings();
        CreatePopupPause();
        CreatePopupWin();
        CreatePopupLose();
    }

    [MenuItem("Tools/Setup Addressables/UI/Create ViewHome")]
    public static void CreateViewHome()
    {
        var root = NewRoot("ViewHome");

        var levelLabel = CreateLabel(root.transform, "LevelLabel", "Level 1", 40,
            new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.95f));
        var coinLabel = CreateLabel(root.transform, "CoinLabel", "0", 32,
            new Vector2(0.6f, 0.92f), new Vector2(0.98f, 1f));

        var playButton = CreateButton(root.transform, "PlayButton", "Play",
            new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.42f));
        var settingsButton = CreateButton(root.transform, "SettingsButton", "Settings",
            new Vector2(0.35f, 0.16f), new Vector2(0.65f, 0.26f));

        var view = root.AddComponent<ViewHome>();
        var so = new SerializedObject(view);
        so.FindProperty("_levelLabel").objectReferenceValue = levelLabel;
        so.FindProperty("_coinLabel").objectReferenceValue = coinLabel;
        so.FindProperty("_playButton").objectReferenceValue = playButton;
        so.FindProperty("_settingsButton").objectReferenceValue = settingsButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        SaveAndRegister(root, ViewDir, "ViewHome");
    }

    [MenuItem("Tools/Setup Addressables/UI/Create PopupSettings")]
    public static void CreatePopupSettings()
    {
        var root = NewRoot("PopupSettings");
        var window = CreateWindow(root.transform, "Settings");

        var musicButton = CreateButton(window.transform, "MusicButton", "Music: On",
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.7f));
        var sfxButton = CreateButton(window.transform, "SfxButton", "Sound: On",
            new Vector2(0.1f, 0.37f), new Vector2(0.9f, 0.52f));
        var closeButton = CreateButton(window.transform, "CloseButton", "Close",
            new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.27f));

        var popup = root.AddComponent<PopupSettings>();
        var so = new SerializedObject(popup);
        so.FindProperty("_musicButton").objectReferenceValue = musicButton;
        so.FindProperty("_musicLabel").objectReferenceValue = musicButton.GetComponentInChildren<Text>();
        so.FindProperty("_sfxButton").objectReferenceValue = sfxButton;
        so.FindProperty("_sfxLabel").objectReferenceValue = sfxButton.GetComponentInChildren<Text>();
        so.FindProperty("_closeButton").objectReferenceValue = closeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        SaveAndRegister(root, PopupDir, "PopupSettings");
    }

    [MenuItem("Tools/Setup Addressables/UI/Create PopupPause")]
    public static void CreatePopupPause()
    {
        var root = NewRoot("PopupPause");
        var window = CreateWindow(root.transform, "Paused");

        var resumeButton = CreateButton(window.transform, "ResumeButton", "Resume",
            new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.72f));
        var restartButton = CreateButton(window.transform, "RestartButton", "Restart",
            new Vector2(0.1f, 0.46f), new Vector2(0.9f, 0.58f));
        var settingsButton = CreateButton(window.transform, "SettingsButton", "Settings",
            new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.44f));
        var homeButton = CreateButton(window.transform, "HomeButton", "Home",
            new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.3f));

        var popup = root.AddComponent<PopupPause>();
        var so = new SerializedObject(popup);
        so.FindProperty("_resumeButton").objectReferenceValue = resumeButton;
        so.FindProperty("_restartButton").objectReferenceValue = restartButton;
        so.FindProperty("_settingsButton").objectReferenceValue = settingsButton;
        so.FindProperty("_homeButton").objectReferenceValue = homeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        SaveAndRegister(root, PopupDir, "PopupPause");
    }

    [MenuItem("Tools/Setup Addressables/UI/Create PopupWin")]
    public static void CreatePopupWin()
    {
        var root = NewRoot("PopupWin");
        var window = CreateWindow(root.transform, "");

        var titleLabel = CreateLabel(window.transform, "TitleLabel", "You Win!", 44,
            new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));
        var heartsLabel = CreateLabel(window.transform, "HeartsLabel", "Hearts left: 0", 28,
            new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.74f));
        var movesLabel = CreateLabel(window.transform, "MovesLabel", "Moves: 0", 28,
            new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.62f));

        var nextButton = CreateButton(window.transform, "NextButton", "Next",
            new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.44f));
        var replayButton = CreateButton(window.transform, "ReplayButton", "Replay",
            new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.3f));
        var homeButton = CreateButton(window.transform, "HomeButton", "Home",
            new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.16f));

        var popup = root.AddComponent<PopupWin>();
        var so = new SerializedObject(popup);
        so.FindProperty("_titleLabel").objectReferenceValue = titleLabel;
        so.FindProperty("_heartsLabel").objectReferenceValue = heartsLabel;
        so.FindProperty("_movesLabel").objectReferenceValue = movesLabel;
        so.FindProperty("_nextButton").objectReferenceValue = nextButton;
        so.FindProperty("_replayButton").objectReferenceValue = replayButton;
        so.FindProperty("_homeButton").objectReferenceValue = homeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        SaveAndRegister(root, PopupDir, "PopupWin");
    }

    [MenuItem("Tools/Setup Addressables/UI/Create PopupLose")]
    public static void CreatePopupLose()
    {
        var root = NewRoot("PopupLose");
        var window = CreateWindow(root.transform, "");

        var titleLabel = CreateLabel(window.transform, "TitleLabel", "You Lose", 44,
            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.92f));
        var messageLabel = CreateLabel(window.transform, "MessageLabel", "Moves: 0", 28,
            new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.68f));

        var retryButton = CreateButton(window.transform, "RetryButton", "Retry",
            new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.46f));
        var homeButton = CreateButton(window.transform, "HomeButton", "Home",
            new Vector2(0.1f, 0.14f), new Vector2(0.9f, 0.28f));

        var popup = root.AddComponent<PopupLose>();
        var so = new SerializedObject(popup);
        so.FindProperty("_titleLabel").objectReferenceValue = titleLabel;
        so.FindProperty("_messageLabel").objectReferenceValue = messageLabel;
        so.FindProperty("_retryButton").objectReferenceValue = retryButton;
        so.FindProperty("_homeButton").objectReferenceValue = homeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        SaveAndRegister(root, PopupDir, "PopupLose");
    }

    private static GameObject NewRoot(string name)
    {
        var root = new GameObject(name);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return root;
    }

    private static GameObject CreateWindow(Transform parent, string title)
    {
        var backdrop = CreateUIObject("Backdrop", parent);
        SetAnchors(backdrop.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var backdropImg = backdrop.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.6f);

        var window = CreateUIObject("Window", parent);
        SetAnchors(window.GetComponent<RectTransform>(), new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.75f));
        var windowImg = window.AddComponent<Image>();
        windowImg.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        if (!string.IsNullOrEmpty(title))
            CreateLabel(window.transform, "TitleLabel", title, 40,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.98f));

        return window;
    }

    private static Text CreateLabel(Transform parent, string name, string content, int fontSize, Vector2 min, Vector2 max)
    {
        var go = CreateUIObject(name, parent);
        SetAnchors(go.GetComponent<RectTransform>(), min, max);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = Color.black;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        var go = CreateUIObject(name, parent);
        SetAnchors(go.GetComponent<RectTransform>(), min, max);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.9f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = CreateUIObject("Text", go.transform);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 28;
        text.color = Color.white;

        return btn;
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

    private static void SaveAndRegister(GameObject root, string dir, string key)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = $"{dir}/{key}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        Object.DestroyImmediate(root);

        if (!success || prefab == null)
        {
            Debug.LogError($"[UIPrefabCreator] Failed to save prefab at {path}");
            return;
        }

        AssetDatabase.Refresh();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning($"[UIPrefabCreator] '{key}' saved but Addressables not configured — mark it manually.");
            return;
        }

        var group = settings.FindGroup(AddressableGroup) ?? settings.DefaultGroup;
        string guid = AssetDatabase.AssetPathToGUID(path);
        var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        entry.address = key;

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[UIPrefabCreator] Prefab saved at '{path}', Addressable key = '{key}'.");
    }
}
