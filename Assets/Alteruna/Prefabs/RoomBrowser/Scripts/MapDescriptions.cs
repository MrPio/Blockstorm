using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine.SceneManagement;
#endif

namespace Alteruna
{
	[Serializable]
	public class MapInfo
	{
		[NonSerialized]
		public int BuildIndex = -1;

		public bool Hidden = false;

		[HideInInspector]
		public string SceneName;
		public string ScenePath;

		public Sprite Image;
		public string Title = "New Map Info";
		[TextArea(1, 6)]
		public string Description;

		public MapInfo(int buildIndex, string description, Sprite image = null)
		{
			BuildIndex = buildIndex;
#if UNITY_EDITOR
			ScenePath = EditorBuildSettings.scenes[buildIndex].path;
			SceneName = System.IO.Path.GetFileNameWithoutExtension(ScenePath);
#endif
			Title = SceneName;
			Image = image;
			Description = description;
		}
	}


	public class MapDescriptions : ScriptableObject
	{
#region PATHS
		private const string RESOURCE_NAME = "MapDescriptions";
		private const string RESOURCE_DIRECTORY = "Assets/Resources";
		private const string RESOURCE_PATH = RESOURCE_DIRECTORY + "/" + RESOURCE_NAME + ".asset";
#endregion
		static MapDescriptions _instance;
		public static MapDescriptions Instance => GetInstance();

		public bool ChangeSceneOnRoomJoined = true;
		public Sprite DefaultImage;

		[SerializeField]
		private List<MapInfo> DescriptionItems = new List<MapInfo>();


		public List<MapInfo> GetValidMapDescriptions(bool getHiddenDescriptions = false)
		{
			return DescriptionItems.Where(d => d.BuildIndex >= 0 && (getHiddenDescriptions || !d.Hidden))
				.GroupBy(d => d.BuildIndex)
				.Select(d => d.First())
			.ToList();
		}

		public MapInfo GetMapDescription(int sceneIndex)
		{
			MapInfo info = DescriptionItems.FirstOrDefault(d => d.BuildIndex == sceneIndex);

			if (info == null)
			{
				info = new MapInfo(sceneIndex, "Description missing.");
				DescriptionItems.Add(info);
#if UNITY_EDITOR
				MarkAsDirty();
#endif
			}

			return info;
		}

		/// <summary>
		/// Returns the custom <b>title</b> of the scene with matching build index.
		/// </summary>
		/// <remarks>Note:<br></br>
		/// The title of a scene can be set in the <see cref="MapDescriptions"/> file in the resources folder
		/// </remarks>
		/// <returns>Title of scene if buildIndex is valid. Null otherwise</returns>
		public string GetSceneTitleByIndex(int buildIndex)
		{
			var item = DescriptionItems.FirstOrDefault(d => d.BuildIndex == buildIndex);
			return item != null ? item.Title : null;
		}

#if UNITY_EDITOR
		public void PopulateScenesIntoList()
		{
			bool buildSettingsContainsInvalidScenes = false;
			DescriptionItems.Clear();

			for (int i = 0, l = EditorBuildSettings.scenes.Length; i < l; i++)
			{
				if (!File.Exists(EditorBuildSettings.scenes[i].path))
				{
					buildSettingsContainsInvalidScenes = true;
					continue;
				}

				MapInfo info = new MapInfo(i, "Description missing.");
				DescriptionItems.Add(info);
			}

			if (buildSettingsContainsInvalidScenes)
			{
				Debug.LogWarning($"Warning! Build settings refers to one or more scenes that couldn't be found.");
			}

			MarkAsDirty();
		}

		private void OnEnable()
		{
			EditorBuildSettings.sceneListChanged += SceneListChanged;
		}

		private void OnDisable()
		{
			EditorBuildSettings.sceneListChanged -= SceneListChanged;
		}

		private void OnValidate()
		{
			EditorBuildSettings.scenes.AssignIndexesToScenes(DescriptionItems);
		}

		private void SceneListChanged()
		{
			EditorBuildSettings.scenes.AssignIndexesToScenes(DescriptionItems);
		}

		public void Reset()
		{
			HandleDefaultImage();
			PopulateScenesIntoList();
		}

		private void HandleDefaultImage()
		{
			if (DefaultImage == null)
			{
				DefaultImage = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Alteruna/Media/Alteruna Logo.png");
			}
		}
#endif

		private static MapDescriptions GetInstance()
		{
			if (_instance != null) return _instance;

			_instance = Resources.Load<MapDescriptions>(RESOURCE_NAME);
			if (_instance == null)
			{
				_instance = CreateInstance<MapDescriptions>();
#if UNITY_EDITOR
				if (!Directory.Exists(RESOURCE_DIRECTORY))
				{
					Directory.CreateDirectory(RESOURCE_DIRECTORY);
				}
				AssetDatabase.CreateAsset(_instance, RESOURCE_PATH);

				_instance.PopulateScenesIntoList();
#endif
			}

#if UNITY_EDITOR
			if (_instance.DescriptionItems.Count <= 0)
			{
				_instance.PopulateScenesIntoList();
			}
			_instance.HandleDefaultImage();
#else
			foreach (var item in _instance.DescriptionItems)
			{
				item.BuildIndex = SceneUtility.GetBuildIndexByScenePath(item.ScenePath);
			}
#endif

			return _instance;
		}
#if UNITY_EDITOR
		void MarkAsDirty()
		{
			EditorUtility.SetDirty(Instance);
		}
#endif
	}


#if UNITY_EDITOR
	public static class EditorBuildSettingsSceneExtensions
	{
		public static string Name(this EditorBuildSettingsScene scene)
		{
			return System.IO.Path.GetFileNameWithoutExtension(scene.path);
		}

		public static void AssignIndexesToScenes(this EditorBuildSettingsScene[] scenes, List<MapInfo> mapInfos)
		{
			foreach (var info in mapInfos)
			{
				info.BuildIndex = -1;
			}

			int negator = 0;
			for (int i = 0, l = scenes.Length; i < l; i++)
			{
				if (!scenes[i].enabled)
				{
					negator++;
				}

				var infos = mapInfos.Where(m => m.SceneName == scenes[i].Name()).ToList();
				foreach (var info in infos)
				{
					if (info != null)
					{
						info.BuildIndex = scenes[i].enabled ? i - negator : -1;
					}
				}
			}
		}

		public static bool GetSceneIndexByName(this EditorBuildSettingsScene[] scenes, string name, out int index)
		{
			index = -1;

			var scenesAndIndexes = scenes
				.Select((scene, i) => new { Scene = scene, Index = i })
				.Where(s => s.Scene.Name() == name)
				.ToList();

			if (scenesAndIndexes.Count > 1)
			{
				Debug.LogError($"Found more than one scene called '{name}'! Consider giving your scenes unique names.");
				index = scenesAndIndexes[0].Index;
			}
			else if (scenesAndIndexes.Count <= 0)
			{
				Debug.LogError($"Couldn't find any scene called '{name}'! Have you added the scene in the build settings menu?");
				return false;
			}
			else
			{
				index = scenesAndIndexes[0].Index;
			}

			return true;
		}
	}
#endif
}
