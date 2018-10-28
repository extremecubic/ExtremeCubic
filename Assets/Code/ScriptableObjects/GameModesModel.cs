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
	public double turfRoundTime   = 120.0;
	public float tileFlipTime     = 0.25f;
	public float tileFlipHeight   = 1.0f;
	[Range(0,1)]
	public float tileChangeColorIntoFlipPercent = 0.0f;
	public Color[] turfColors;

	[Header("ULTIMATE KILLER SETTINGS")]
	public int       killerNumRoundsToWin           = 3;
	public double    killerRespawnTime              = 5.0;
	public double    killerRoundTime                = 120.0;
	public double    tilesRespawnMoveDuration       = 3.0;
	public float     tilesRespawnStartDepth         = -20.0f;
	public float     tilePercentDestroyedForRespawn = 0.1f;
	public AudioClip tilesRespawnSound;

	public Color GetColorFromPlayerIndexID(int playerIndexID)
	{
		return turfColors[playerIndexID];
	}

	public double GetRespawnTimeFromGameMode(GameMode mode)
	{
		if      (mode == GameMode.TurfWar)        return turfRespawnTime;
		else if (mode == GameMode.UltimateKiller) return killerRespawnTime;

		Debug.Assert(true == false, "Tried to get a respawn time on a gamemode that does not have one declared");
		return 0.0;
	}
}
