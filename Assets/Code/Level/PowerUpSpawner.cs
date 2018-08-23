﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class PowerUpSpawner : Photon.MonoBehaviour
{
	[Serializable]
	public struct SpawnerSystem
	{
		public string spawnerName;

		public PowerUpType[] powerUps;
		public MinMaxFloat spawnFrequency;
		public int numSpawnsEachTime;
		public bool sameEachTime;

		[HideInInspector] public float timer;
	}
	[SerializeField] PowerUpModel    _powerUps;
	[SerializeField] Transform		 _powerUpFolder;
	[SerializeField] SpawnerSystem[] _spawners;
	
	void Awake()
	{		
		for (int i =0; i< _spawners.Length; i++)
			_spawners[i].timer = _spawners[i].spawnFrequency.RandomRange();		
	}

	void Update()
	{
		if (!PhotonNetwork.isMasterClient)
			return;

		UpdateSpawners();
	}

	void UpdateSpawners()
	{
		for (int i = 0; i < _spawners.Length; i++)
		{
			_spawners[i].timer -= Time.deltaTime;
			if (_spawners[i].timer <= 0)
			{
				int powerIndex = Random.Range(0, _spawners[i].powerUps.Length);
				
				for (int y = 0; y < _spawners[i].numSpawnsEachTime; y++)
				{
					Tile freeTile = TileMap.instance.GetRandomFreeTile(10);
					if (freeTile == null)
						continue;

					photonView.RPC("SpawnPowerUp", PhotonTargets.All, i, powerIndex, freeTile.position.x, freeTile.position.y);
					
					if (!_spawners[i].sameEachTime)
						powerIndex = Random.Range(0, _spawners[i].powerUps.Length);
				}				
				_spawners[i].timer = _spawners[i].spawnFrequency.RandomRange();
			}
		}
	}

	[PunRPC]
	void SpawnPowerUp(int spawnerIndex, int powerIndex, int tileX, int tileY)
	{
		PowerUpType type = _spawners[spawnerIndex].powerUps[powerIndex];

		TileMap.instance.GetTile(new Vector2DInt(tileX, tileY)).SpawnPowerUp(_powerUps.GetPowerUpFromType(type), _powerUpFolder);
	}
}
