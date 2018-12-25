using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ShortcutsWindow : EditorWindow 
{
	private static readonly GUILayoutOption[] DEFAULT_GUI_BUTTON_OPTIONS =
	{
		GUILayout.ExpandWidth(true),
		GUILayout.MaxWidth(200),
		GUILayout.Height(24)
	};

	private SceneAsset _loadingScene;
	private string _loadingScenePath;
	private string _cachedScene;
	private bool _pressedPlay = false;
	
		
	[MenuItem("Window/Shortcuts")]
	public static void ShowWindow()
	{
		var window = GetWindow(typeof(ShortcutsWindow), false, "Shortcuts");
		window.autoRepaintOnSceneChange = true;
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

	private bool ShouldOpenCachedScene()
	{
		return !(_pressedPlay || string.IsNullOrEmpty(_cachedScene));
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
		_loadingScenePath = AssetDatabase.GetAssetPath(_loadingScene);
		if (!Application.isPlaying)
		{
			if (ShouldOpenCachedScene())
			{
				EditorSceneManager.OpenScene(_cachedScene);
				_cachedScene = null;
			}
			
			GUI.enabled = !_pressedPlay;
			DrawEditorButton(
				"Play from Loading Scene",
				() =>
				{
					_pressedPlay = true;
					_cachedScene = EditorApplication.currentScene;
					EditorSceneManager.OpenScene(_loadingScenePath);
					EditorApplication.isPlaying = true;
				}
			);
		}
		else
		{
			GUI.enabled = _pressedPlay;
			DrawEditorButton(
				"Exit Play Mode",
				() =>
				{
					_pressedPlay = false;
					EditorApplication.isPlaying = false;
				}
			);
		}

		EditorGUILayout.Space();
		
		GUILayout.EndVertical();
	}
}
