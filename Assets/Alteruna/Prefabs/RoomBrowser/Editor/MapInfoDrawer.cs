using System;
using Alteruna;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MapInfo))]
public class MapInfoDrawer : PropertyDrawer
{
	private const float spacing = 2f;


	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		float totalHeight = EditorGUIUtility.singleLineHeight + spacing;

		if (property.isExpanded)
		{
			totalHeight += (EditorGUIUtility.singleLineHeight + spacing) * 4 + spacing + EditorGUIUtility.singleLineHeight * 4;
		}

		return totalHeight;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		EditorGUI.indentLevel = 0;

		SerializedProperty hideProperty = property.FindPropertyRelative("Hidden");
		SerializedProperty imageProperty = property.FindPropertyRelative("Image");
		SerializedProperty titleProperty = property.FindPropertyRelative("Title");
		SerializedProperty descriptionProperty = property.FindPropertyRelative("Description");

		position.height = EditorGUIUtility.singleLineHeight;
		property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
		position.y += EditorGUIUtility.singleLineHeight + spacing;

		if (property.isExpanded)
		{
			GUIContent hideLabel = new GUIContent("Hidden", "Marks if this element should be hidden or not.\n" +
				"Used by the CreateRoomMenu component to hide scenes in build settings that shouldn't be selectable");
			EditorGUI.PropertyField(position, hideProperty, hideLabel);

			position.y += EditorGUIUtility.singleLineHeight + spacing;
			DrawSceneSelector(position, property);

			position.y += EditorGUIUtility.singleLineHeight + spacing;
			EditorGUI.PropertyField(position, titleProperty);

			position.y += EditorGUIUtility.singleLineHeight + spacing;
			EditorGUI.PropertyField(position, imageProperty);

			position.y += EditorGUIUtility.singleLineHeight + spacing;
			position.height = EditorGUIUtility.singleLineHeight * 4;
			EditorGUI.PropertyField(position, descriptionProperty);
		}

		EditorGUI.EndProperty();
	}

	private void DrawSceneSelector(Rect position, SerializedProperty property)
	{
		var scenes = EditorBuildSettings.scenes;
		string[] sceneNames = new string[scenes.Length];
		GUIContent[] newSceneArray = new GUIContent[scenes.Length];

		for (int i = 0, enabledIndex = 0; i < scenes.Length; i++)
		{
			if (scenes[i].enabled)
			{
				sceneNames[i] = scenes[i].Name();
				newSceneArray[i] = new GUIContent($"[{enabledIndex}] {sceneNames[i]}", sceneNames[i]);
				enabledIndex++;
			}
			else
			{
				sceneNames[i] = scenes[i].Name();
				newSceneArray[i] = new GUIContent($"[No Build Index] {sceneNames[i]}", sceneNames[i]);
			}
		}

		SerializedProperty _scenesProp = property.FindPropertyRelative("SceneName");
		string sceneName = _scenesProp.stringValue;

		if (!EditorBuildSettings.scenes.GetSceneIndexByName(sceneName, out int currentSceneIndex))
		{
			Array.Resize(ref newSceneArray, newSceneArray.Length + 1);
			currentSceneIndex = newSceneArray.Length - 1;
			newSceneArray[currentSceneIndex] = new GUIContent($"[Scene Not Found] {sceneName}", sceneName);
		}

		// Dropdown for scene selection
		GUIContent sceneDropdownLabel = new GUIContent("Scene", "What scene to load");
		int selectedSceneIndex = EditorGUI.Popup(position, sceneDropdownLabel, currentSceneIndex, newSceneArray);
		if (selectedSceneIndex != currentSceneIndex)
		{
			_scenesProp.stringValue = sceneNames[selectedSceneIndex];
		}
	}
}
