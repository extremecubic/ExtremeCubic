using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class PhotonHelpers
{
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
	
	public static void ClearPlayerProperties(PhotonPlayer player)
	{
		Hashtable p = player.CustomProperties;
		if(p != null)
		{
			p.Clear();
			player.SetCustomProperties(p);
		}
	}
}
