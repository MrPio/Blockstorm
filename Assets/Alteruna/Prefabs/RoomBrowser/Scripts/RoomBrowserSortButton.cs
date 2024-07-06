using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alteruna
{
	[RequireComponent(typeof(Toggle))]
	public class RoomBrowserSortButton : MonoBehaviour
	{
		[SerializeField] private RoomBrowser.Column _sortBy;

		[SerializeField] private Color _sortingColor = new Color(0.8f, 0.8f, 0.8f, 1);
		[Range(1, 10)]
		[SerializeField] private float _colorMultiplier = 1.5f;

		[SerializeField] private RoomBrowser _roomBrowser;

		private Image _targetImage;


		void Start()
		{
			_targetImage = GetComponent<Image>();

			if (_roomBrowser == null)
			{
				Debug.LogException(new MissingReferenceException("RoomBrowser reference is null"), gameObject);
				return;
			}

			GetComponent<Toggle>().onValueChanged.AddListener(ChangeState);
		}

		private void ChangeState(bool selected)
		{
			Color targetColor = selected ? _sortingColor * _colorMultiplier : new Color(1, 1, 1, 1);
			_targetImage.CrossFadeColor(targetColor, 0, true, true);

			if (selected == true)
				_roomBrowser.SortRowsBy(_sortBy);
		}
	}
}
