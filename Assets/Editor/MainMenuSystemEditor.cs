using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(MainMenuSystem))]
public class MainMenuSystemEditor : Editor
{
	int _index;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		MainMenuSystem MMS = target as MainMenuSystem;

		GUILayout.Label(string.Format("ACTIVE PAGE : {0}", MMS.menuPages[_index].gameObject.name));

		for(int i =0; i < MMS.menuPages.Length; i++)
			if (GUILayout.Button(MMS.menuPages[i].gameObject.name))
			{
				MMS.SetToPage(MMS.menuPages[i].pageType);
				_index = i;
			}

		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.Label("PAGE TYPE ID:S");
		GUILayout.EndHorizontal();
		GUILayout.Space(5);
		int ID = 0;
		foreach (var item in Enum.GetValues(typeof(MenuPageType)))
		{
			MenuPageType type = (MenuPageType)item;
			GUILayout.Label(string.Format("{0} : {1}", type.ToString(), ID));
			ID++;
		}
	}
}
