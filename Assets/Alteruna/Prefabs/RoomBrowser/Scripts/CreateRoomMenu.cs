using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Alteruna
{
	public class CreateRoomMenu : BaseRoomBrowser
	{
		[Range(0, 30)]
		public int MaxNameLength = 20; // It's recommended to limit the length of RoomName since strings can easily scale up bandwith usage.
		[Range(2, 10)]
		public int MaxPlayers = 10;

		[SerializeField] private Image _imageMap;
		[SerializeField] private TMP_Text _textMapTitle;
		[SerializeField] private TMP_Text _textMapInfo;
		[SerializeField] private TMP_Text _textInviteCode;

		[Header("Settings")]

		[SerializeField] private TMP_InputField _inputRoomName;
		[SerializeField] private TMP_InputField _inputMaxPlayers;
		[SerializeField] private TMP_InputField _inputPassword;
		[SerializeField] private TMP_Dropdown _dropdownGameMode;
		[SerializeField] private TMP_Dropdown _dropdownScene;
		[SerializeField] private Toggle _toggleHideRoom;
		[SerializeField] private Button _buttonCreateRoom;

		private CustomRoomInfo _customRoomInfo;
		private List<MapInfo> _mapDescriptions = new List<MapInfo>();


		private new void OnEnable()
		{
			base.OnEnable();
			Multiplayer.OnRoomCreated.AddListener(CreatedRoom);
		}

		private void OnDisable()
		{
			Multiplayer.OnRoomCreated.RemoveListener(CreatedRoom);
		}

		void Start()
		{
			_customRoomInfo = new CustomRoomInfo();

			RoomNameChanged(Multiplayer.Me.Name);
			PopulateDropdownWithEnumValues<GameMode>(_dropdownGameMode);
			PopulateDropdownWithSceneNames(_dropdownScene);

			_inputRoomName.characterLimit = MaxNameLength;

			_inputRoomName.onValueChanged.AddListener(RoomNameChanged);
			_inputMaxPlayers.onEndEdit.AddListener(MaxPlayersChanged);

			_dropdownGameMode.onValueChanged.AddListener(GameModeChanged);
			_dropdownScene.onValueChanged.AddListener(MapChanged);
			_toggleHideRoom.onValueChanged.AddListener(ToggleHideRoom);

			_buttonCreateRoom.onClick.AddListener(Submit);
		}

		private void ToggleHideRoom(bool value)
		{
			_inputPassword.transform.parent.gameObject.SetActive(!value);
		}

		public void ChangeMaxPlayersValue(int value)
		{
			int maxPlayers = int.Parse(_inputMaxPlayers.text) + value;
			maxPlayers = HandleMaxPlayers(maxPlayers);
			_inputMaxPlayers.SetTextWithoutNotify(maxPlayers.ToString());
		}

#region Callbacks

		private void RoomNameChanged(string value)
		{
			HandleRoomName(value);

			if (_customRoomInfo.RoomName != value)
				_inputRoomName.SetTextWithoutNotify(_customRoomInfo.RoomName);
		}

		private void MaxPlayersChanged(string value)
		{
			if (!int.TryParse(value, out int maxPlayers))
			{
				maxPlayers = MaxPlayers;
			}
			else
			{
				maxPlayers = HandleMaxPlayers(maxPlayers);
			}

			_inputMaxPlayers.SetTextWithoutNotify(maxPlayers.ToString());
		}

		private void GameModeChanged(int value)
		{
			HandleGameModeValue(value);

			if ((int)_customRoomInfo.GameMode != value)
				_dropdownGameMode.SetValueWithoutNotify((int)_customRoomInfo.GameMode);
		}

		private void MapChanged(int value)
		{
			HandleMapValue(value);
			SetMapInfo();

			if (_customRoomInfo.SceneIndex != _mapDescriptions[value].BuildIndex)
				_dropdownScene.SetValueWithoutNotify(_customRoomInfo.SceneIndex);
		}

		private void CreatedRoom(Multiplayer multiplayer, bool success, Room room, string inviteCode)
		{
			gameObject.SetActive(false);
			_textInviteCode.text = _toggleHideRoom.isOn ? inviteCode : "";

			if (success && MapDescriptions.Instance.ChangeSceneOnRoomJoined)
			{
				CustomRoomInfo roomInfo = Reader.DeserializePackedString<CustomRoomInfo>(room.Name);
				Multiplayer.LoadScene(roomInfo.SceneIndex, SpawnAvatarAfterLoad);
			}
			else
			{
				Debug.LogError("Failed to create room!");
			}
		}

#endregion

		private void SetMapInfo()
		{
			MapInfo info = _mapDescriptions.FirstOrDefault(m => m.BuildIndex == _customRoomInfo.SceneIndex);
			_imageMap.sprite = info.Image;
			_textMapInfo.text = info.Description;
			_textMapTitle.text = info.Title;

			if (_imageMap.sprite == null)
				_imageMap.sprite = MapDescriptions.Instance.DefaultImage;
		}

		private void HandleRoomName(string value)
		{
			_customRoomInfo.RoomName = value.Length > MaxNameLength ? value.Substring(0, MaxNameLength) : value;
		}

		private int HandleMaxPlayers(int value)
		{
			if (value < 2)
				value = 2;
			else if (value > MaxPlayers)
				value = MaxPlayers;

			return value;
		}

		private void HandleGameModeValue(int value)
		{
			_customRoomInfo.GameMode = Enum.IsDefined(typeof(GameMode), (GameMode)value) ? (GameMode)value : 0;
		}

		private void HandleMapValue(int value)
		{
			_customRoomInfo.SceneIndex = _mapDescriptions[value].BuildIndex;
		}

		public void Submit()
		{
			if (Multiplayer.InRoom)
			{
				StatusPopup.Instance.TriggerStatus("Invalid action!\nAlready in a room!");
				return;
			}

			bool maxPlayersValid = int.TryParse(_inputMaxPlayers.text, out int maxPlayers);
			ushort password = FormatPassword(_inputPassword.text);

			if (_toggleHideRoom.isOn)
				password = (ushort)UnityEngine.Random.Range(1, 1024);

			HandleRoomName(_inputRoomName.text);
			HandleGameModeValue(_dropdownGameMode.value);
			HandleMapValue(_dropdownScene.value);


			_customRoomInfo.RoomName = _customRoomInfo.RoomName.Trim(); // Remove whitespaces

			if (_customRoomInfo.RoomName.Length > MaxNameLength)
			{
				StatusPopup.Instance.TriggerStatus($"Invalid value!\n[Room Name] can be no longer than '{MaxNameLength}' characters!");
				return;
			}

			if (!maxPlayersValid || maxPlayers > MaxPlayers || maxPlayers < 2)
			{
				StatusPopup.Instance.TriggerStatus($"Invalid value!\n[Max Players] need to be between 2 and {MaxPlayers}!");
				return;
			}

			// Serialize custom info into a string and pass it as the name of the room.
			string roomInfo = Writer.SerializeAndPackString(_customRoomInfo);

			if (!_toggleHideRoom.isOn)
				Multiplayer.CreateRoom(roomInfo, _toggleHideRoom.isOn, password, true, true, (ushort)maxPlayers);
			else
				Multiplayer.CreatePrivateRoom(_customRoomInfo.RoomName, (ushort)MaxPlayers, true, true);
		}

		public static ushort FormatPassword(string password)
		{
			if (password.Length == 0) return 0;

			if (int.TryParse(password, out int pin))
			{
				if (pin < 0) return (ushort)-pin;
				if (pin != 0 && (ushort)pin == 0) return (ushort)(pin >> 8);
				return (ushort)pin;
			}

			ushort hash = 0;
			foreach (char c in password)
			{
				hash += c;
			}

			if (hash == 0) return ushort.MaxValue;
			return hash;
		}

		/// <summary>
		/// Populates a dropdown with options based on an enum type.
		/// </summary>
		/// <param name="dropdown">The dropdown to populate.</param>
		/// <typeparam name="T">The enum type to base the values on.</typeparam>
		private void PopulateDropdownWithEnumValues<T>(TMP_Dropdown dropdown) where T : Enum
		{
			// Clear existing options
			dropdown.ClearOptions();

			// Retrieve enum names dynamically
			string[] names = Enum.GetNames(typeof(T));

			// Create a list of OptionData to store enum member names
			List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

			foreach (string name in names)
			{
				// Add enum name as an option to the dropdown
				options.Add(new TMP_Dropdown.OptionData(name));
			}

			dropdown.options = options;
		}

		private void PopulateDropdownWithSceneNames(TMP_Dropdown dropdown)
		{
			dropdown.ClearOptions();
			List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

			_mapDescriptions.Clear();
			_mapDescriptions = MapDescriptions.Instance.GetValidMapDescriptions();

			if (_mapDescriptions.Count <= 0)
			{
				Debug.LogError("Could not get any MapDescriptions." +
					"This is usually due to not having any scenes in build settings or that all descriptions are hidden.");
			}

			foreach (var item in _mapDescriptions)
			{
				options.Add(new TMP_Dropdown.OptionData(item.Title));
			}
			dropdown.options = options;

			HandleMapValue(0);
			SetMapInfo();
		}

#if UNITY_EDITOR
		private new void Reset()
		{
			base.Reset();

			if (EditorApplication.isPlaying)
				return;

			MapDescriptions.Instance.PopulateScenesIntoList();
		}
#endif
	}
}
