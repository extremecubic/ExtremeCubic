using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameModesModel", menuName = "Data Models/Game Mode Model", order = 3)]
public class GameModesModel : ScriptableObject
{
	[Header("KING OF THE HILL SETTINGS")]
	public int kingNumRoundsToWin = 3;

	[Header("TURF WAR SETTINGS")]
	public int turfNumRoundsToWin = 3;
	public double turfRespawnTime = 5.0;
	public double turfRoundTime = 120.0;
	public Color[] turfColors;
	

	public Color GetColorFromPlayerIndexID(int playerIndexID)
	{
		return turfColors[playerIndexID];
	}
}
