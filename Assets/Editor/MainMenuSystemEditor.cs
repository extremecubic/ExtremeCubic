using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
				MMS.SetToPage(MMS.menuPages[i].gameObject.name);
				_index = i;
			}
	}
}
