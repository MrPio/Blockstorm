using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Alteruna.Trinity;

namespace Alteruna
{
	public class RoomMenu : CommunicationBridge
	{
		[SerializeField] private Text TitleText;
		[SerializeField] private GameObject LANEntryPrefab;
		[SerializeField] private GameObject WANEntryPrefab;
		[SerializeField] private GameObject ContentContainer;
		[SerializeField] private Button StartButton;
		[SerializeField] private Button LeaveButton;

		public bool ShowUserCount = false;

		// manual refresh can be done by calling Multiplayer.RefreshRoomList();
		public bool AutomaticallyRefresh = true;
		public float RefreshInterval = 5.0f;

		private readonly List<RoomObject> _roomObjects = new List<RoomObject>();
		private float _refreshTime;

		private int _count;
		private string _connectionMessage = "Connecting";
		private float _statusTextTime;
		private int _roomI = -1;


		private void Start()
		{
			if (Multiplayer == null)
			{
				Multiplayer = FindObjectOfType<Multiplayer>();
			}

			if (Multiplayer == null)
			{
				Debug.LogError("Unable to find a active object of type Multiplayer.");
				if (TitleText != null) TitleText.text = "Missing Multiplayer Component";
				enabled = false;
			}
			else
			{
				Multiplayer.OnConnected.AddListener(Connected);
				Multiplayer.OnDisconnected.AddListener(Disconnected);
				Multiplayer.OnRoomListUpdated.AddListener(UpdateList);
				Multiplayer.OnRoomJoined.AddListener(JoinedRoom);
				Multiplayer.OnRoomLeft.AddListener(LeftRoom);

				StartButton.onClick.AddListener(() =>
				{
					// for more control, use Multiplayer.CreateRoom
					Multiplayer.JoinOnDemandRoom();
					_refreshTime = RefreshInterval;
				});

				LeaveButton.onClick.AddListener(() =>
				{
					Multiplayer.CurrentRoom?.Leave();
					_refreshTime = RefreshInterval;
				});

				if (TitleText != null)
				{
					ResponseCode blockedReason = Multiplayer.GetLastBlockResponse();
					if (blockedReason != ResponseCode.NaN)
					{
						string str = blockedReason.ToString();
						str = string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
						TitleText.text = str;
					}
					else
					{
						TitleText.text = "Connecting";
					}
				}

				// if already connected
				if (Multiplayer.IsConnected)
				{
					Connected(Multiplayer, null);
					return;
				}
			}

			StartButton.interactable = false;
			LeaveButton.interactable = false;
		}

		private void FixedUpdate()
		{
			if (!Multiplayer.enabled)
			{
				TitleText.text = "Offline";
			}
			else if (Multiplayer.IsConnected)
			{
				if (!AutomaticallyRefresh || (_refreshTime += Time.fixedDeltaTime) < RefreshInterval) return;
				_refreshTime -= RefreshInterval;

				Multiplayer.RefreshRoomList();

				if (TitleText == null) return;

				ResponseCode blockedReason = Multiplayer.GetLastBlockResponse();

				if (blockedReason == ResponseCode.NaN) return;

				string str = blockedReason.ToString();
				str = string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
				TitleText.text = str;
			}
			else if ((_statusTextTime += Time.fixedDeltaTime) >= 1)
			{
				_statusTextTime -= 1;
				ResponseCode blockedReason = Multiplayer.GetLastBlockResponse();
				if (blockedReason != ResponseCode.NaN)
				{
					string str = blockedReason.ToString();
					str = string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
					TitleText.text = str;
					return;
				}

				switch (_count)
				{
					case 0:
						TitleText.text = _connectionMessage + ".  ";
						break;
					case 1:
						TitleText.text = _connectionMessage + ".. ";
						break;
					default:
						TitleText.text = _connectionMessage + "...";
						_count = -1;
						break;
				}

				_count++;
			}
		}

		public bool JoinRoom(string roomName, ushort password = 0)
		{
			roomName = roomName.ToLower();
			if (Multiplayer != null && Multiplayer.IsConnected)
			{
				foreach (var room in Multiplayer.AvailableRooms)
				{
					if (room.Name.ToLower() == roomName)
					{
						room.Join(password);
						return true;
					}
				}
			}

			return false;
		}

		private void Connected(Multiplayer multiplayer, Endpoint endpoint)
		{
			// if already connected to room
			if (multiplayer.InRoom)
			{
				JoinedRoom(multiplayer, multiplayer.CurrentRoom, multiplayer.Me);
				return;
			}

			StartButton.interactable = true;
			LeaveButton.interactable = false;

			if (TitleText != null)
			{
				TitleText.text = "Rooms";
			}
		}

		private void Disconnected(Multiplayer multiplayer, Endpoint endPoint)
		{
			StartButton.interactable = false;
			LeaveButton.interactable = false;

			_connectionMessage = "Reconnecting";
			if (TitleText != null)
			{
				TitleText.text = "Reconnecting";
			}
		}

		private void JoinedRoom(Multiplayer multiplayer, Room room, User user)
		{
			StartButton.interactable = false;
			LeaveButton.interactable = true;

			if (TitleText != null)
			{
				TitleText.text = "In Room " + room.Name;
			}
		}

		private void LeftRoom(Multiplayer multiplayer)
		{
			_roomI = -1;

			StartButton.interactable = true;
			LeaveButton.interactable = false;

			if (TitleText != null)
			{
				TitleText.text = "Rooms";
			}
		}

		private void UpdateList(Multiplayer multiplayer)
		{
			if (ContentContainer == null) return;

			RemoveExtraRooms(multiplayer);

			for (int i = 0; i < multiplayer.AvailableRooms.Count; i++)
			{
				Room room = multiplayer.AvailableRooms[i];
				RoomObject entry;

				if (_roomObjects.Count > i)
				{
					if (room.Local != _roomObjects[i].Lan)
					{
						Destroy(_roomObjects[i].GameObject);
						entry = new RoomObject(Instantiate(WANEntryPrefab, ContentContainer.transform), room.ID, room.Local);
						_roomObjects[i] = entry;
					}
					else
					{
						entry = _roomObjects[i];
						entry.Button.onClick.RemoveAllListeners();
					}
				}
				else
				{
					entry = new RoomObject(Instantiate(WANEntryPrefab, ContentContainer.transform), room.ID, room.Local);
					_roomObjects.Add(entry);
				}

				if (
					// Hide private rooms.
					room.InviteOnly && room.ID != _roomI ||
					// Hide locked rooms.
					room.IsLocked ||
					// Hide full rooms.
					room.GetUserCount() > room.MaxUsers
				)
				{
					entry.GameObject.SetActive(false);
					entry.GameObject.name = room.Name;
					continue;
				}

				string newName = room.Name;
				if (ShowUserCount)
				{
					newName += " (" + room.GetUserCount() + "/" + room.MaxUsers + ")";
				}

				if (entry.GameObject.name != newName)
				{
					entry.GameObject.name = newName;
					entry.Text.text = newName;
				}

				entry.GameObject.SetActive(true);

				if (room.ID == _roomI)
				{
					entry.Button.interactable = false;
				}
				else
				{
					entry.Button.interactable = true;
					entry.Button.onClick.AddListener(() =>
					{
						room.Join();
						UpdateList(multiplayer);
					});
				}
			}
		}

		private void RemoveExtraRooms(Multiplayer multiplayer)
		{
			int l = _roomObjects.Count;
			if (multiplayer.AvailableRooms.Count < l)
			{
				for (int i = 0; i < l; i++)
				{
					if (multiplayer.AvailableRooms.All(t => t.ID != _roomObjects[i].ID))
					{
						Destroy(_roomObjects[i].GameObject);
						_roomObjects.RemoveAt(i);
						i--;
						l--;
						if (multiplayer.AvailableRooms.Count >= l) return;
					}
				}
			}
		}

		public new void Reset()
		{
			base.Reset();
			EnsureEventSystem.Ensure(true);
		}

		private struct RoomObject
		{
			public readonly GameObject GameObject;
			public readonly Text Text;
			public readonly Button Button;
			public readonly uint ID;
			public readonly bool Lan;

			public RoomObject(GameObject obj, uint id, bool lan = false)
			{
				GameObject = obj;
				Text = obj.GetComponentInChildren<Text>();
				Button = obj.GetComponentInChildren<Button>();
				ID = id;
				Lan = lan;
			}
		}
	}
}