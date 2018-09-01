using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectHovered : MonoBehaviour, IPointerEnterHandler, IDeselectHandler, IPointerExitHandler
{
	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!EventSystem.current.alreadySelecting)
			EventSystem.current.SetSelectedGameObject(gameObject);
	}

	public void OnDeselect(BaseEventData eventData)
	{
		GetComponent<Selectable>().OnPointerExit(null);
	}

	public void OnPointerExit(PointerEventData eventData)
	{		
		if (EventSystem.current.currentSelectedGameObject == gameObject)
			EventSystem.current.SetSelectedGameObject(null);
	}
}
