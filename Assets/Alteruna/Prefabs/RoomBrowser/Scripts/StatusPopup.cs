using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alteruna
{
	public class StatusPopup : MonoBehaviour
	{
		private static StatusPopup _instance;
		public static StatusPopup Instance
		{
			get
			{
				if (_instance == null)
					Debug.LogError("StatusPrompt instance is null!");
				return _instance;
			}
		}

		[SerializeField] private Button _buttonClose;
		[SerializeField] private TMP_Text _textStatus;
		[SerializeField] private Transform _container;


		void Awake()
		{
			if (_instance != null && _instance != this)
				Destroy(gameObject);
			else
				_instance = this;
		}

		void Start()
		{
			if (EventSystem.current == null)
			{
				Debug.LogWarning("Found No EventSystem. Did you forget to add one?");
			}
			_buttonClose.onClick.AddListener(() => _container.gameObject.SetActive(false));
			_container.gameObject.SetActive(false);
		}

		public void TriggerStatus(string text, bool logWarning = false)
		{
			if (logWarning)
				Debug.LogWarning(text);

			_container.gameObject.SetActive(true);
			_textStatus.text = text;
		}
	}
}
