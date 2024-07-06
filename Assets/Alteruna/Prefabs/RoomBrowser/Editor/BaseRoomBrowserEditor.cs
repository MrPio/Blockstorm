using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Alteruna
{
	[CustomEditor(typeof(BaseRoomBrowser), true)]
	public class BaseRoomBrowserEditor : Editor
	{
		private bool _oldChangeSceneOnRoomJoined;

		private void OnEnable()
		{
			_oldChangeSceneOnRoomJoined = MapDescriptions.Instance.ChangeSceneOnRoomJoined;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawScriptProperty();

			string nicifiedName = ObjectNames.NicifyVariableName(nameof(MapDescriptions.Instance.ChangeSceneOnRoomJoined));
			MapDescriptions.Instance.ChangeSceneOnRoomJoined = EditorGUILayout.Toggle(nicifiedName, MapDescriptions.Instance.ChangeSceneOnRoomJoined);

			if (_oldChangeSceneOnRoomJoined != MapDescriptions.Instance.ChangeSceneOnRoomJoined)
			{
				_oldChangeSceneOnRoomJoined = MapDescriptions.Instance.ChangeSceneOnRoomJoined;
				EditorUtility.SetDirty(MapDescriptions.Instance);
			}

			if (target.GetType() != typeof(BaseRoomBrowser) && MapDescriptions.Instance.ChangeSceneOnRoomJoined)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space(10, false);
				SerializedProperty spawnPlayerOnLoadProperty = serializedObject.FindProperty(nameof(BaseRoomBrowser.SpawnAvatarAfterLoad));
				EditorGUILayout.PropertyField(spawnPlayerOnLoadProperty, true);
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();

			DrawPropertiesExcluding(serializedObject, "m_Script");

			EditorGUILayout.Space(10);

			if (GUILayout.Button("Reset Map Descriptions"))
			{
				if (EditorUtility.DisplayDialog("Do You Want To Reset Your Map Descriptions?",
				"WARNING!\nThis resets all changes you may have made to your MapDescription file. Please make sure to back up your descriptions before proceeding.",
				"Continue", "Cancel"))
				{
					MapDescriptions.Instance.Reset();
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		void DrawScriptProperty()
		{
			SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(scriptProperty, true);
			EditorGUI.EndDisabledGroup();
		}
	}
}
