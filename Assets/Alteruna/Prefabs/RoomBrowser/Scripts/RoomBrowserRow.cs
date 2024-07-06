using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alteruna
{
	[RequireComponent(typeof(Button))]
	public class RoomBrowserRow : MonoBehaviour
	{
		public Room Room;

		public string RoomName { get => _textRoomName.text; set { _textRoomName.text = value; } }

		public GameMode GameMode { get => _customInfo.GameMode; set { _customInfo.GameMode = value; _textGameMode.text = value.ToString(); } }
		public int SceneIndex { get => _customInfo.SceneIndex; set { _customInfo.SceneIndex = value; _textMap.text = MapDescriptions.Instance.GetSceneTitleByIndex(value); } }

		public int MaxPlayers { get; set; }
		public int ConnectedPlayers { get => _connectedPlayers; set { _connectedPlayers = value; _textPlayers.text = $"{value} / {MaxPlayers}"; } }

		public bool RequiresPassword { get => _passwordIcon.enabled; set { _passwordIcon.enabled = value; } }

		[SerializeField] private Color _evenBackgroundColor;
		[SerializeField] private Color _oddBackgroundColor;
		[SerializeField] private Color _selectedBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 1);

		[SerializeField] private Color _textColor;
		[SerializeField] private Color _selectedTextColor = new Color(0, 0, 0, 1);

		[SerializeField] private TMP_Text _textRoomName;
		[SerializeField] private TMP_Text _textGameMode;
		[SerializeField] private TMP_Text _textMap;
		[SerializeField] private TMP_Text _textPlayers;
		[SerializeField] private Image _passwordIcon;

		private CustomRoomInfo _customInfo;
		private int _connectedPlayers;

		private List<TMP_Text> _texts = new List<TMP_Text>();
		private List<Image> _images = new List<Image>();
		private RoomBrowser _roomBrowser;

		private bool _selected;


		private void Start()
		{
			_texts = GetComponentsInChildren<TMP_Text>().ToList();
			_images = GetComponentsInChildren<Image>().Where(img => img.transform != transform).ToList();
		}

		public void Initialize(RoomBrowser roomBrowser)
		{
			_roomBrowser = roomBrowser;
			_roomBrowser.OnRowsSorted.AddListener(Sorted);
			GetComponent<Button>().onClick.AddListener(() => _roomBrowser.SelectRoom(this));
			SetRowColor();
		}

		public void UpdateRoom(Room room)
		{
			Room = room;
			_customInfo = Reader.DeserializePackedString<CustomRoomInfo>(room.Name);
			RoomName = _customInfo.RoomName;
			GameMode = _customInfo.GameMode;
			SceneIndex = _customInfo.SceneIndex;

			MaxPlayers = Room.MaxUsers;
			ConnectedPlayers = Room.GetUserCount();
			RequiresPassword = Room.Pincode;
		}

		public void ChangeSelection(bool selected)
		{
			_selected = selected;
			SetRowColor();
		}

		private void Sorted()
		{
			SetRowColor();
		}

		private void SetRowColor()
		{
			if (!_selected)
			{
				GetComponent<Image>().color = transform.GetSiblingIndex() % 2 == 0 ? _evenBackgroundColor : _oddBackgroundColor;
				foreach (var text in _texts)
				{
					text.color = _textColor;
				}
				foreach (var image in _images)
				{
					image.color = _textColor;
				}
			}
			else
			{
				GetComponent<Image>().color = _selectedBackgroundColor;
				foreach (var text in _texts)
				{
					text.color = _selectedTextColor;
				}
				foreach (var image in _images)
				{
					image.color = _selectedTextColor;
				}
			}
		}
	}
}
