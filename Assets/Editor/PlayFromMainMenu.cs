#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Automatically loads MainMenu scene when Play is pressed in the Editor,
/// regardless of which scene is currently open.
/// Toggle via menu: Game > Play From Main Menu (checkmark shows active state).
/// </summary>
[InitializeOnLoad]
public static class PlayFromMainMenu
{
    const string MenuPath    = "Game/Play From Main Menu";
    const string PrefKey     = "PlayFromMainMenu_Enabled";
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMainMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static bool IsEnabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath, priority = 200)]
    static void Toggle()
    {
        IsEnabled = !IsEnabled;
        Menu.SetChecked(MenuPath, IsEnabled);
        UnityEngine.Debug.Log($"[PlayFromMainMenu] {(IsEnabled ? "Enabled" : "Disabled")} — Play will {(IsEnabled ? "" : "NOT ")}start from MainMenu.");
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, IsEnabled);
        return true;
    }

    static string _previousScene;

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!IsEnabled) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Save current open scene path so we can restore it after Play
            _previousScene = EditorSceneManager.GetActiveScene().path;

            // If we're already in MainMenu, nothing to do
            if (_previousScene == MainMenuPath) return;

            // Save any unsaved changes
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // User cancelled — abort Play
                EditorApplication.isPlaying = false;
                return;
            }

            // Tell Unity to start playing with MainMenu
            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Clear override so normal Editor behavior resumes
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
#endif
