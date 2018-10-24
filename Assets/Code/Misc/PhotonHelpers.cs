﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class PhotonHelpers
{
	// sets a player property
	// if one already exist with the given key it
	// will be overwritten else a new one will be created
	public static void SetPlayerProperty<T>(PhotonPlayer player, string key, T value)
	{
		Hashtable p = player.CustomProperties;
		if (p == null)
			p = new Hashtable();

		if (p.ContainsKey(key))
			p[key] = value;
		else
			p.Add(key, value);

		player.SetCustomProperties(p);
	}
	
	// clear the hashtable of player properties
	public static void ClearPlayerProperties(PhotonPlayer player)
	{
		Hashtable p = player.CustomProperties;
		if (p != null)
		{
			 if (p.ContainsKey(Constants.CHARACTER_NAME                 )) p[Constants.CHARACTER_NAME                 ] = null;
			 if (p.ContainsKey(Constants.LEVEL_SCENE_NAME               )) p[Constants.LEVEL_SCENE_NAME               ] = null;
			 if (p.ContainsKey(Constants.SPAWN_ID                       )) p[Constants.SPAWN_ID                       ] = null;
			 if (p.ContainsKey(Constants.SKIN_ID                        )) p[Constants.SKIN_ID                        ] = null;
			 if (p.ContainsKey(Constants.PLAYER_READY                   )) p[Constants.PLAYER_READY                   ] = null;
			 if (p.ContainsKey(Constants.NOMINATED_LEVEL                )) p[Constants.NOMINATED_LEVEL                ] = null;
			 if (p.ContainsKey(Constants.NOMINATED_LEVEL_TILEMAP        )) p[Constants.NOMINATED_LEVEL_TILEMAP        ] = null;
			 if (p.ContainsKey(Constants.LEVEL_MAP_NAME                 )) p[Constants.LEVEL_MAP_NAME                 ] = null;
			 if (p.ContainsKey(Constants.NOMINATED_LEVEL_GAME_MODE_INDEX)) p[Constants.NOMINATED_LEVEL_GAME_MODE_INDEX] = null;
			 if (p.ContainsKey(Constants.MATCH_GAME_MODE                )) p[Constants.MATCH_GAME_MODE                ] = null;

			player.SetCustomProperties(p);
		}
	}
}
