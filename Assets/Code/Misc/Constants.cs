using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class Constants
{
    public static readonly string APP_NAME = "Cubic";
    public static readonly string GAME_VERSION = "0.01";

    public static readonly string TILEMAP_SAVE_FOLDER = Path.Combine(Application.dataPath + "/../", "Maps");

	public static readonly string EDGE_TYPE = "edge";

	public static readonly int NUM_COLLISIONS_TO_SAVE_ON_SERVER = 10;

	public static Vector2DInt NOT_FOUND_SPECIALTILE = new Vector2DInt(-1000, -1000);

	// input mapping strings
	public static readonly string AXIS_HORIZONTAL = "Horizontal";
	public static readonly string AXIS_VERTICAL   = "Vertical";
	public static readonly string BUTTON_CHARGE   = "Charge";
	public static readonly string BUTTON_LB		  = "LB";
	public static readonly string BUTTON_RB		  = "RB";

	// PhotonPlayer properties keys
	public static readonly string CHARACTER_NAME                   = "0";
	public static readonly string LEVEL_SCENE_NAME                 = "1";
	public static readonly string SPAWN_ID                         = "2";
	public static readonly string SKIN_ID                          = "3";
	public static readonly string PLAYER_READY                     = "4";
	public static readonly string NOMINATED_LEVEL                  = "5";
	public static readonly string NOMINATED_LEVEL_TILEMAP          = "6";
	public static readonly string NOMINATED_LEVEL_MAP_INDEX        = "7";
	public static readonly string NOMINATED_LEVEL_GAME_MODE_INDEX  = "8";

	// menu pages names
	public static readonly string SCREEN_START                     = "StartScreen";
	public static readonly string SCREEN_ONLINEPLAY                = "OnlinePlayScreen";
	public static readonly string SCREEN_ONLINE_PLAY_WITH_FRIENDS  = "PlayWithFriendsScreen";
	public static readonly string SCREEN_ONLINE_PLAY_QUICK_MATCH   = "QuickmatchScreen";
	public static readonly string SCREEN_ONLINE_LEVELSELECT        = "OnlineLevelSelectScreen";
	public static readonly string SCREEN_ONLINE_CHARACTERSELECT    = "OnlineCharacterSelectScreen";
	public static readonly string SCREEN_ONLINE_RANDOM_MATCHMAKING = "RandomPlayScreen";

	// temp storage here for now
	public static bool  onlineGame = true;
	public static float masterEffectVolume = 0.20f;


}
