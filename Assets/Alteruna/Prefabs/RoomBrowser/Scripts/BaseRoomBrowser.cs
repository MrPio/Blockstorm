using UnityEngine;

namespace Alteruna
{
	/// <summary>
	/// Renders partial information from <see cref="MapDescriptions"/>
	/// </summary>
	public class BaseRoomBrowser : CommunicationBridge
	{
		[HideInInspector, Tooltip("Should avatar be spawned automatically after scene is loaded through this component?")]
		public bool SpawnAvatarAfterLoad = true;
	}
}