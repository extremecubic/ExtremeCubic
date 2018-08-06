using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessagePromt : MonoBehaviour
{
	public delegate void OkAction();
	OkAction OnClicked;

	[SerializeField] Text _messagetext;
	
	public void SetAndShow(string message, OkAction action)
	{
		gameObject.SetActive(true);
		_messagetext.text = message;
		OnClicked = action;
	}

	public void OnOk()
	{
		if (OnClicked != null)
			OnClicked.Invoke();

		gameObject.SetActive(false);
	}
	
}
