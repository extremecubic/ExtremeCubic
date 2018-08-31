using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class Constants
{
    public static readonly string APP_NAME = "Cubic";
    public static readonly string GAME_VERSION = "0.01";

    public static string TILEMAP_SAVE_FOLDER => Path.Combine(Application.dataPath + "/../", "Maps");

	public static string EDGE_TYPE = "edge";

	public static int NUM_COLLISIONS_TO_SAVE_ON_SERVER = 10;

	// input mapping strings
	public static string AXIS_HORIZONTAL = "Horizontal";
	public static string AXIS_VERTICAL   = "Vertical";
	public static string BUTTON_CHARGE   = "Charge";

	// PhotonPlayer properties keys
	public static string CHARACTER_NAME = "0";
	public static string LEVEL_SCENE_NAME = "1";
	public static string SPAWN_ID = "2";
	public static string SKIN_ID = "3";
	public static string PLAYER_READY = "4";
	public static string NOMINATED_LEVEL = "5";

	// menu pages names
	public static string SCREEN_START                     = "StartScreen";
	public static string SCREEN_ONLINEPLAY                = "OnlinePlayScreen";
	public static string SCREEN_ONLINE_PLAY_WITH_FRIENDS  = "PlayWithFriendsScreen";
	public static string SCREEN_ONLINE_PLAY_QUICK_MATCH   = "QuickmatchScreen";
	public static string SCREEN_ONLINE_LEVELSELECT        = "OnlineLevelSelectScreen";
	public static string SCREEN_ONLINE_CHARACTERSELECT    = "OnlineCharacterSelectScreen";
	public static string SCREEN_ONLINE_RANDOM_MATCHMAKING = "RandomPlayScreen";

	// temp storage here for now
	public static bool onlineGame = false;

}
