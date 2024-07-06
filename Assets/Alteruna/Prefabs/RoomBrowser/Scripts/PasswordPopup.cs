using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alteruna
{
	public class PasswordPopup : MonoBehaviour
	{
		[SerializeField] private RoomBrowser _roomBrowser;
		[SerializeField] private TMP_InputField _inputPassword;
		[SerializeField] private Button _buttonConnect;


		private void Start()
		{
			_inputPassword = _inputPassword != null ? _inputPassword : GetComponentInChildren<TMP_InputField>();
			_buttonConnect = _buttonConnect != null ? _buttonConnect : GetComponentInChildren<Button>();

			_inputPassword.onValueChanged.AddListener(InputValueChanged);
			_inputPassword.onSubmit.AddListener(Submit);
			_buttonConnect.onClick.AddListener(() => ValidatePasswordAndSubmit(_inputPassword.text));
		}

		private void InputValueChanged(string value)
		{
			// Parse to int since parsing to ushort will modify the value ushort.MaxValue if the value is any higher.
			if (int.TryParse(_inputPassword.text, out int password) && password > ushort.MaxValue)
			{
				_inputPassword.text = ushort.MaxValue.ToString();
			}
		}

		private void ValidatePasswordAndSubmit(string text)
		{
			if (ushort.TryParse(text, out ushort password))
			{
				Submit(password);
			}
			else
			{
				Debug.LogWarning("Password field contains invalid characters!");
				StatusPopup.Instance.TriggerStatus("Password field contains invalid characters", false);
			}
		}

		public void Submit(string password)
		{
			ValidatePasswordAndSubmit(_inputPassword.text);
		}

		public void Submit(ushort password)
		{
			if (string.IsNullOrWhiteSpace(_inputPassword.text))
			{
				gameObject.SetActive(false);
				return;
			}

			//_roomBrowser.JoinRoomWithPassword(password);
			gameObject.SetActive(false);
		}
	}
}
