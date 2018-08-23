using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartUp : MonoBehaviour
{
	[SerializeField] GameObject _onlinePlayParent;
	[SerializeField] GameObject _localPlayParent;

	void Awake()
	{
		_onlinePlayParent.SetActive(true);	
	}
}
