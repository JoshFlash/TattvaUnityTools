using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class ShortcutsWindow : EditorWindow 
{
	static ShortcutsWindow()
	{
		EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
	}
	
	[MenuItem("Window/Shortcuts")]
	public static void ShowWindow()
	{
		var window = GetWindow(typeof(ShortcutsWindow), false, "Shortcuts");
		window.autoRepaintOnSceneChange = true;
	}
	
	private const string CACHED_SCENE_KEY = "ShortcutsCachedScene";
	private const string LOADING_SCENE_KEY = "ShortcutsLoadingScene";
	private const string AUTO_LOAD_KEY = "ShortcutsAutoLoad";
	
	private static readonly GUILayoutOption[] DEFAULT_GUI_BUTTON_OPTIONS =
	{
		GUILayout.ExpandWidth(true),
		GUILayout.MaxWidth(200),
		GUILayout.Height(24)
	};

	private SceneAsset _loadingScene;
	private bool _automaticallyLoadScenes;
	
	private static void HandlePlayModeStateChanged(PlayModeStateChange playModeStateChange)
	{
		if (EditorPrefs.GetBool(AUTO_LOAD_KEY, false))
		{
			if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
			{
				string cachedScene = EditorPrefs.GetString(CACHED_SCENE_KEY);
				if (!string.IsNullOrEmpty(cachedScene))
				{
					EditorSceneManager.OpenScene(cachedScene);
				}
			}
			else if (playModeStateChange == PlayModeStateChange.ExitingEditMode)
			{
				EditorPrefs.SetString(CACHED_SCENE_KEY, EditorApplication.currentScene);
				string loadingScenePath = EditorPrefs.GetString(LOADING_SCENE_KEY);
				if (!string.IsNullOrEmpty(loadingScenePath))
				{
					EditorSceneManager.OpenScene(loadingScenePath);
				}
			}
		}
	}

	private void DrawEditorButton(string text, Action buttonAction, bool useDefaultLayoutOptions = true, bool isCentered = true, params GUILayoutOption[] options)
	{
		if (isCentered)
		{
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
		}

		try
		{
			if (GUILayout.Button(text, useDefaultLayoutOptions ? DEFAULT_GUI_BUTTON_OPTIONS : options))
			{
				buttonAction.Invoke();
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError("Unable to execute action on button " + text + "\n" + e.Message);
		}

		if (isCentered)
		{
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
	}

	private void ShowProjectSettings(string settingsType, string settingsSuffix = "Settings")
	{
#if UNITY_2018_1_OR_NEWER
		Selection.activeObject = Unsupported.GetSerializedAssetInterfaceSingleton(settingsType + settingsSuffix);
#else
		EditorApplication.ExecuteMenuItem("Edit/Project Settings/" + settingsType);
#endif
	}
	
	private void OnGUI()
	{
		DrawSettingsShortcuts();
		
		EditorGUILayout.Space();
		
		DrawLoadingSceneShortcut();
	}

	private void DrawSettingsShortcuts()
	{
		GUILayout.BeginVertical("box");
		
		EditorGUILayout.Space();
		
		GUILayout.BeginHorizontal();
		DrawEditorButton(
			"Show Player Settings",
			() => ShowProjectSettings("Player")
		);

		EditorGUILayout.Space();
		DrawEditorButton(
			"Show Editor Settings",
			() => ShowProjectSettings("Editor")
		);
		GUILayout.EndHorizontal();
		
		EditorGUILayout.Space();
		
		GUILayout.BeginHorizontal();
		DrawEditorButton(
			"Show Quality Settings",
			() => ShowProjectSettings("Quality")
		);
		
		EditorGUILayout.Space();
		DrawEditorButton(
			"Show Physics Settings",
			() => ShowProjectSettings("Physics", "Manager")
		);
		GUILayout.EndHorizontal();
		
		EditorGUILayout.Space();
		
		GUILayout.EndVertical();
	}

	private void DrawLoadingSceneShortcut()
	{
		GUILayout.BeginVertical("box");
		
		EditorGUILayout.Space();
		
		_loadingScene = EditorGUILayout.ObjectField("Loading Scene", _loadingScene, typeof(SceneAsset), true) as SceneAsset;
		if (_loadingScene != null)
		{
			EditorPrefs.SetString(LOADING_SCENE_KEY, AssetDatabase.GetAssetPath(_loadingScene));
		}

		_automaticallyLoadScenes = EditorGUILayout.Toggle("Automatically Load Scenes", _automaticallyLoadScenes);
		EditorPrefs.SetBool(AUTO_LOAD_KEY, _automaticallyLoadScenes);
		
		EditorGUILayout.Space();
		
		GUILayout.EndVertical();
	}
}

