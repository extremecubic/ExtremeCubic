using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileVisualizer))]
public class TileVisualizerEditor : Editor
{
	
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		TileVisualizer TV = target as TileVisualizer;

		if (GUILayout.Button("Show Tile level"))
			TV.Show();

		if (GUILayout.Button("Show Tile Grid Only"))
			TV.ShowGrid();

		if (GUILayout.Button("CLEAR"))
			TV.Clear();

		TV.CheckGrid();
	}

}
