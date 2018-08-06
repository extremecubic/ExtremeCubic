using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MenuPage : Photon.MonoBehaviour
{
	[SerializeField] protected GameObject _content;

	public string pageName { get; protected set; }

	public abstract void OnPageEnter();
	public abstract void UpdatePage();
	public abstract void OnPageExit();
	public abstract void OnPlayerLeftRoom(PhotonPlayer player);

	void Awake()
	{
		pageName = name;	
	}

	public void EnableDisableContent(bool enable)
	{
		_content.SetActive(enable);
	}
}
