using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MessagePromt : MonoBehaviour
{
	public delegate void OkAction();
	OkAction OnClicked;

	[SerializeField] Text _messagetext;
	[SerializeField] Button _okButton;
	
	public void SetAndShow(string message, OkAction action)
	{
		gameObject.SetActive(true);
		_messagetext.text = message;
		OnClicked = action;
		EventSystem.current.SetSelectedGameObject(_okButton.gameObject);
	}

	public void OnOk()
	{
		if (OnClicked != null)
			OnClicked.Invoke();

		gameObject.SetActive(false);
	}
	
}
