using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alteruna
{
	public enum GameMode : byte
	{
		FreeForAll,
		Teams
	}

	public class CustomRoomInfo
	{
		public string RoomName { get; set; } = "";
		public GameMode GameMode { get; set; }
		public int SceneIndex { get; set; }
	}
}
