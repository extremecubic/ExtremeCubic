using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetDropdownObjectInactive : MonoBehaviour
{
	[SerializeField] bool _changeSize = false;
	[SerializeField] bool _hideEditorObjects = true;

	[SerializeField] float _newSize = 200.0f;

	void Start()
	{
		// this will hide dropdown objects in a dropdown menu (hardcoded to what is hidden in tileEitor)
		if (_hideEditorObjects)		
			if(GetComponentInChildren<Text>().text == "empty" || GetComponentInChildren<Text>().text == "edge")			
				GetComponent<Toggle>().interactable = false;

		// change the size of the dropdown rect transform
		// the values that is set in the "template" object is reset to just fit all dropdown objects
		// this is a fix for that if we want to use a bigger height of rect then all items combined
		if (_changeSize)
			GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _newSize);
		
	}

}
