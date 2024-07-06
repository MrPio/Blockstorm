using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Alteruna
{
	public class RoomBrowser : BaseRoomBrowser
	{
		public enum Column
		{
			None,
			Name,
			GameMode,
			Players,
			Map,
			Password
		}


		public UnityEvent OnRowsSorted { get; set; } = new UnityEvent();

		[Tooltip("Time in seconds to wait before allowing a player to refresh the room list. Should be fairly high to save data usage.")]
		[SerializeField, Range(0, 60)] private int _refreshDelay = 10;

		[Tooltip("The column to sort rooms by.")]
		[SerializeField] private Column _sortColumn = Column.None;

		[Tooltip("The parent where the rows will be instantiated in.")]
		[SerializeField] private Transform _rowsContainer;
		[Tooltip("The prefab that's instantiated to represent a room.")]
		[SerializeField] private RoomBrowserRow _roomBrowserRowPrefab;

		[Tooltip("The button to press to refresh the room list.")]
		[SerializeField] private Button _buttonRefresh;
		[Tooltip("The button to press to join a selected room.")]
		[SerializeField] private Button _buttonJoin;
		[Tooltip("The button to press to leave the current room.")]
		[SerializeField] private Button _buttonLeave;

		[Tooltip("The popup menu to display when joining a password protected room.")]
		[SerializeField] private GameObject _passwordPopupMenu;

		[Tooltip("The text to display the invite code on for a private room.")]
		[SerializeField] private TMP_Text _textInviteCode;


		[Header("Filters")]

		[Tooltip("The input to search for rooms by name.")]
		[SerializeField] private TMP_InputField _inputSearch;

		[Tooltip("The toggle for hiding password protected rooms.")]
		[SerializeField] private Toggle _toggleHidePasswordRooms;
		[Tooltip("The toggle for hiding rooms that are full.")]
		[SerializeField] private Toggle _toggleHideFullRooms;


		private List<RoomBrowserRow> _rowsPool = new List<RoomBrowserRow>();
		private List<RoomBrowserRow> _activeRows = new List<RoomBrowserRow>();
		private List<RoomBrowserRow> _filteredRows = new List<RoomBrowserRow>();
		private RoomBrowserRow _selectedRow;

		private float _refreshTimer = 0;
		private bool _sortDescending = false;


		private new void OnEnable()
		{
			base.OnEnable();

			Multiplayer.OnRoomJoined.AddListener(JoinedRoom);
			CheckCanJoinRoom();
			Multiplayer.RefreshRoomList();
		}

		private void OnDisable()
		{
			Multiplayer.OnRoomJoined.RemoveListener(JoinedRoom);
		}

		private void Start()
		{
			_buttonJoin.interactable = false;

			_roomBrowserRowPrefab.gameObject.SetActive(false);
			_buttonRefresh.onClick.AddListener(Refresh);
			_buttonJoin.onClick.AddListener(JoinRoom);
			_buttonLeave.onClick.AddListener(LeaveRoom);

			if (Multiplayer == null)
			{
				Debug.LogException(new NullReferenceException("Missing Multiplayer Component!"), gameObject);
				gameObject.SetActive(false);
			}
			else
			{
				_inputSearch.onValueChanged.AddListener(_ => FilterChanged());
				_toggleHidePasswordRooms.onValueChanged.AddListener(_ => FilterChanged());
				_toggleHideFullRooms.onValueChanged.AddListener(_ => FilterChanged());

				Multiplayer.OnRoomListUpdated.AddListener(RoomsUpdated);
				Multiplayer.OnConnected.AddListener(Connected);
				Multiplayer.OnDisconnected.AddListener(Disconnected);
				Multiplayer.OnJoinRejected.AddListener(JoinRejected);
				Multiplayer.OnRoomLeft.AddListener(LeftRoom);

				CheckCanJoinRoom();
			}
		}

		private void Update()
		{
			_refreshTimer -= Time.deltaTime;
		}


		public void Refresh()
		{
			if (_refreshTimer > 0) return;

			_refreshTimer = _refreshDelay;
			Multiplayer.RefreshRoomList();
		}

		public void SelectRoom(RoomBrowserRow roomInfo)
		{
			if (roomInfo == _selectedRow) return;

			if (_selectedRow != null)
				_selectedRow.ChangeSelection(false);

			if (roomInfo == null)
			{
				_selectedRow = null;
				_buttonJoin.interactable = false;
				return;
			}

			_selectedRow = roomInfo;
			_selectedRow.ChangeSelection(true);
			_buttonJoin.interactable = true;
		}

		private bool CheckCanJoinRoom()
		{
			SetCanJoinRoom(!Multiplayer.InRoom);
			return !Multiplayer.InRoom;
		}

		private void SetCanJoinRoom(bool canJoin)
		{
			_buttonJoin.gameObject.SetActive(canJoin);
			_buttonLeave.gameObject.SetActive(!canJoin);
		}

		public void JoinRoom()
		{
			if (_selectedRow == null || _selectedRow.Room.ID == Multiplayer.CurrentRoom?.ID) return;

			if (Multiplayer.InRoom)
				Multiplayer.CurrentRoom?.Leave();

			if (!_selectedRow.RequiresPassword)
			{
				_selectedRow.Room.Join();
				SelectRoom(null);
			}
			else
			{
				_passwordPopupMenu.SetActive(true);
				return;
			}
		}

		public void JoinRoomWithPassword(TMP_InputField inputPassword)
		{
			ushort formattedPassword = CreateRoomMenu.FormatPassword(inputPassword.text);
			Multiplayer.JoinRoom(_selectedRow.Room, formattedPassword);
		}

		public void JoinRoomWithInviteCode(TMP_InputField inputInviteCode)
		{
			Multiplayer.JoinWithInviteCode(inputInviteCode.text);
		}

		public void LeaveRoom()
		{
			if (Multiplayer.CurrentRoom == null) return;

			Multiplayer.CurrentRoom.Leave();
		}

		#region Multiplayer Events

		private void RoomsUpdated(Multiplayer multiplayer)
		{
			List<Room> newRooms = Multiplayer.AvailableRooms;

			SelectRoom(null);

			_rowsPool = _activeRows.Take(_activeRows.Count).ToList();

			int l = newRooms.Count - _rowsPool.Count;
			if (l > 0)
			{
				for (int i = 0; i < l; i++)
				{
					RoomBrowserRow roomInfo = Instantiate(_roomBrowserRowPrefab, _rowsContainer);
					roomInfo.Initialize(this);
					_rowsPool.Add(roomInfo);
				}
			}

			foreach (var row in _rowsPool)
			{
				row.gameObject.SetActive(false);
			}

			_activeRows = _rowsPool.Take(newRooms.Count).ToList();

			for (int i = 0; i < newRooms.Count; i++)
			{
				_activeRows[i].UpdateRoom(newRooms[i]);
			}

			_filteredRows = FilterRows();

			foreach (var room in _filteredRows)
			{
				room.gameObject.SetActive(true);
			}

			DisplayFilteredRows();

			SortRowsBy(_sortColumn, true);
		}

		private void Connected(Multiplayer multiplayer, Endpoint endpoint)
		{
			if (multiplayer.InRoom)
			{
				JoinedRoom(multiplayer, multiplayer.CurrentRoom, multiplayer.Me);
				return;
			}
		}

		private void Disconnected(Multiplayer multiplayer, Endpoint endPoint)
		{
			StatusPopup.Instance.TriggerStatus("Disconnected from server!", true);
		}

		private void JoinedRoom(Multiplayer multiplayer, Room room, User user)
		{
			SetCanJoinRoom(false);
			gameObject.SetActive(false);

			if (MapDescriptions.Instance.ChangeSceneOnRoomJoined)
			{
				CustomRoomInfo roomInfo = Reader.DeserializePackedString<CustomRoomInfo>(room.Name);
				Multiplayer.LoadScene(roomInfo.SceneIndex, SpawnAvatarAfterLoad);
			}
		}

		private void JoinRejected(Multiplayer multiplayer, string rejectReason)
		{
			string message = rejectReason == "SessionInvalidPin" ? "Incorrect password" : rejectReason;
			StatusPopup.Instance.TriggerStatus($"Failed to join room!\nReason: '{message}'");
		}

		private void LeftRoom(Multiplayer arg0)
		{
			_textInviteCode.text = "";
			SetCanJoinRoom(true);
		}

		#endregion

		private void FilterChanged()
		{
			SelectRoom(null);
			_filteredRows = FilterRows();
			DisplayFilteredRows();
		}

		private void DisplayFilteredRows()
		{
			foreach (var row in _activeRows)
			{
				row.gameObject.SetActive(false);
			}
			foreach (var row in _filteredRows)
			{
				row.gameObject.SetActive(true);
			}
		}

		private List<RoomBrowserRow> FilterRows()
		{
			string searchText = _inputSearch.text.ToUpper();
			bool hideLockedRooms = _toggleHidePasswordRooms.isOn;
			bool hideFullRooms = _toggleHideFullRooms.isOn;

			return _activeRows.Where(r =>
				r.RoomName.ToUpper().Contains(searchText) // If searchText contains text, only show rooms with names that contains the text;
				&& !(hideLockedRooms && r.Room.Pincode) // If hideLockedRooms, hide rooms that requires passwords to join;
				&& !(hideFullRooms && r.Room.GetUserCount() >= r.Room.MaxUsers) // If hideFullRooms, hide rooms where connected users >= MaxUsers;
			).ToList();
		}

		/// <summary>
		/// Sorts rooms based on the specified column and alternates the sort direction when the same column is selected consecutively.
		/// </summary>
		/// <param name="column">The column to use for sorting. If set to None, no sorting is performed.</param>
		/// <param name="ignoreFlip">If true, the sorting direction is not reversed. Should be true when refreshing the list.</param>
		public void SortRowsBy(Column column, bool ignoreFlip = false)
		{
			IOrderedEnumerable<RoomBrowserRow> sortedList;

			if (!ignoreFlip)
				_sortDescending = _sortColumn == column ? !_sortDescending : false;
			_sortColumn = column;

			if (!_sortDescending)
			{
				switch (column)
				{
					case Column.Name:
						sortedList = _filteredRows.OrderBy(s => s.RoomName);
						break;
					case Column.GameMode:
						sortedList = _filteredRows.OrderBy(s => s.GameMode);
						break;
					case Column.Players:
						sortedList = _filteredRows.OrderByDescending(s => s.ConnectedPlayers);
						break;
					case Column.Map:
						sortedList = _filteredRows.OrderBy(s => s.SceneIndex);
						break;
					case Column.Password:
						sortedList = _filteredRows.OrderByDescending(s => s.RequiresPassword);
						break;
					case Column.None:
					default:
						_roomBrowserRowPrefab.transform.SetAsLastSibling();
						OnRowsSorted.Invoke();
						return;
				}
			}
			else
			{
				switch (column)
				{
					case Column.Name:
						sortedList = _filteredRows.OrderByDescending(s => s.RoomName);
						break;
					case Column.GameMode:
						sortedList = _filteredRows.OrderByDescending(s => s.GameMode);
						break;
					case Column.Players:
						sortedList = _filteredRows.OrderBy(s => s.ConnectedPlayers);
						break;
					case Column.Map:
						sortedList = _filteredRows.OrderByDescending(s => s.SceneIndex);
						break;
					case Column.Password:
						sortedList = _filteredRows.OrderBy(s => s.RequiresPassword);
						break;
					case Column.None:
					default:
						_roomBrowserRowPrefab.transform.SetAsLastSibling();
						OnRowsSorted.Invoke();
						return;
				}
			}

			List<RoomBrowserRow> sortedRooms = sortedList.ThenBy(s => s.RoomName).ToList();
			for (int i = 0; i < sortedRooms.Count; i++)
			{
				sortedRooms[i].transform.SetSiblingIndex(i);
			}
			OnRowsSorted.Invoke();
		}
		
		public new void Reset()
		{
			base.Reset();
			EnsureEventSystem.Ensure(true);
		}
	}
}
